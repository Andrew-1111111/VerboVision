using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VerboVision.DataLayer.AI.Internal.Enums;
using VerboVision.DataLayer.Helper;

namespace VerboVision.DataLayer.AI.Internal.Core
{
    /// <summary>
    /// Предоставляет функциональность для работы с GigaChat API
    /// </summary>
    public class GigaChatClient : IDisposable
    {
        #region Поля
        private readonly HttpClient _httpClient;
        private readonly string _authorizationKey;
        private string _accessToken = null!;
        private readonly string _scope = null!;
        private DateTime _tokenExpiryTime;
        private bool _disposed;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60); // Таймаут запроса
        #endregion

        #region Константы
        // Base URL без /files в конце
        private const string BASE_URL = "https://gigachat.devices.sberbank.ru/api/v1";
        private const string AUTH_URL = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
        private const string FILES_ENDPOINT = BASE_URL + "/files";
        private const string COMPLETIONS_ENDPOINT = BASE_URL + "/chat/completions";

        // Ограничение на размер изображения
        private const int MAX_IMAGE_SIZE_BYTES = 15 * 1024 * 1024;      // 15 МБ для изображений

        // Максимальное число повторных попыток при 401 (обновление токена и повтор запроса)
        private const int MAX_401_RETRIES = 3;
        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="GigaChatClient"/> с закрытым конструктором
        /// </summary>
        /// <param name="authorizationKey">Ключ авторизации в формате Base64 (Client ID:Client Secret)</param>
        /// <param name="scope">Область доступа API (персональная или корпоративная)</param>
        /// <exception cref="ArgumentNullException">Выбрасывается, если ключ авторизации не указан</exception>
        private GigaChatClient(string authorizationKey, GigaChatScope scope)
        {
            if (string.IsNullOrWhiteSpace(authorizationKey))
                throw new ArgumentNullException(nameof(authorizationKey));

            _authorizationKey = authorizationKey;
            _scope = scope.ToString();

            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _httpClient = new HttpClient { Timeout = _timeout };
        }

        /// <summary>
        /// Создает и инициализирует новый экземпляр клиента GigaChat
        /// </summary>
        /// <param name="authorizationKey">Ключ авторизации в формате Base64 (Client ID:Client Secret)</param>
        /// <param name="scope">Область доступа API (персональная или корпоративная)</param>
        /// <returns>Полностью инициализированный экземпляр <see cref="GigaChatClient"/> с действующим токеном доступа</returns>
        public static async Task<GigaChatClient> CreateAsync(string authorizationKey, GigaChatScope scope)
        {
            var client = new GigaChatClient(authorizationKey, scope);
            client._accessToken = await client.GetAccessTokenAsync();
            client.UpdateHttpClientHeaders();
            return client;
        }

        /// <summary>
        /// Отправляет текстовый запрос (промпт) в GigaChat и получает текстовый ответ от модели
        /// </summary>
        /// <param name="prompt">Текстовый запрос</param>
        /// <param name="selectedModel">Идентификатор модели</param>
        /// <param name="selectedTemperature">Температура генерации (0.0-1.0)</param>
        /// <param name="maxTokens">Максимальное число токенов в ответе</param>
        /// <returns>Текстовый ответ модели</returns>
        public async Task<string> SendPromptAsync(
            string prompt,
            string selectedModel,
            double selectedTemperature = 0.7,
            int maxTokens = 1000)
        {
            return await SendPromptAsyncCore(prompt, selectedModel, selectedTemperature, maxTokens, retry401Count: 0);
        }

        /// <summary>
        /// Внутренний метод отправки промпта с поддержкой повторных попыток при 401 ошибке
        /// </summary>
        /// <param name="prompt">Текстовый запрос</param>
        /// <param name="selectedModel">Идентификатор модели</param>
        /// <param name="selectedTemperature">Температура генерации</param>
        /// <param name="maxTokens">Максимальное число токенов</param>
        /// <param name="retry401Count">Счетчик повторных попыток</param>
        /// <returns>Текстовый ответ модели</returns>
        /// <exception cref="ArgumentException">Выбрасывается при пустом промпте</exception>
        /// <exception cref="HttpRequestException">Выбрасывается при ошибке HTTP</exception>
        /// <exception cref="InvalidOperationException">Выбрасывается при пустом ответе модели</exception>
        private async Task<string> SendPromptAsyncCore(
            string prompt,
            string selectedModel,
            double selectedTemperature,
            int maxTokens,
            int retry401Count)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Промпт не может быть пустым.", nameof(prompt));

