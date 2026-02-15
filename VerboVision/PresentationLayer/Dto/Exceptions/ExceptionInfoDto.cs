using System.Text.Json.Serialization;

namespace VerboVision.PresentationLayer.Dto.Exceptions
{
    /// <summary>
    /// DTO для передачи информации об исключении в HTTP-ответе
    /// </summary>
    internal struct ExceptionInfoDto
    {
        /// <summary>
        /// Инициализирует новый экземпляр структуры ExceptionInfoDto
        /// </summary>
        public ExceptionInfoDto()
        {
        }

        /// <summary>
        /// HTTP-статус код ошибки
        /// </summary>
        /// <example>400, 404, 500</example>
        [JsonPropertyOrder(-3)]
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        /// <example>Входная строка имела неверный формат</example>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyOrder(-2)]
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Стек вызовов
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyOrder(-1)]
        [JsonPropertyName("stackTrace")]
        public string? StackTrace { get; set; }
    }
}