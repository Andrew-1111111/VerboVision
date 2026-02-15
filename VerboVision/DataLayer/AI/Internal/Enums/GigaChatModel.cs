namespace VerboVision.DataLayer.AI.Internal.Enums
{
    /// <summary>
    /// Доступные модели GigaChat
    /// </summary>
    public struct GigaChatModel
    {
        /// <summary>
        /// Базовая модель GigaChat (перенаправляется на GigaChat-2) 
        /// </summary>
        public const string GIGA_CHAT = "GigaChat"; // не поддерживает изображения!

        /// <summary>
        /// Улучшенная модель GigaChat-Pro (перенаправляется на GigaChat-2-Pro)
        /// </summary>
        public const string GIGA_CHAT_PRO = "GigaChat-Pro";

        /// <summary>
        /// Мощная модель GigaChat-Max (перенаправляется на GigaChat-2-Max)
        /// </summary>
        public const string GIGA_CHAT_MAX = "GigaChat-Max";

        /// <summary>
        /// Модель для работы с большими контекстами
        /// </summary>
        public const string GIGA_CHAT_PLUS = "GigaChat-Plus"; // не поддерживает изображения!

        /// <summary>
        /// Модель второго поколения — быстрая и легкая для повседневных задач
        /// </summary>
        public const string GIGA_CHAT_2 = "GigaChat-2"; // не поддерживает изображения!

        /// <summary>
        /// Модель второго поколения — для ресурсоемких задач, максимальная эффективность
        /// </summary>
        public const string GIGA_CHAT_2_PRO = "GigaChat-2-Pro";

        /// <summary>
        /// Модель второго поколения — для самых сложных и масштабных задач
        /// </summary>
        public const string GIGA_CHAT_2_MAX = "GigaChat-2-Max";
    }
}