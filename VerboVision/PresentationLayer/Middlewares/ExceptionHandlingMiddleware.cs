using System.Net;
using VerboVision.PresentationLayer.Dto.Exceptions;

namespace VerboVision.PresentationLayer.Middlewares
{
    /// <summary>
    /// Middleware для глобальной обработки исключений и унификации ответов при ошибках
    /// </summary>
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        /// <summary>
        /// Флаг для включения/отключения вывода StackTrace в ответе
        /// </summary>
        private const bool INCLUDE_STACK_TRACE = false;

        /// <summary>
        /// Обрабатывает входящий HTTP-запрос и перехватывает исключения
        /// </summary>
        /// <param name="context">Контекст HTTP-запроса</param>
        /// <returns>Асинхронная задача</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Выполнение следующего middleware в конвейере
                await next(context);

                // Обработка случая, когда запрашиваемый ресурс не найден
                if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                {
                    await HandleExceptionAsync(context, HttpStatusCode.NotFound.ToString(), null, HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {
                // Определяем HTTP-статус код в зависимости от типа исключения
                var statusCode = ex is ArgumentException ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError;

                await HandleExceptionAsync(context, ex.Message, ex.StackTrace, statusCode);
            }
        }

        /// <summary>
        /// Формирует унифицированный JSON-ответ с информацией об ошибке
        /// </summary>
        /// <param name="context">Контекст HTTP-запроса</param>
        /// <param name="exMessage">Сообщение об ошибке</param>
        /// <param name="stackTrace">Стек вызовов (опционально)</param>
        /// <param name="statusCode">HTTP-статус код</param>
        /// <returns>Асинхронная задача</returns>
#pragma warning disable IDE0060
        private async Task HandleExceptionAsync(HttpContext context, string exMessage, string? stackTrace, HttpStatusCode statusCode)
        {
            // Логирование ошибки
            var logMessage = INCLUDE_STACK_TRACE && stackTrace != null
                ? $"{exMessage}{Environment.NewLine}{stackTrace}"
                : exMessage;

            logger.LogError("{LogMessage}", logMessage);

            // Настройка ответа
            var response = context.Response;
            response.ContentType = "application/json";
            response.StatusCode = (int)statusCode;

            // Создание DTO с информацией об ошибке
            var errorInfo = new ExceptionInfoDto
            {
                StatusCode = (int)statusCode,
                Message = exMessage,
                StackTrace = INCLUDE_STACK_TRACE ? stackTrace : null
            };

            // Отправка JSON-ответа
            await response.WriteAsJsonAsync(errorInfo);
        }
#pragma warning restore IDE0060
    }
}