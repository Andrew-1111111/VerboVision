using VerboVision.DataLayer.Helper.Enums;

namespace VerboVision.DataLayer.Helper
{
    /// <summary>
    /// Вспомогательный класс для работы с Entity Framework и PostgreSQL
    /// </summary>
    public static class EfHelper
    {
        /// <summary>
        /// Создаёт паттерн для LIKE/ILIKE поиска с автоматическим экранированием
        /// </summary>
        /// <param name="searchTerm">Поисковый запрос</param>
        /// <param name="matchMode">Режим поиска (Начинается с, Содержит, Заканчивается на, Точное совпадение)</param>
        /// <returns>Паттерн для использования в LIKE/ILIKE</returns>
        /// <example>
        /// EfHelper.CreateLikePattern("test", LikeMatchMode.Contains);   // "%test%"
        /// EfHelper.CreateLikePattern("test", LikeMatchMode.StartsWith); // "test%"
        /// EfHelper.CreateLikePattern("test", LikeMatchMode.EndsWith);   // "%test"
        /// EfHelper.CreateLikePattern("test", LikeMatchMode.Exact);      // "test"
        /// </example>
        public static string CreateLikePattern(string searchTerm, LikeMatchMode matchMode = LikeMatchMode.Contains)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return string.Empty;

            var escaped = EscapeLike(searchTerm);

            return matchMode switch
            {
                LikeMatchMode.StartsWith => $"{escaped}%",
                LikeMatchMode.EndsWith => $"%{escaped}",
                LikeMatchMode.Exact => escaped,
                _ => $"%{escaped}%" // Contains (по умолчанию)
            };
        }

        /// <summary>
        /// Экранирует специальные символы для использования в операторе LIKE/ILIKE
        /// </summary>
        /// <param name="pattern">Исходная строка поиска</param>
        /// <returns>Строка с экранированными символами %, _, \</returns>
        /// <example>
        /// Вход: "100%" -> Выход: "100\%"
        /// Вход: "test_123" -> Выход: "test\_123"
        /// Вход: "C:\temp" -> Выход: "C:\\temp"
        /// </example>
        public static string EscapeLike(string pattern)
        {
            // Возвращаем пустую строку, если входная строка null или пуста
            if (string.IsNullOrEmpty(pattern))
                return string.Empty;

            // Важен порядок замены: сначала экранируем обратную косую черту,
            // затем остальные специальные символы
            return pattern
                .Replace("\\", "\\\\") // Экранируем обратную косую черту
                .Replace("%", "\\%")   // Экранируем знак процента (любая последовательность символов)
                .Replace("_", "\\_");  // Экранируем подчёркивание (один любой символ)
        }
    }
}