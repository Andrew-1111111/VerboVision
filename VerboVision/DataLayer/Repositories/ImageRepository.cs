using Microsoft.EntityFrameworkCore;
using VerboVision.DataLayer.AI;
using VerboVision.DataLayer.DB.Context;
using VerboVision.DataLayer.DB.Models;
using VerboVision.DataLayer.Dto;
using VerboVision.DataLayer.Helper;
using VerboVision.DataLayer.Repositories.Interfaces;

namespace VerboVision.DataLayer.Repositories
{
    /// <summary>
    /// Репозиторий для работы с изображениями, предоставляет методы для скачивания изображений по URL и работу с AI
    /// </summary>
    /// <param name="context">Контекст базы данных для работы с сущностями изображений</param>
    /// <param name="logger">Логгер для записи событий и ошибок</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если context или logger равен null</exception>
    public class ImageRepository(ApiAppContext context, ILogger<ImageRepository> logger) : IImageRepository
    {
        #region Поля
        // Контекст базы данных
        private readonly ApiAppContext _context = context ?? throw new ArgumentNullException(nameof(context));

        // Логгер для записи событий и ошибок
        private readonly ILogger<ImageRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Singleton HttpClient
        private static readonly HttpClient _httpClient = new();

        // Таймаут для HTTP-запросов (1 минута)
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(60);
        #endregion

        #region Константы
        // Флаг для включения отладочного режима (false - продакшн, true - отладка)
        private const bool USE_DEBUG = false;

        // Максимальный допустимый размер скачиваемого изображения в байтах (15 Мб)
        private const int MAX_IMAGE_SIZE = 15 * 1024 * 1024;
        #endregion

        /// <summary>
        /// Статический конструктор, выполняет однократную настройку HttpClient
        /// </summary>
        static ImageRepository()
        {
            // Настройка HttpClient
            _httpClient.Timeout = TIMEOUT;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "VerboVision/1.0");
        }

        /// <summary>
        /// Анализирует список пользовательских предметов, определяет материалы через GigaChat и сохраняет результаты в базу данных
        /// </summary>
        /// <param name="autorizationKey">Ключ авторизации для доступа к GigaChat API</param>
        /// <param name="imageId">Уникальный идентификатор изображения в базе данных</param>
        /// <param name="userSubjects">Список названий предметов, введённых пользователем для анализа</param>
        /// <param name="cToken">Токен отмены операции (по умолчанию CancellationToken.None)</param>
        /// <returns>Оболочка <see cref="CoreSubjectsWrapper"/> с распознанными предметами и материалами.
        /// В случае ошибки возвращает пустую оболочку.</returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если imageId равен Guid.Empty или userSubjects пуст</exception>
        public async Task<CoreSubjectsWrapper> AnalyzeSubjectsAsync(
            string autorizationKey, 
            Guid imageId, 
            List<string> userSubjects, 
            CancellationToken cToken = default)
        {
            if (imageId == Guid.Empty)
                throw new ArgumentNullException(nameof(imageId), "ID изображения не может быть пустым");

            if (userSubjects.Count == 0)
                throw new ArgumentNullException(nameof(userSubjects), "Список пользовательских предметов не может быть пустым");

            try
            {
                // 1. Отправляем изображение на обработку в AI
                var aiResponse = await GigaChatCommand.CheckTextAsync(autorizationKey, userSubjects);

                // 2. Парсим ответ
                var coreSubjects = CoreSubjectsWrapper.ParseMultiple(aiResponse);

                // 3. Сохраняем данные изображения в БД
                await UpdateImageSubjectsAsync(imageId, coreSubjects, aiResponse, cToken);

                return coreSubjects;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Ошибка при передаче параметров в GigaChat: {Message}", ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Некорректный формат данных при анализе предметов: {Message}", ex.Message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка HTTP при обращении к GigaChat: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при анализе предметов: {Message}", ex.Message);
            }

            return new CoreSubjectsWrapper();
        }

        /// <summary>
        /// Анализирует изображение по URL: скачивает, вычисляет хеш, отправляет в GigaChat и сохраняет в БД
        /// </summary>
        /// <param name="autorizationKey">Ключ авторизации GigaChat</param>
        /// <param name="url">URL изображения для анализа</param>
        /// <param name="cToken">Токен отмены операции</param>
        /// <returns>Кортеж с UUID записи в БД и распознанными объектами</returns>
        public async Task<(Guid? Uuid, CoreSubjectsWrapper CoreSubjects)> AnalyzeImageAsync(string autorizationKey, string url, CancellationToken cToken = default)
        {
            try
            {
                // 1. Получаем изображение (массив байтов и имя файла)
                var (imageBytes, fileName) = await DownloadImageAsync(url);
                if (imageBytes.Length == 0)
                    return (null, new CoreSubjectsWrapper());

                // 2. Получаем хеш изображения
                var imageHash = GetImageHash(imageBytes);

                // 3. Проверка по хешу: если такое изображение уже есть, возвращаем существующую запись (без вызова GigaChat)
                var imageEntity = await GetImageByHashAsync(imageHash, cToken);
                if (imageEntity != null)
                    return (imageEntity.Id, imageEntity.CoreSubjects ?? new CoreSubjectsWrapper());
                
                // 4. Отправляем изображение на обработку в AI
                var (fileId, aiResponse, coreSubjects) = await GigaChatCommand.CheckImageAsync(autorizationKey, imageBytes, fileName);

                // 5. Сохраняем данные изображения в БД
                var uUid = await AddImageInDbAsync(url, fileName, imageHash, fileId, aiResponse, coreSubjects, cToken);

                return (uUid, coreSubjects);
            }
            catch (OperationCanceledException) 
            { 
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Ошибка получения хеша файла {Url}: {Message}", url, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки или анализа файла {Url}: {Message}", url, ex.Message);
            }

