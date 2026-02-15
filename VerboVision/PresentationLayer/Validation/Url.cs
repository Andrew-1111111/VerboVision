namespace VerboVision.PresentationLayer.Validation
{
    /// <summary>
    /// Класс для валидации URL
    /// </summary>
    public static class UrlValidation
    {
        /// <summary>
        /// Проверяет, является ли строка валидным URL, ведущим к JPG изображению, и очищает её от пробелов
        /// </summary>
        /// <param name="url">URL для проверки (передаётся по ссылке для автоматической очистки)</param>
        /// <returns>true, если URL валидный и ведёт к .jpg файлу; иначе false</returns>
        public static bool IsValidJpgUrl(ref string url)
        {
            // Проверка на пустую или состоящую из пробелов строку
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // Удаляем начальные и конечные пробелы
            url = url.Trim();

            // Пытаемся создать объект Uri из строки
            // Uri.TryCreate не выбрасывает исключение при ошибке, а возвращает false
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult))
                return false;

            // Проверяем, что используется HTTP или HTTPS протокол
            if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
                return false;

            // Получаем расширение файла из пути URL (часть после последней точки)
            // Например: из "https://site.com/image.jpg" получим ".jpg"
            var extension = Path.GetExtension(uriResult.LocalPath);

            // Сравниваем расширение с ".jpg" без учёта регистра
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase);
        }
    }
}