            await RefreshTokenAsync(false);

            var requestBody = new
            {
                model = selectedModel,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = selectedTemperature,
                max_tokens = maxTokens,
                stream = false,
                update_interval = 0
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await AsyncExt.TimeoutAsync(_httpClient.PostAsync(COMPLETIONS_ENDPOINT, jsonContent), _timeout);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await AsyncExt.TimeoutAsync(
                    response.Content.ReadAsStringAsync(),
                    _timeout);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (retry401Count >= MAX_401_RETRIES)
                    {
                        throw new HttpRequestException(
                            $"Ошибка авторизации GigaChat после {MAX_401_RETRIES + 1} попыток. HTTP статус: 401. Детали: {errorContent}");
                    }

                    await RefreshTokenAsync(true);
                    return await SendPromptAsyncCore(prompt, selectedModel, selectedTemperature, maxTokens, retry401Count + 1);
                }

                throw new HttpRequestException($"Ошибка при отправке промпта. Статус: {response.StatusCode}. Детали: {errorContent}");
            }

            var jsonResponse = await AsyncExt.TimeoutAsync(
                response.Content.ReadAsStringAsync(),
                _timeout);

            var completionResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(
                jsonResponse,
                _jsonOptions);

            return completionResponse?.Choices?.FirstOrDefault()?.Message?.Content
                ?? throw new InvalidOperationException("Ответ модели не содержит текстового содержимого.");
        }

        /// <summary>
        /// Выполняет полный цикл: загружает изображение и отправляет его на анализ
        /// </summary>
        /// <param name="imageBytes">Массив байтов изображения</param>
        /// <param name="fileName">Имя файла (должно включать расширение, например "image.jpg")</param>
        /// <param name="prompt">Текстовый запрос для анализа изображения</param>
        /// <param name="mimeType">MIME-тип изображения (например, "image/jpeg", "image/png")</param>
        /// <param name="selectedModel">Идентификатор модели GigaChat для анализа</param>
        /// <returns>Результат анализа изображения в виде текста</returns>
        public async Task<string> UploadAndAnalyzeImageAsync(
            byte[] imageBytes,
            string fileName,
            string prompt,
            string mimeType,
            string selectedModel)
        {
            var fileId = await UploadFileAsync(imageBytes, fileName, mimeType);
            return await AnalyzeFileAsync(prompt, fileId, selectedModel);
        }

        /// <summary>
        /// Загружает файл в хранилище GigaChat
        /// </summary>
        /// <param name="fileBytes">Массив байтов файла</param>
        /// <param name="fileName">Имя файла с расширением</param>
        /// <param name="mimeType">MIME-тип файла</param>
        /// <returns>Уникальный идентификатор загруженного файла (UUID)</returns>
        /// <exception cref="ArgumentException">Выбрасывается при некорректных параметрах</exception>
        /// <exception cref="HttpRequestException">Выбрасывается при ошибке HTTP-запроса</exception>
        /// <exception cref="InvalidOperationException">Выбрасывается, если ответ не содержит идентификатор файла</exception>
        public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string mimeType)
        {
            return await UploadFileAsyncCore(fileBytes, fileName, mimeType, retry401Count: 0);
        }

        /// <summary>
        /// Внутренний метод загрузки файла с поддержкой повторных попыток при 401 ошибке
        /// </summary>
        /// <param name="fileBytes">Массив байтов файла</param>
        /// <param name="fileName">Имя файла</param>
        /// <param name="mimeType">MIME-тип</param>
        /// <param name="retry401Count">Счетчик повторных попыток</param>
        /// <returns>Уникальный идентификатор файла</returns>
        /// <exception cref="ArgumentException">Выбрасывается при некорректных параметрах</exception>
        /// <exception cref="HttpRequestException">Выбрасывается при ошибке HTTP</exception>
        /// <exception cref="InvalidOperationException">Выбрасывается при отсутствии ID в ответе</exception>
        private async Task<string> UploadFileAsyncCore(byte[] fileBytes, string fileName, string mimeType, int retry401Count)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                throw new ArgumentException("Массив байтов файла не может быть пустым.", nameof(fileBytes));

            if (fileBytes.Length > MAX_IMAGE_SIZE_BYTES)
                throw new ArgumentException(
                    $"Размер изображения превышает максимально допустимый ({MAX_IMAGE_SIZE_BYTES / 1024 / 1024} МБ). " +
                    $"Текущий размер: {fileBytes.Length / 1024 / 1024} МБ",
                    nameof(fileBytes));

            await RefreshTokenAsync(false);

            using var formData = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
            formData.Add(fileContent, "file", fileName);
            formData.Add(new StringContent("general"), "purpose");

            using var response = await AsyncExt.TimeoutAsync(_httpClient.PostAsync(FILES_ENDPOINT, formData), _timeout);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await AsyncExt.TimeoutAsync(response.Content.ReadAsStringAsync(), _timeout);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (retry401Count >= MAX_401_RETRIES)
                    {
                        throw new HttpRequestException(
                            $"Ошибка авторизации GigaChat после {MAX_401_RETRIES + 1} попыток. HTTP статус: 401. Детали: {errorContent}");
                    }

                    await RefreshTokenAsync(true);
                    return await UploadFileAsyncCore(fileBytes, fileName, mimeType, retry401Count + 1);
                }

                throw new HttpRequestException($"Ошибка загрузки файла. Статус: {response.StatusCode}. Детали: {errorContent}");
            }

            var jsonResponse = await AsyncExt.TimeoutAsync(response.Content.ReadAsStringAsync(), _timeout);
            var uploadResult = JsonSerializer.Deserialize<FileUploadResponse>(jsonResponse, _jsonOptions);

            return uploadResult?.Id ?? throw new InvalidOperationException("Не удалось получить идентификатор файла из ответа.");
        }

        /// <summary>
        /// Анализирует файл по его идентификатору с текстовым запросом
        /// </summary>
        /// <param name="prompt">Текстовый запрос для анализа файла</param>
        /// <param name="fileId">Идентификатор файла, полученный при загрузке</param>
        /// <param name="selectedModel">Идентификатор модели GigaChat для анализа</param>
        /// <returns>Текстовый ответ модели</returns>
        /// <exception cref="ArgumentException">Выбрасывается при некорректных параметрах</exception>
        /// <exception cref="HttpRequestException">Выбрасывается при ошибке HTTP-запроса</exception>
        /// <exception cref="InvalidOperationException">Выбрасывается, если ответ модели не содержит текста</exception>
        public async Task<string> AnalyzeFileAsync(string prompt, string fileId, string selectedModel)
        {
            return await AnalyzeFileAsyncCore(prompt, fileId, selectedModel, retry401Count: 0);
        }

        /// <summary>
        /// Внутренний метод анализа файла с поддержкой повторных попыток при 401 ошибке
        /// </summary>
        /// <param name="prompt">Текстовый запрос</param>
        /// <param name="fileId">Идентификатор файла</param>
        /// <param name="selectedModel">Модель для анализа</param>
        /// <param name="retry401Count">Счетчик повторных попыток</param>
        /// <returns>Текстовый ответ модели</returns>
        /// <exception cref="ArgumentException">Выбрасывается при пустых параметрах</exception>
        /// <exception cref="HttpRequestException">Выбрасывается при ошибке HTTP</exception>
        /// <exception cref="InvalidOperationException">Выбрасывается при пустом ответе</exception>
        private async Task<string> AnalyzeFileAsyncCore(string prompt, string fileId, string selectedModel, int retry401Count)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Промпт не может быть пустым.", nameof(prompt));

            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("Идентификатор файла не может быть пустым.", nameof(fileId));

            await RefreshTokenAsync(false);

            var analyzeRequest = new
            {
                model = selectedModel,
                messages = new[] { new { role = "user", content = prompt, attachments = new[] { fileId } } },
                stream = false,
                update_interval = 0
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(analyzeRequest),
                Encoding.UTF8,
                "application/json");

            using var response = await AsyncExt.TimeoutAsync(_httpClient.PostAsync(COMPLETIONS_ENDPOINT, jsonContent), _timeout);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await AsyncExt.TimeoutAsync(response.Content.ReadAsStringAsync(), _timeout);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (retry401Count >= MAX_401_RETRIES)
                    {
                        throw new HttpRequestException(
                            $"Ошибка авторизации GigaChat после {MAX_401_RETRIES + 1} попыток. HTTP статус: 401. Детали: {errorContent}");
                    }

                    await RefreshTokenAsync(true);
                    return await AnalyzeFileAsyncCore(prompt, fileId, selectedModel, retry401Count + 1);
                }

                throw new HttpRequestException($"Ошибка при анализе. Статус: {response.StatusCode}. Детали: {errorContent}");
            }

            var jsonResponse = await AsyncExt.TimeoutAsync(response.Content.ReadAsStringAsync(), _timeout);
            var completionResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(jsonResponse, _jsonOptions);

            return completionResponse?.Choices?.FirstOrDefault()?.Message?.Content
                ?? throw new InvalidOperationException("Ответ модели не содержит текстового содержимого.");
        }

        /// <summary>
        /// Обновляет заголовки HTTP-клиента, устанавливая текущий токен авторизации
        /// </summary>
        private void UpdateHttpClientHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Получает новый токен доступа через OAuth 2.0, используя ключ авторизации
        /// </summary>
        /// <returns>Строка токена доступа</returns>
        /// <exception cref="HttpRequestException">Выбрасывается при ошибке HTTP-запроса к серверу авторизации</exception>
        /// <exception cref="InvalidOperationException">Выбрасывается, если ответ не содержит токен или структура ответа некорректна</exception>
        private async Task<string> GetAccessTokenAsync()
        {
            using var tokenClient = new HttpClient();

            tokenClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authorizationKey);
            tokenClient.DefaultRequestHeaders.Add("RqUID", Guid.NewGuid().ToString());

            var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("scope", _scope)]);
            var response = await AsyncExt.TimeoutAsync(tokenClient.PostAsync(AUTH_URL, content), _timeout);

            if (!response.IsSuccessStatusCode)
            {
                var error = await AsyncExt.TimeoutAsync(response.Content.ReadAsStringAsync(), _timeout);
                throw new HttpRequestException($"Ошибка получения токена: {response.StatusCode}, {error}");
            }

            var json = await AsyncExt.TimeoutAsync(response.Content.ReadAsStringAsync(), _timeout);
            using var doc = JsonDocument.Parse(json);

            var token = doc.RootElement.GetProperty("access_token").GetString();
            var expiresAt = doc.RootElement.GetProperty("expires_at").GetInt64();

            _tokenExpiryTime = DateTimeOffset.FromUnixTimeMilliseconds(expiresAt).UtcDateTime;
            return token ?? throw new InvalidOperationException("Не удалось получить токен из ответа.");
        }

        /// <summary>
        /// Обновляет токен доступа при необходимости или принудительно
        /// </summary>
        /// <param name="forceRefresh">
        /// true - принудительно обновить токен независимо от времени истечения
        /// false - обновить только если токен истекает в ближайшую минуту
        /// </param>
        /// <returns>Задача, представляющая асинхронную операцию</returns>
        /// <exception cref="InvalidOperationException">Выбрасывается, если не удалось обновить токен</exception>
        private async Task RefreshTokenAsync(bool forceRefresh = false)
        {
            // Если не принудительно и токен еще действителен, выходим
            if (!forceRefresh && DateTime.UtcNow < _tokenExpiryTime.AddMinutes(-1))
                return;

            await _tokenLock.WaitAsync();
            try
            {
                // После захвата семафора проверяем снова (double check)
                if (!forceRefresh && DateTime.UtcNow < _tokenExpiryTime.AddMinutes(-1))
                    return;

                var newToken = await GetAccessTokenAsync();
                _accessToken = newToken;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось обновить токен доступа.", ex);
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        /// <summary>
        /// Освобождает управляемые ресурсы
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _tokenLock?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}