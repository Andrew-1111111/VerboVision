using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VerboVision.DataLayer.Dto;

namespace VerboVision.DataLayer.DB.Models
{
    /// <summary>
    /// Сущность изображения, представляющая запись в системе
    /// </summary>
    public sealed class ImageEntity
    {
        #region Константы
        /// <summary>
        /// Минимальная длина URL (http://a.b)
        /// </summary>
        public const int MIN_URL_LENGTH = 11;

        /// <summary>
        /// Максимальная длина URL (стандартный лимит браузеров)
        /// </summary>
        public const int MAX_URL_LENGTH = 2048;

        /// <summary>
        /// Минимальная длина имени файла
        /// </summary>
        public const int MIN_FILE_NAME_LENGTH = 3;

        /// <summary>
        /// Максимальная длина имени файла
        /// </summary>
        public const int MAX_FILE_NAME_LENGTH = 255;

        /// <summary>
        /// Длина хеша файла
        /// </summary>
        public const int FILE_HASH_LENGTH = 64;

        /// <summary>
        /// Максимальная длина ответа AI (100 000 символов)
        /// </summary>
        public const int MAX_TEXT_LENGTH = 100_000;
        #endregion

        #region Свойства
        /// <summary>
        /// Уникальный идентификатор запроса
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id", TypeName = "uuid")]
        public Guid Id { get; set; }

        /// <summary>
        /// Ссылка на изображение
        /// </summary>
        [Required]
        [MinLength(MIN_URL_LENGTH)]
        [MaxLength(MAX_URL_LENGTH)]
        [Column("url", TypeName = "varchar(2048)")]
        public string Url { get; set; } = null!;

        /// <summary>
        /// Название файла
        /// </summary>
        [Required]
        [MinLength(MIN_FILE_NAME_LENGTH)]
        [MaxLength(MAX_FILE_NAME_LENGTH)]
        [Column("file_name", TypeName = "varchar(255)")]
        public string FileName { get; set; } = null!;

        /// <summary>
        /// SHA 256 хеш файла
        /// </summary>
        [Required]
        [MinLength(FILE_HASH_LENGTH)]
        [MaxLength(FILE_HASH_LENGTH)]
        [Column("file_sha256_hash", TypeName = "char(64)")]
        public string FileSha256Hash { get; set; } = null!;

        /// <summary>
        /// ID изображения на платформе AI
        /// </summary>
        [Required]
        [Column("file_id", TypeName = "uuid")]
        public Guid? FileId { get; set; }

        /// <summary>
        /// Ответ от облачного AI сервиса
        /// </summary>
        [MaxLength(MAX_TEXT_LENGTH)]
        [Column("ai_response", TypeName = "text")]
        public string? AiResponse { get; set; }

        /// <summary>
        /// Список основных предметов (хранится как JSON)
        /// </summary>
        [Column("core_subjects", TypeName = "jsonb")]
        public CoreSubjectsWrapper? CoreSubjects { get; set; } = new();
        #endregion

        /// <summary>
        /// Конструктор без параметров для Entity Framework
        /// </summary>
        private ImageEntity() 
        { 
        }

        /// <summary>
        /// Конструктор для создания новой записи изображения
        /// </summary>
        /// <param name="url">URL-адрес изображения</param>
        /// <param name="fileName">Имя файла</param>
        /// <param name="fileSha256Hash">SHA-256 хеш файла (64 символа)</param>
        /// <param name="fileId">ID файла в GigaChat (может быть null)</param>
        /// <param name="aiResponse">Ответ от AI (может быть null)</param>
        /// <param name="coreSubjects">Распознанные предметы (может быть null)</param>
        /// <exception cref="ArgumentNullException">Если обязательные параметры не указаны</exception>
        /// <exception cref="ArgumentException">
        /// Если хеш имеет неверную длину, неверный формат GUID или превышены ограничения длины
        /// </exception>
        public ImageEntity(string url,
            string fileName,
            string fileSha256Hash,
            string? fileId = null,
            string? aiResponse = null,
            CoreSubjectsWrapper? coreSubjects = null)
        {
            // Проверка обязательных параметров
            Url = url ?? throw new ArgumentNullException(nameof(url));
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            FileSha256Hash = fileSha256Hash ?? throw new ArgumentNullException(nameof(fileSha256Hash));

            // Проверка длины хеша
            if (fileSha256Hash.Length != FILE_HASH_LENGTH)
                throw new ArgumentException($"Хеш должен быть длиной {FILE_HASH_LENGTH} символов", nameof(fileSha256Hash));

            // Проверка длины URL
            if (url.Length < MIN_URL_LENGTH || url.Length > MAX_URL_LENGTH)
                throw new ArgumentException($"URL должен быть от {MIN_URL_LENGTH} до {MAX_URL_LENGTH} символов", nameof(url));

            // Проверка имени файла
            if (fileName.Length < MIN_FILE_NAME_LENGTH || fileName.Length > MAX_FILE_NAME_LENGTH)
                throw new ArgumentException($"Имя файла должно быть от {MIN_FILE_NAME_LENGTH} до {MAX_FILE_NAME_LENGTH} символов", nameof(fileName));

            // Парсинг GUID
            if (!string.IsNullOrWhiteSpace(fileId))
            {
                if (!Guid.TryParse(fileId, out var guid))
                    throw new ArgumentException($"Неверный формат GUID: {fileId}", nameof(fileId));
                FileId = guid;
            }

            // Проверка длины ответа AI
            if (aiResponse?.Length > MAX_TEXT_LENGTH)
                throw new ArgumentException($"Ответ AI не может превышать {MAX_TEXT_LENGTH} символов", nameof(aiResponse));

            AiResponse = aiResponse;
            CoreSubjects = coreSubjects;
        }
    }
}