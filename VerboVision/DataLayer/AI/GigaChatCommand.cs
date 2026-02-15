using VerboVision.DataLayer.AI.Internal.Core;
using VerboVision.DataLayer.AI.Internal.Enums;
using VerboVision.DataLayer.Dto;

namespace VerboVision.DataLayer.AI
{
    /// <summary>
    /// Класс для взаимодействия с GigaChat API
    /// </summary>
    public static class GigaChatCommand
    {
        #region Промпты
        /// <summary>
        /// Возвращает промпт для отправки в GigaChat с целью анализа изображения и определения основных объектов 
        /// и материалов, из которых эти объекты изготовлены
        /// </summary>
        /// <returns>Строка промпта с инструкциями для анализа изображения</returns>
        public static string GetSubjectsPrompt()
        {
            return
@"Проанализируй объекты на изображении, выдели все важные материальные объекты (не людей, не животных или других живых существ). 
Выдели материалы (исходя из наиболее вероятного типа материала, относительно назначения объекта и требуемой прочности), из которых они состоят. 
Ответы списком в формате: объект:материал. Материалы выводи через запятую. 
Если ничего не найдешь, выведи: ""Объекты не найдены:Материалы не найдены"". Ответь кратко.";
        }

        /// <summary>
        /// Формирует промпт для отправки в GigaChat с целью определения материалов указанных предметов
        /// </summary>
        /// <param name="subjects">Список предметов для анализа</param>
        /// <returns>Строка промпта, содержащая инструкции для AI и список предметов для анализа</returns>
        public static string GetMaterialsPrompt(List<string> subjects)
        {
            var subjectsList = string.Join(", ", subjects);

            return
$@"Проанализируй и определи, из каких материалов сделаны следующие предметы: {subjectsList}.
Для каждого предмета верни ответ строго в формате: Название предмета: материал1, материал2, материал3
Например:
Ежедневник: бумага, картон, клей
Ручка: пластик, металл, чернила

Предметы для анализа:
{subjectsList}

Ответь кратко.";
        }
        #endregion

        /// <summary>
        /// Отправляет текстовый запрос в GigaChat для определения материалов указанных предметов
        /// </summary>
        /// <param name="authorizationKey">Ключ авторизации для доступа к GigaChat API</param>
        /// <param name="coreSubjects">Список предметов для анализа</param>
        /// <returns>Текстовый ответ от GigaChat с определёнными материалами</returns>
        /// <exception cref="ArgumentNullException">
        /// Выбрасывается, если ключ авторизации или имя файла не указаны
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Выбрасывается, если список предметов пуст
        /// </exception>
        public static async Task<string> CheckTextAsync(string authorizationKey, List<string> coreSubjects)
        {
            if (string.IsNullOrWhiteSpace(authorizationKey))
                throw new ArgumentNullException(nameof(authorizationKey), "Ключ авторизации не может быть пустым");

            if (coreSubjects.Count == 0)
                throw new ArgumentNullException(nameof(coreSubjects), "Объекты не могут быть пустыми");

            // Создание клиента и выполнение запросов
            using var gigaChatClient = await GigaChatClient.CreateAsync(authorizationKey, GigaChatScope.GIGACHAT_API_PERS);

            // Генерация промпта из списка предметов и отправка в GigaChat
            return await gigaChatClient.SendPromptAsync(GetMaterialsPrompt(coreSubjects), GigaChatModel.GIGA_CHAT_MAX);
        }

        /// <summary>
        /// Выполняет анализ изображения через GigaChat API
        /// </summary>
        /// <param name="authorizationKey">Ключ авторизации GigaChat</param>
        /// <param name="imageBytes">Байты изображения</param>
        /// <param name="fileName">Имя файла</param>
        /// <returns>FileId, ответ AI и распознанный объект</returns>
        /// <exception cref="ArgumentNullException">Если параметры не валидны</exception>
        /// <exception cref="HttpRequestException">При ошибке HTTP</exception>
        /// <exception cref="InvalidOperationException">При ошибке аутентификации</exception>
        public static async Task<(string FileId, string AiResponse, CoreSubjectsWrapper CoreSubjects)> CheckImageAsync(
            string authorizationKey,
            byte[] imageBytes,
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(authorizationKey))
                throw new ArgumentNullException(nameof(authorizationKey), "Ключ авторизации не может быть пустым");

            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentNullException(nameof(imageBytes), "Массив байтов изображения не может быть пустым");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName), "Имя файла не может быть пустым");

            // Создание клиента и выполнение запросов
            using var gigaChatClient = await GigaChatClient.CreateAsync(authorizationKey, GigaChatScope.GIGACHAT_API_PERS);

            var fileId = await gigaChatClient.UploadFileAsync(imageBytes, fileName, ImageMimeType.JPEG);
            var response = await gigaChatClient.AnalyzeFileAsync(GetSubjectsPrompt(), fileId, GigaChatModel.GIGA_CHAT_MAX);

            // Пытаемся распарсить ответ AI как несколько объектов
            var coreSubj = CoreSubjectsWrapper.ParseMultiple(response);

            return (fileId, response, coreSubj);
        }
    }
}
