using System.Text.Json.Serialization;

namespace VerboVision.DataLayer.AI.Internal
{
    /// <summary>
    /// Представляет один вариант ответа от модели.
    /// </summary>
    public class Choice
    {
        /// <summary>
        /// Сообщение от модели.
        /// </summary>
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }
}