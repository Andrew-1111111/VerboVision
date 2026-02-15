using System.Text.Json.Serialization;

namespace VerboVision.DataLayer.AI.Internal
{
    /// <summary>
    /// Представляет ответ от API при загрузке файла
    /// </summary>
    public class FileUploadResponse
    {
        /// <summary>
        /// Уникальный идентификатор загруженного файла
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Размер файла в байтах
        /// </summary>
        [JsonPropertyName("bytes")]
        public int? Bytes { get; set; }

        /// <summary>
        /// Временная метка создания файла (Unix timestamp)
        /// </summary>
        [JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }

        /// <summary>
        /// Имя файла.
        /// </summary>
        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        /// <summary>
        /// Назначение файла
        /// </summary>
        [JsonPropertyName("purpose")]
        public string? Purpose { get; set; }
    }
}