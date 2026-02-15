namespace VerboVision.DataLayer.Dto
{
    /// <summary>
    /// Основной предмет/объект, состоящий из нескольких материалов
    /// </summary>
    public class CoreSubjectDto
    {
        /// <summary>
        /// Название предмета/объекта
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Список материалов, из которых состоит предмет
        /// </summary>
        public List<MaterialDto> Materials { get; set; } = [];

        /// <summary>
        /// Добавляет материал к предмету
        /// </summary>
        public void AddMaterial(MaterialDto material)
        {
            if (material != null)
                Materials.Add(material);
        }

        /// <summary>
        /// Добавляет несколько материалов к предмету
        /// </summary>
        public void AddMaterials(IEnumerable<MaterialDto> materials)
        {
            if (materials != null)
                Materials.AddRange(materials);
        }

        /// <summary>
        /// Возвращает названия всех материалов через запятую
        /// </summary>
        public string GetMaterialsSummary()
        {
            return string.Join(", ", Materials.Select(m => m.Name));
        }

        /// <summary>
        /// Проверяет, содержит ли предмет материал с указанным названием
        /// </summary>
        public bool ContainsMaterial(string materialName)
        {
            return Materials.Any(m => m.Name.Equals(materialName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Строковое представление предмета
        /// </summary>
        public override string ToString()
        {
            if (Materials.Count == 0)
                return Name;

            var materialsList = string.Join(", ", Materials.Select(m => m.Name));
            return $"{Name}: {materialsList}";
        }

        /// <summary>
        /// Парсит строку формата "объект: материал1, материал2" в объект CoreSubject
        /// </summary>
        /// <param name="input">Входная строка, например: "стул: дерево, металл, пластик"</param>
        /// <returns>Объект CoreSubject с заполненными Name и Materials</returns>
        /// <exception cref="ArgumentException">Выбрасывается при некорректном формате</exception>
        public static CoreSubjectDto Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Входная строка не может быть пустой", nameof(input));

            // Разделяем на объект и материалы по первому двоеточию
            var parts = input.Split(':', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
                throw new ArgumentException("Строка должна содержать двоеточие, разделяющее объект и материалы", nameof(input));

            var coreSubject = new CoreSubjectDto
            {
                Name = parts[0].Trim()
            };

            // Разделяем материалы по запятой
            var materialNames = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var materialName in materialNames)
            {
                if (!string.IsNullOrWhiteSpace(materialName))
                {
                    coreSubject.AddMaterial(new MaterialDto { Name = materialName });
                }
            }

            return coreSubject;
        }

        /// <summary>
        /// Пытается распарсить строку формата "объект: материал1, материал2" в объект CoreSubject
        /// </summary>
        /// <param name="input">Входная строка</param>
        /// <param name="result">Результат парсинга, если успешно</param>
        /// <returns>true, если парсинг успешен, иначе false</returns>
        public static bool TryParse(string input, out CoreSubjectDto? result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            try
            {
                result = Parse(input);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}