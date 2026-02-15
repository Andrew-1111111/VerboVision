using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using VerboVision.DataLayer.DB.Models;
using VerboVision.DataLayer.Dto;

namespace VerboVision.DataLayer.DB.Configurations
{
    /// <summary>
    /// Конфигурация Entity Framework Core для сущности ImageEntity
    /// </summary>
    public class ImageConfiguration : IEntityTypeConfiguration<ImageEntity>
    {
        /// <summary>
        /// Настройки сериализации JSON для Entity Framework Core
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            // Игнорирует регистр символов при десериализации JSON
            PropertyNameCaseInsensitive = true,

            // Отключает форматирование JSON с отступами для экономии места в БД
            WriteIndented = false
        };

        /// <summary>
        /// Настраивает схему, таблицу, индексы и ограничения для сущности ImageEntity
        /// </summary>
        /// <param name="builder">Построитель конфигурации для сущности ImageEntity</param>
        public void Configure(EntityTypeBuilder<ImageEntity> builder)
        {
            // Название таблицы
            builder.ToTable("images");

            // Первичный ключ
            builder.HasKey(e => e.Id);

            // Конфигурация Id - автоматическая генерация UUID в PostgreSQL
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedOnAdd();

            // Конфигурация URL
            builder.Property(e => e.Url)
                .HasColumnName("url")
                .HasColumnType("varchar(2048)")
                .HasMaxLength(ImageEntity.MAX_URL_LENGTH)
                .IsRequired();

            // Конфигурация FileName
            builder.Property(e => e.FileName)
                .HasColumnName("file_name")
                .HasColumnType("varchar(255)")
                .HasMaxLength(ImageEntity.MAX_FILE_NAME_LENGTH)
                .IsRequired();

            // Конфигурация FileSha256Hash
            builder.Property(e => e.FileSha256Hash)
                .HasColumnName("file_sha256_hash")
                .HasColumnType("char(64)")
                .HasMaxLength(ImageEntity.FILE_HASH_LENGTH)
                .IsFixedLength()
                .IsRequired();

            // Конфигурация FileId
            builder.Property(e => e.FileId)
                .HasColumnName("file_id")
                .HasColumnType("uuid");

            // Конфигурация AiResponse
            builder.Property(e => e.AiResponse)
                .HasColumnName("ai_response")
                .HasColumnType("text");

            // CoreSubjects - хранение в формате JSONB
            builder.Property(e => e.CoreSubjects)
                .HasColumnName("core_subjects")
                .HasColumnType("jsonb")
                .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<CoreSubjectsWrapper>(v, _jsonOptions) ?? new CoreSubjectsWrapper());

            #region Индексы (Indexes)
            // Индекс для быстрого поиска по URL
            builder.HasIndex(e => e.Url)
                .HasDatabaseName("ix_images_url")
                .HasMethod("btree");

            // Индекс для быстрого поиска по SHA-256 хешу файла (уникальный)
            builder.HasIndex(e => e.FileSha256Hash)
                .HasDatabaseName("ix_images_file_sha256_hash")
                .HasMethod("btree")
                .IsUnique();

            // GIN индекс для эффективного поиска по JSONB полю CoreSubjects
            builder.HasIndex(e => e.CoreSubjects)
                .HasDatabaseName("ix_images_core_subjects")
                .HasMethod("gin");
            #endregion

            #region Ограничения (Constraints)
            // Проверка длины URL
            builder.ToTable(b => b.HasCheckConstraint(
                "ck_images_url_length",
                "LENGTH(url) BETWEEN 11 AND 2048"
            ));

            // Проверка длины имени файла
            builder.ToTable(b => b.HasCheckConstraint(
                "ck_images_file_name_length",
                "LENGTH(file_name) BETWEEN 3 AND 255"
            ));

            // Проверка длины хеша файла
            builder.ToTable(b => b.HasCheckConstraint(
                "ck_images_file_sha256_hash_length",
                "LENGTH(file_sha256_hash) = 64"
            ));

            // Проверка размера AiResponse
            builder.ToTable(b => b.HasCheckConstraint(
                "ck_images_ai_response_size",
                "LENGTH(ai_response) <= 100000"
            ));
            #endregion
        }
    }
}