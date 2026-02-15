using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VerboVision.DataLayer.Dto;

namespace VerboVision.PresentationLayer.Dto.Api
{
    /// <summary>
    /// DTO для ответа после загрузки изображения по URL
    /// </summary>
    public class UploadImgDto
    {
        /// <summary>
        /// Уникальный идентификатор запроса
        /// </summary>
        [Required]
        [JsonPropertyName("ID")]
        public Guid? Id { get; set; }

        /// <summary>
        /// Список основных предметов/объектов, распознанных на изображении
        /// </summary>
        [Required]
        [JsonPropertyName("CoreSubjects")]
        public CoreSubjectsWrapper CoreSubjects { get; set; } = new();
    }
}