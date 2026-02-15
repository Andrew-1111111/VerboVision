using System.Text.Json.Serialization;

namespace VerboVision.DataLayer.AI.Internal
{
    /// <summary>
    /// Представляет сообщение в диалоге
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Роль отправителя (обычно "assistant" для ответов модели)
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// Текстовое содержимое сообщения
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}