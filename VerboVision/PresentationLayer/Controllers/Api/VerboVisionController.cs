using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerboVision.DataLayer.Dto;
using VerboVision.DataLayer.Repositories.Interfaces;
using VerboVision.PresentationLayer.Dto.Api;
using VerboVision.PresentationLayer.Validation;

namespace VerboVision.PresentationLayer.Controllers.Api
{
    /// <summary>
    /// Контроллер для работы с изображениями и взаимодействия с AI
    /// </summary>
    /// <param name="configuration">Конфигурация приложения</param>
    /// <param name="imageRepository">Репозиторий для работы с изображениями</param>
    /// <param name="logger">Логгер для записи событий и ошибок</param>
    [ApiController]
    [Route("Api/[controller]")]
    public class VerboVisionController(IConfiguration configuration, IImageRepository imageRepository, ILogger<VerboVisionController> logger) : ControllerBase
    {
        #region Поля
        // Конфигурация приложения
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Репозиторий для работы с изображениями
        private readonly IImageRepository _imageRepository = imageRepository ?? throw new ArgumentNullException(nameof(imageRepository));

        // Логгер для записи событий и ошибок в контроллере
        private readonly ILogger<VerboVisionController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        #endregion

        /// <summary>
        /// Получает информацию об изображении по URL-ссылке, анализируя его через облачный AI сервис
        /// </summary>
        /// <remarks>
        /// Пример запроса: 
        /// GET /Api/VerboVision/GetImageInfo?imageUrl=https://site.com/image.jpg
        /// </remarks>
        /// <param name="imageUrl">URL-адрес изображения для обработки</param>
        /// <returns>Объект с информацией об изображении</returns>
        /// <response code="200">Успешное выполнение, возвращает ID изображения в базе и ответ AI сервиса</response>
        /// <response code="400">Ошибка валидации - некорректный URL</response>
        /// <response code="401">Ошибка авторизации GigaChat</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        /// <response code="502">Ошибка HTTP при обработке URL</response>
        [HttpGet("GetImageInfo")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UploadImgDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<UploadImgDto>> GetImageInfo([FromQuery] string imageUrl)
        {
            try
            {
                // Валидация URL
                if (!UrlValidation.IsValidJpgUrl(ref imageUrl))
                {
                    return BadRequest("Некорректный URL изображения! Поддерживаются только JPG/JPEG форматы.");
                }

                // Получение ключа авторизации
                var authKey = _configuration["GigaChat:AuthorizationKey"];
                if (string.IsNullOrWhiteSpace(authKey))
                {
                    _logger.LogError("Ключ авторизации GigaChat не задан. Установите ключ в секретах приложения.");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка конфигурации сервера.");
                }

                // Анализ изображения
                var (uuid, coreSubjects) = await _imageRepository.AnalyzeImageAsync(authKey, imageUrl);

                // Проверка результата
                if (uuid == null)
                {
                    return BadRequest("Не удалось получить UUID изображения.");
                }

                // Возвращаем результат (даже если coreSubjects пуст)
                return Ok(new UploadImgDto
                {
                    Id = uuid.Value,
                    CoreSubjects = coreSubjects ?? new CoreSubjectsWrapper()
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Ошибка авторизации GigaChat для URL: {Url}", imageUrl);
                return Unauthorized("Ошибка авторизации при обращении к GigaChat.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка HTTP при обработке URL: {Url}", imageUrl);
                return StatusCode(StatusCodes.Status502BadGateway, "Ошибка при обращении к внешнему сервису.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при обработке URL: {Url}", imageUrl);
                return StatusCode(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера.");
            }
        }

        /// <summary>
        /// Уточняет и дополняет информацию о предметах на изображении, определяет материалы
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        /// POST /Api/VerboVision/CheckImageInfo
        /// {
        ///     "requestId": "123e4567-e89b-12d3-a456-426614174000",
        ///     "subjects": ["ежедневник", "ручка"]
        /// }
        /// </remarks>
        /// <param name="requestId">UUID запроса из БД</param>
        /// <param name="subjects">Пользовательский список предметов</param>
        /// <returns>Список предметов с определёнными материалами</returns>
        /// <response code="200">Успешное определение материалов</response>
        /// <response code="400">Некорректные входные данные</response>
        /// <response code="404">Запрос с указанным ID не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [HttpPost("CheckImageInfo")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<CoreSubjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CoreSubjectDto>>> CheckImageInfo([FromQuery] Guid requestId, [FromBody] List<string> subjects)
        {
            try
            {
                // Валидация входных данных
                if (requestId == Guid.Empty)
                    return BadRequest("ID запроса не может быть пустым");

                if (subjects == null || subjects.Count == 0)
                    return BadRequest("Список предметов не может быть пустым");

                // Получаем ключ авторизации
                var authKey = _configuration["GigaChat:AuthorizationKey"];
                if (string.IsNullOrWhiteSpace(authKey))
                {
                    _logger.LogError("Ключ авторизации GigaChat не настроен");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка конфигурации сервера");
                }

                // Анализ изображения
                var parsedSubjects = await _imageRepository.AnalyzeSubjectsAsync(authKey, requestId, subjects);

                // Проверка результата
                if (parsedSubjects == null || parsedSubjects.Count == 0)
                {
                    return BadRequest("Не удалось получить ответ от AI сервиса.");
                }

                // Возвращаем результат
                return Ok(parsedSubjects);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка при обращении к GigaChat для запроса {RequestId}", requestId);
                return StatusCode(StatusCodes.Status502BadGateway, "Ошибка при обращении к сервису GigaChat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при обработке запроса {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера");
            }
        }

    }
}