            return (null, new CoreSubjectsWrapper());
        }

        /// <summary>
        /// Асинхронно скачивает изображение по указанному URL с проверкой размера и возвращает его байты вместе с именем файла
        /// </summary>
        /// <param name="url">URL-адрес изображения для скачивания</param>
        /// <returns>
        /// Кортеж, содержащий:
        /// <list type="bullet">
        /// <item><description><c>ImageBytes</c> - массив байтов скачанного изображения</description></item>
        /// <item><description><c>FileName</c> - имя файла, извлечённое из URL или заголовков</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Выбрасывается, если:
        /// <list type="bullet">
        /// <item><description>Размер изображения превышает максимально допустимый лимит (<see cref="MAX_IMAGE_SIZE"/>)</description></item>
        /// <item><description>Произошла ошибка HTTP или превышен таймаут</description></item>
        /// </list>
        /// </exception>
        private async Task<(byte[] ImageBytes, string FileName)> DownloadImageAsync(string url)
        {
            string fileName;

            try
            {
                // Скачиваем с проверкой размера
                using var response = await AsyncExt.TimeoutAsync(_httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead), TIMEOUT);

                // Если HTTP статус не 200-299, здесь будет выброшено исключение
                response.EnsureSuccessStatusCode();

                // Получаем имя файла из URL
                var uri = new Uri(url);
                fileName = Path.GetFileName(uri.LocalPath);

                // Если имя пустое или нет расширения, пробуем получить его из Content-Disposition
                if (string.IsNullOrWhiteSpace(fileName) || !Path.HasExtension(fileName))
                {
                    var contentDisposition = response.Content.Headers.ContentDisposition;
                    if (contentDisposition != null && !string.IsNullOrWhiteSpace(contentDisposition.FileName))
                    {
                        fileName = contentDisposition.FileNameStar ?? contentDisposition.FileName;
                    }
                }

                // Если всё ещё нет имени, генерируем из timestamp
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    var extension = WebHelper.GetExtensionFromContentType(response.Content.Headers.ContentType?.MediaType);
                    fileName = $"Image_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{extension}";
                }

                // Проверка Content-Length если известен
                if (response.Content.Headers.ContentLength > MAX_IMAGE_SIZE)
                {
                    throw new InvalidOperationException($"Размер изображения превышает лимит в {MAX_IMAGE_SIZE / 1024 / 1024} Мб");
                }

                // Скачиваем с контролем размера
                await using var contentStream = await AsyncExt.TimeoutAsync(response.Content.ReadAsStreamAsync(), TIMEOUT);
                await using var memoryStream = new MemoryStream();

                var buffer = new byte[81920];
                int bytesRead;
                long totalBytesRead = 0;

                while ((bytesRead = await AsyncExt.TimeoutAsync(contentStream.ReadAsync(buffer), TIMEOUT)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead > MAX_IMAGE_SIZE)
                    {
                        throw new InvalidOperationException($"Размер изображения превышает лимит в {MAX_IMAGE_SIZE / 1024 / 1024} Мб");
                    }

                    await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }

                var imageBytes = memoryStream.ToArray();

                return (imageBytes, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка скачивания {Url}: {Message}", url, ex.Message);

                return ([], string.Empty);
            }
        }

        /// <summary>
        /// Вычисляет криптографический хеш SHA-256 для переданного массива байтов изображения
        /// </summary>
        /// <param name="image">Массив байтов изображения</param>
        /// <returns>
        /// Строка, содержащая SHA-256 хеш в шестнадцатеричном формате (64 символа в нижнем регистре)
        /// </returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="image"/> равен null</exception>
        private static string GetImageHash(byte[] image)
        {
            return CryptographicImageHash.ComputeSha256Hash(image);
        }

        #region Операции с базой данных
        /// <summary>
        /// Добавляет информацию об изображении в базу данных
        /// </summary>
        /// <param name="url">URL изображения</param>
        /// <param name="fileName">Имя файла</param>
        /// <param name="fileHash">SHA-256 хеш файла</param>
        /// <param name="fileId">ID файла в GigaChat</param>
        /// <param name="aiResponse">Ответ от AI</param>
        /// <param name="coreSubjects">Распознанные объекты</param>
        /// <param name="cToken">Токен отмены</param>
        /// <returns>Автоматически сгенерированный UUID записи из базы данных</returns>
        private async Task<Guid> AddImageInDbAsync(
            string url,
            string fileName,
            string fileHash,
            string fileId,
            string aiResponse,
            CoreSubjectsWrapper? coreSubjects,
            CancellationToken cToken = default)
        {
            try
            {
                // Защита от гонки, второй параллельный запрос с тем же файлом мог уже вставить запись
                var existing = await GetImageByHashAsync(fileHash, cToken);
                if (existing != null)
                    return existing.Id;

                var entity = new ImageEntity(url, fileName, fileHash, fileId, aiResponse, coreSubjects);
                await _context.Images.AddAsync(entity, cToken);
                await _context.SaveChangesAsync(cToken);

                return entity.Id;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка сохранения изображения в базу данных: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения изображения в базу данных: {Message}", ex.Message);
            }
            return Guid.Empty;
        }

        /// <summary>
        /// Получает изображение из базы данных по его уникальному идентификатору
        /// </summary>
        /// <param name="id">Уникальный идентификатор изображения (GUID)</param>
        /// <param name="cToken">Токен отмены</param>
        /// <returns>Сущность изображения или null, если изображение не найдено</returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если id равен Guid.Empty</exception>
        /// <exception cref="OperationCanceledException">Выбрасывается при отмене операции</exception>
        private async Task<ImageEntity?> GetImageByIdAsync(Guid id, CancellationToken cToken = default)
        {
            if (id == Guid.Empty)
                throw new ArgumentNullException(nameof(id), "ID изображения не может быть пустым");

            ImageEntity? result = null;

            try
            {
                // Поиск изображения в базе данных по ID
                result = await _context
                    .Images
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == id, cToken);

                // Логирование результата
                if (result != null)
                {
                    _logger.LogDebug("Изображение с ID {ImageId} успешно найдено", id);
                }
                else
                {
                    _logger.LogDebug("Изображение с ID {ImageId} не найдено в базе данных", id);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Операция получения изображения по ID {ImageId} была отменена", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении изображения по ID {ImageId}: {Message}", id, ex.Message);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Получает изображение из базы данных по SHA-256 хешу
        /// </summary>
        /// <param name="fileHash">SHA-256 хеш файла</param>
        /// <param name="cToken">Токен отмены</param>
        /// <returns>Сущность изображения или null, если изображение не найдено</returns>
        private async Task<ImageEntity?> GetImageByHashAsync(string fileHash, CancellationToken cToken = default)
        {
            ImageEntity? result = null;

            try
            {
                result = await _context
                    .Images
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => EF.Functions.ILike(u.FileSha256Hash, fileHash), cToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения изображения по хешу: {Hash}", fileHash);
            }

            return result;
        }

        /// <summary>
        /// Обновляет информацию о предметах для указанного изображения в базе данных
        /// </summary>
        /// <param name="imageId">Уникальный идентификатор изображения</param>
        /// <param name="coreSubjects">Список предметов с материалами для сохранения</param>
        /// <param name="aiResponse">Ответ от AI</param>
        /// <param name="cToken">Токен отмены</param>
        /// <returns>Задача, представляющая асинхронную операцию</returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если imageId пустой или subjects равен null</exception>
        /// <exception cref="InvalidOperationException">Выбрасывается, если изображение с указанным ID не найдено</exception>
        /// <exception cref="OperationCanceledException">Выбрасывается при отмене операции</exception>
        private async Task UpdateImageSubjectsAsync(
            Guid imageId, 
            CoreSubjectsWrapper coreSubjects, 
            string? aiResponse = null, 
            CancellationToken cToken = default)
        {
            if (imageId == Guid.Empty)
                throw new ArgumentNullException(nameof(imageId), "ID изображения не может быть пустым");

            if (coreSubjects.Count == 0)
                throw new ArgumentNullException(nameof(coreSubjects), "Список предметов не может быть null");

            try
            {
                // Поиск изображения в базе данных
                var imageEntity = await _context
                    .Images
                    .FirstOrDefaultAsync(i => i.Id == imageId, cToken);

                if (imageEntity == null)
                {
                    _logger.LogError("Изображение с ID {ImageId} не найдено в базе данных", imageId);
                    throw new InvalidOperationException($"Изображение с ID {imageId} не найдено");
                }

                // Обновление данных
                imageEntity.CoreSubjects = coreSubjects;

                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    imageEntity.AiResponse = aiResponse;
                }

                // Сохранение изменений в базе данных
                await _context.SaveChangesAsync(cToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при обновлении предметов для изображения {ImageId}: {Message}", imageId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при обновлении предметов для изображения {ImageId}: {Message}", imageId, ex.Message);
                throw;
            }
        }
        #endregion
    }
}