using VerboVision.DataLayer.DB.Context;

namespace VerboVision.DataLayer.DB.Management
{
    /// <summary>
    /// Предоставляет методы для управления жизненным циклом базы данных
    /// </summary>
    public static class DatabaseManager
    {
        /// <summary>
        /// Асинхронно создает базу данных
        /// </summary>
        /// <param name="connectionString">Строка подключения к базе данных</param>
        /// <param name="logger">Логгер для записи событий и ошибок</param>
        /// <exception cref="ArgumentNullException">Выбрасывается, если connectionString равен null или пуст</exception>
        /// <exception cref="InvalidOperationException">Выбрасывается при ошибке создания базы данных</exception>
        /// <returns>Задача, представляющая асинхронную операцию</returns>
        public static async Task CreateAsync(string connectionString, ILogger<ApiAppContext> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Строка подключения не может быть пустой");

            try
            {
                // Создаем контекст базы данных
                using var context = new ApiAppContext(connectionString, logger);

                // Проверяем возможность подключения
                if (!await context.Database.CanConnectAsync())
                {
                    await context.Database.EnsureCreatedAsync(); // Если база данных не существует - создаем её
                    //await context.Database.MigrateAsync(); // По условиям задачи миграции создавать и применять не нужно
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при создании базы данных: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Асинхронно удаляет базу данных
        /// </summary>
        /// <param name="connectionString">Строка подключения к базе данных</param>
        /// <param name="logger">Логгер для записи событий и ошибок</param>
        /// <returns>true - если база данных была удалена, false - если база данных не существовала</returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если connectionString равен null или пуст</exception>
        public static async Task<bool> DeleteAsync(string connectionString, ILogger<ApiAppContext> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Строка подключения не может быть пустой");

            // Создаем контекст базы данных
            using var context = new ApiAppContext(connectionString, logger);

            // Проверяем существование базы данных
            if (await context.Database.CanConnectAsync())
            {
                return await context.Database.EnsureDeletedAsync(); // Удаляем базу данных
            }

            return false;
        }
    }
}