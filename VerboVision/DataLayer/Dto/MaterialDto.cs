namespace VerboVision.DataLayer.Dto
{
    /// <summary>
    /// Материал, из которого состоит предмет
    /// </summary>
    public class MaterialDto
    {
        /// <summary>
        /// Название материала
        /// </summary>
        /// <example>Дерево, Металл, Пластик, Стекло...</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Строковое представление материала
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
