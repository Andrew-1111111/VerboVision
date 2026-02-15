namespace VerboVision.DataLayer.Helper
{
    /// <summary>
    /// Вспомогательный класс для работы с WEB запросами
    /// </summary>
    public static class WebHelper
    {
        /// <summary>
        /// Очищает имя файла от недопустимых символов и применяет все необходимые проверки безопасности
        /// </summary>
        /// <param name="fileName">Исходное имя файла</param>
        /// <returns>Безопасное имя файла, готовое к использованию в файловой системе</returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если fileName равен null</exception>
        /// <exception cref="ArgumentException">Выбрасывается, если после очистки имя пустое</exception>
        public static string CleanFileName(string fileName)
        {
            // 1. Проверка на null
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName), "Имя файла не может быть null");

            // 2. Проверка на пустую строку или только пробелы
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Имя файла не может быть пустым или состоять только из пробелов", nameof(fileName));

            var originalFileName = fileName;

            // 3. Удаление недопустимых символов файловой системы
            var invalidChars = Path.GetInvalidFileNameChars();
            var fileNameParts = fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries);

            if (fileNameParts.Length == 0)
                throw new ArgumentException("Имя файла не содержит допустимых символов после очистки", nameof(fileName));

            fileName = string.Join("_", fileNameParts);

            // 4. Удаление потенциально опасных последовательностей
            fileName = fileName
                .Replace("..", "_")             // Предотвращает path traversal (выход за пределы директории)
                .Replace("./", "_")             // Предотвращает относительные пути
                .Replace(".\\", "_")
                .Replace("//", "_")             // Удаляет двойные слеши
                .Replace("\\\\", "_")           // Удаляет двойные обратные слеши
                .Replace("__", "_")             // Убирает двойные подчеркивания
                .Replace("_.", "_")             // Убирает комбинации с точкой
                .Replace("._", "_");

            // 5. Удаление управляющих символов ASCII (0-31)
            fileName = new string([.. fileName.Where(c => c >= 32 || c == '\t' || c == '\n' || c == '\r')]);

            // 6. Удаление непечатаемых символов Unicode
            fileName = new string([.. fileName.Where(c => !char.IsControl(c))]);

            // 7. Проверка на зарезервированные имена устройств Windows
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
            var reservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8",
                "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

            if (reservedNames.Contains(nameWithoutExt))
            {
                fileName = $"_{fileName}";
            }

            // 8. Проверка на скрытые файлы (начинающиеся с точки)
            if (fileName.StartsWith('.'))
            {
                fileName = "_" + fileName.TrimStart('.');
            }

            // 9. Проверка на имена, заканчивающиеся точкой или пробелом (проблемно в Windows)
            if (fileName.EndsWith('.') || fileName.EndsWith(' '))
            {
                fileName = fileName.TrimEnd('.', ' ') + "_";
            }

            // 10. Проверка на имена, начинающиеся с пробела
            if (fileName.StartsWith(' '))
            {
                fileName = "_" + fileName.TrimStart();
            }

            // 11. Проверка на несколько точек подряд
            while (fileName.Contains(".."))
            {
                fileName = fileName.Replace("..", ".");
            }

            // 12. Ограничение максимальной длины (255 для Windows)
            const int MAX_FILENAME_LENGTH = 255;
            if (fileName.Length > MAX_FILENAME_LENGTH)
            {
                var extension = Path.GetExtension(fileName);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                var maxNameLength = MAX_FILENAME_LENGTH - extension.Length;
                if (maxNameLength > 0)
                {
                    nameWithoutExtension = nameWithoutExtension[..Math.Min(nameWithoutExtension.Length, maxNameLength)];
                    fileName = nameWithoutExtension + extension;
                }
                else
                {
                    fileName = fileName[..MAX_FILENAME_LENGTH];
                }
            }

            // 13. Проверка минимальной длины
            const int MIN_FILENAME_LENGTH = 1;
            if (fileName.Length < MIN_FILENAME_LENGTH)
            {
                throw new ArgumentException($"После очистки имя файла слишком короткое: {originalFileName}", nameof(fileName));
            }

            // 14. Удаление лишних пробелов в начале и конце
            fileName = fileName.Trim();

            // 15. Замена пробелов на подчеркивания (опционально, для совместимости)
            fileName = fileName.Replace(' ', '_');

            // 16. Проверка на пустое имя после всех преобразований
            if (string.IsNullOrWhiteSpace(fileName))
            {
                // Генерируем имя по умолчанию
                fileName = $"file_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            }

            // 17. Дополнительная проверка на недопустимые символы (на всякий случай)
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                // Если вдруг что-то пропустили - рекурсивно очищаем ещё раз
                return CleanFileName(fileName);
            }

            return fileName;
        }

        /// <summary>
        /// Возвращает расширение файла на основе MIME-типа
        /// </summary>
        /// <param name="contentType">MIME-тип содержимого</param>
        /// <returns>Расширение файла с точкой (например, ".jpg") или ".bin" по умолчанию</returns>
        public static string GetExtensionFromContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return ".bin";

            // Убираем параметры (например, "; charset=utf-8")
            var cleanType = contentType.Split(';')[0].Trim().ToLowerInvariant();

            return cleanType switch
            {
                // Изображения (Image)
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/webp" => ".webp",
                "image/tiff" or "image/tif" => ".tiff",
                "image/avif" => ".avif",            // AVIF изображения
                "image/apng" => ".apng",            // Анимированные PNG
                "image/svg+xml" => ".svg",          // Векторная графика
                "image/x-icon" or "image/vnd.microsoft.icon" => ".ico", // Иконки
                "image/heic" or "image/heif" => ".heic", // HEIC/HEIF (современные форматы)

                // Документы (Documents)
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                "application/vnd.ms-excel" => ".xls",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                "application/vnd.ms-powerpoint" => ".ppt",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
                "text/plain" => ".txt",
                "text/csv" => ".csv",
                "text/html" or "application/xhtml+xml" => ".html",
                "application/rtf" => ".rtf",        // Rich Text Format
                "application/epub+zip" => ".epub",  // Электронные книги
                "application/x-abiword" => ".abw",  // AbiWord документы

                // Аудио (Audio)
                "audio/mpeg" or "audio/mp3" => ".mp3",
                "audio/mp4" => ".m4a",
                "audio/wav" or "audio/x-wav" => ".wav",
                "audio/ogg" or "audio/opus" => ".ogg",
                "audio/webm" => ".weba",
                "audio/aac" => ".aac",              // AAC аудио
                "audio/flac" => ".flac",            // FLAC аудио
                "audio/midi" or "audio/x-midi" => ".midi", // MIDI

                // Видео
                "video/mp4" => ".mp4",
                "video/webm" => ".webm",
                "video/ogg" => ".ogv",
                "video/mpeg" => ".mpeg",
                "video/x-msvideo" or "video/avi" => ".avi",
                "video/quicktime" => ".mov",
                "video/3gpp" => ".3gp",
                "video/3gpp2" => ".3g2",
                "video/x-matroska" or "video/mkv" => ".mkv",

                // Архивы
                "application/zip" => ".zip",
                "application/x-zip-compressed" => ".zip",    // Нестандартный, но часто используется
                "application/x-7z-compressed" => ".7z",
                "application/x-rar" or "application/vnd.rar" => ".rar",
                "application/gzip" or "application/x-gzip" => ".gz",
                "application/x-bzip2" => ".bz2",
                "application/x-tar" => ".tar",
                "application/x-freearc" => ".arc",           // ARC архив

                // Шрифты
                "font/ttf" or "application/x-font-ttf" => ".ttf",
                "font/otf" => ".otf",
                "font/woff" => ".woff",
                "font/woff2" => ".woff2",
                "application/vnd.ms-fontobject" => ".eot",   // Embedded OpenType

                // JSON и структурированные данные
                "application/json" => ".json",
                "application/ld+json" => ".jsonld",
                "application/xml" or "text/xml" => ".xml",
                "application/yaml" or "text/yaml" => ".yaml",
                "application/toml" => ".toml",                // TOML конфиги
                "text/markdown" => ".md",                     // Markdown
                "text/css" => ".css",                         // Стили
                "text/javascript" or "application/javascript" => ".js", // JavaScript

                // Специфические форматы
                "application/java-archive" => ".jar",
                "application/x-shockwave-flash" => ".swf",
                "application/postscript" => ".ps",            // PostScript
                "application/octet-stream" => ".bin",         // Бинарные данные

                // По умолчанию
                _ => ".bin"
            };
        }

    }
}