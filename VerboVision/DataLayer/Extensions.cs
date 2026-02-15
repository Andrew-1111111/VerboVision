using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VerboVision.DataLayer.DB.Context;
using VerboVision.DataLayer.Repositories;
using VerboVision.DataLayer.Repositories.Interfaces;

namespace VerboVision.DataLayer
{
    /// <summary>
    /// Класс расширения для подключения сервисов уровня данных
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Метод расширения для регистрации сервисов уровня данных в DI контейнере
        /// </summary>
        /// <param name="serviceCollection">Коллекция сервисов (IServiceCollection)</param>
        /// <param name="connectionString">Строка подключения к базе данных</param>
        /// <returns>Та же коллекция сервисов для цепочки вызовов</returns>
        internal static IServiceCollection AddDataLayer(this IServiceCollection serviceCollection, string connectionString)
        {
            // Регистрация репозиториев в DI контейнере
            serviceCollection.AddScoped<IImageRepository, ImageRepository>();

            // Настройка контекста базы данных
            serviceCollection.AddDbContext<ApiAppContext>(options =>
            {
                // Установка строки подключения и параметров подключения
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,                           // Максимальное количество повторных попыток
                        maxRetryDelay: TimeSpan.FromSeconds(5),     // Максимальная задержка между попытками
                        errorCodesToAdd: null);                     // Дополнительные коды ошибок для повтора
                });

                // Отключение логирования команд SQL
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuting));
            });

            return serviceCollection;
        }
    }
}