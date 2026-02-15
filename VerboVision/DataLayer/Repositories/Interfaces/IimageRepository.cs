using VerboVision.DataLayer.Dto;

namespace VerboVision.DataLayer.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с изображениями, предоставляет методы для скачивания изображений по URL и работу с AI
    /// </summary>
    public interface IImageRepository
    {
        /// <summary>
        /// Анализирует список пользовательских предметов, определяет материалы через GigaChat и сохраняет результаты в базу данных
        /// </summary>
        /// <param name="autorizationKey">Ключ авторизации для доступа к GigaChat API</param>
        /// <param name="imageId">Уникальный идентификатор изображения в базе данных</param>
        /// <param name="userSubjects">Список названий предметов, введённых пользователем для анализа</param>
        /// <param name="cToken">Токен отмены операции (по умолчанию CancellationToken.None)</param>
        /// <returns>Оболочка <see cref="CoreSubjectsWrapper"/> с распознанными предметами и материалами.
        /// В случае ошибки возвращает пустую оболочку.</returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если imageId равен Guid.Empty или userSubjects пуст</exception>
        Task<CoreSubjectsWrapper> AnalyzeSubjectsAsync(string autorizationKey, Guid imageId, List<string> userSubjects, CancellationToken cToken = default);

        /// <summary>
        /// Анализирует изображение по URL: скачивает, вычисляет хеш, отправляет в GigaChat и сохраняет в БД
        /// </summary>
        /// <param name="autorizationKey">Ключ авторизации GigaChat</param>
        /// <param name="url">URL изображения для анализа</param>
        /// <param name="cToken">Токен отмены операции</param>
        /// <returns>Кортеж с UUID записи в БД и распознанными объектами</returns>
        Task<(Guid? Uuid, CoreSubjectsWrapper CoreSubjects)> AnalyzeImageAsync(string autorizationKey, string url, CancellationToken cToken = default);
    }
}