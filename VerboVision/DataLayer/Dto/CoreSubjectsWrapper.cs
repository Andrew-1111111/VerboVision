using System.Collections;

namespace VerboVision.DataLayer.Dto
{
    /// <summary>
    /// Оболочка для хранения списка основных предметов/объектов
    /// </summary>
    public class CoreSubjectsWrapper
    {
        /// <summary>
        /// Список основных предметов/объектов
        /// </summary>
        public List<CoreSubjectDto> Items { get; set; } = [];

        /// <summary>
        /// Добавляет предмет в список
        /// </summary>
        /// <param name="item">Предмет для добавления</param>
        public void Add(CoreSubjectDto item)
        {
            if (item != null)
                Items.Add(item);
        }

        /// <summary>
        /// Добавляет несколько предметов в список
        /// </summary>
        /// <param name="items">Коллекция предметов</param>
        public void AddRange(IEnumerable<CoreSubjectDto> items)
        {
            if (items != null)
                Items.AddRange(items);
        }

        /// <summary>
        /// Удаляет предмет из списка
        /// </summary>
        /// <param name="item">Предмет для удаления</param>
        /// <returns>true, если предмет был удалён</returns>
        public bool Remove(CoreSubjectDto item)
        {
            return Items.Remove(item);
        }

        /// <summary>
        /// Очищает список
        /// </summary>
        public void Clear()
        {
            Items.Clear();
        }

        /// <summary>
        /// Возвращает количество предметов в списке
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Получает или устанавливает предмет по индексу
        /// </summary>
        public CoreSubjectDto this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        /// <summary>
        /// Парсит строку с несколькими объектами формата "объект: материал1, материал2"
        /// </summary>
        /// <param name="inputString">Входная строка с объектами, разделёнными новой строкой или точкой с запятой</param>
        /// <returns>Оболочка со списком распарсенных объектов</returns>
        public static CoreSubjectsWrapper ParseMultiple(string inputString)
        {
            var wrapper = new CoreSubjectsWrapper();

            if (string.IsNullOrWhiteSpace(inputString))
                return wrapper;

            // Разделяем по переносу строки или точке с запятой
            var lines = inputString.Split(['\n', '\r', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    try
                    {
                        var item = CoreSubjectDto.Parse(line);
                        wrapper.Add(item);
                    }
                    catch
                    {
                        continue; // Пропускаем некорректные строки
                    }
                }
            }

            return wrapper;
        }

        /// <summary>
        /// Парсит список строк, каждая из которых содержит объект в формате: "объект: материал1, материал2"
        /// </summary>
        /// <param name="inputLines">Список строк с объектами для парсинга</param>
        /// <returns>Оболочка со списком распарсенных объектов</returns>
        public static CoreSubjectsWrapper ParseMultiple(List<string> inputLines)
        {
            var wrapper = new CoreSubjectsWrapper();

            if (inputLines == null || inputLines.Count == 0)
                return wrapper;

            foreach (var line in inputLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var trimmedLine = line.Trim();
                try
                {
                    var item = CoreSubjectDto.Parse(trimmedLine);
                    wrapper.Add(item);
                }
                catch
                {
                    continue; // Пропускаем некорректные строки
                }
            }

            return wrapper;
        }

        /// <summary>
        /// Сравнивает два объекта CoreSubjectsWrapper на равенство по содержимому
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not CoreSubjectsWrapper other)
                return false;

            if (Items.Count != other.Items.Count)
                return false;

            for (int i = 0; i < Items.Count; i++)
            {
                if (!Items[i].Equals(other.Items[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Возвращает хеш-код на основе содержимого
        /// </summary>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var item in Items)
                hash.Add(item);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Возвращает строковое представление всех предметов
        /// </summary>
        public override string ToString()
        {
            return $"CoreSubjectsWrapper [{Count} items]";
        }
    }
}