using System.Security.Cryptography;

namespace VerboVision.DataLayer.Helper
{
    /// <summary>
    /// Предоставляет методы для вычисления криптографических хешей изображений
    /// </summary>
    public static class CryptographicImageHash
    {
        /// <summary>
        /// Вычисляет SHA-256 хеш изображения из потока данных
        /// </summary>
        /// <param name="imageStream">Поток, содержащий данные изображения</param>
        /// <returns>
        /// Строка, содержащая SHA-256 хеш в шестнадцатеричном формате (64 символа в нижнем регистре)
        /// </returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="imageStream"/> равен null</exception>
        /// <exception cref="CryptographicException">Выбрасывается при ошибке вычисления хеша</exception>
        public static string ComputeSha256Hash(Stream imageStream)
        {
            // Проверка на null
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream), "Поток изображения не может быть null");

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(imageStream);

            // Convert.ToHexString доступен с .NET 5
            // Для более старых версий используйте BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Вычисляет SHA-256 хеш изображения из массива байтов
        /// </summary>
        /// <param name="imageBytes">Массив байтов изображения</param>
        /// <returns>SHA-256 хеш в шестнадцатеричном формате (64 символа в нижнем регистре)</returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="imageBytes"/> равен null</exception>
        public static string ComputeSha256Hash(byte[] imageBytes)
        {
            if (imageBytes == null)
                throw new ArgumentNullException(nameof(imageBytes), "Массив байтов изображения не может быть null");

            using var stream = new MemoryStream(imageBytes);
            return ComputeSha256Hash(stream);
        }

        /// <summary>
        /// Вычисляет SHA-256 хеш изображения из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу изображения</param>
        /// <returns>SHA-256 хеш в шестнадцатеричном формате (64 символа в нижнем регистре)</returns>
        /// <exception cref="ArgumentException">Выбрасывается, если путь к файлу пуст или невалиден</exception>
        /// <exception cref="FileNotFoundException">Выбрасывается, если файл не найден</exception>
        public static string ComputeSha256Hash(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}", filePath);

            using var stream = File.OpenRead(filePath);
            return ComputeSha256Hash(stream);
        }
    }
}