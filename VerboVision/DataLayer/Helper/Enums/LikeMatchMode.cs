namespace VerboVision.DataLayer.Helper.Enums
{
    /// <summary>
    /// Режимы поиска для оператора LIKE/ILIKE
    /// </summary>
    public enum LikeMatchMode
    {
        /// <summary>
        /// Содержит подстроку (по умолчанию) - %текст%
        /// </summary>
        Contains,

        /// <summary>
        /// Начинается с - текст%
        /// </summary>
        StartsWith,

        /// <summary>
        /// Заканчивается на - %текст
        /// </summary>
        EndsWith,

        /// <summary>
        /// Точное совпадение - текст (без масок)
        /// </summary>
        Exact
    }
}