using System.Text.Json.Serialization;

namespace VerboVision.DataLayer.AI.Internal.Core
{
    /// <summary>
    /// Представляет ответ от API при запросе к chat/completions.
    /// </summary>
    public class ChatCompletionResponse
    {
        /// <summary>
        /// Список вариантов ответа от модели
        /// </summary>
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }
}