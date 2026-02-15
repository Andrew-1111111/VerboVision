namespace VerboVision.DataLayer.AI.Internal.Enums
{
    /// <summary>
    /// Сферы деятельности GigaChat
    /// </summary>
    public enum GigaChatScope : byte
    {
        /// <summary>
        /// Доступ для физических лиц
        /// </summary>
        GIGACHAT_API_PERS = 0x00,

        /// <summary>
        /// Доступ для ИП и юридических лиц по платным пакетам
        /// </summary>
        GIGACHAT_API_B2B = 0x01,

        /// <summary>
        /// Доступ для ИП и юридических лиц по схеме pay-as-you-go
        /// </summary>
        GIGACHAT_API_CORP = 0x02
    }
}
