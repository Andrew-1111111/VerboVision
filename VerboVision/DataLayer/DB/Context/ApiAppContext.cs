using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VerboVision.DataLayer.DB.Configurations;
using VerboVision.DataLayer.DB.Models;

namespace VerboVision.DataLayer.DB.Context
{
    /// <summary>
    /// Контекст базы данных Entity Framework Core для приложения
    /// </summary>
    public class ApiAppContext : DbContext
    {
        #region Поля
        // Строка подключения к базе данных
        private readonly string? _connectionString;

        // Логгер для записи событий и ошибок контекста базы данных
        private readonly ILogger<ApiAppContext> _logger;
        #endregion

        #region Константы
        // Флаг для включения отладочного режима базы данных (false - продакшн, true - отладка)
        private const bool USE_DB_DEBUG = false;
        #endregion

        /// <summary>
        /// Набор данных для работы с изображениями в базе данных
        /// </summary>
        internal DbSet<ImageEntity> Images => Set<ImageEntity>();

        /// <summary>
        /// Конструктор для использования с Dependency Injection
        /// </summary>
        /// <param name="options">Параметры контекста базы данных</param>
        /// <param name="logger">Логгер для записи событий и ошибок</param>
        public ApiAppContext(DbContextOptions<ApiAppContext> options, ILogger<ApiAppContext> logger) : base(options)
        {
            // DbContextOptions проверяются автоматически
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Конструктор для прямого создания контекста со строкой подключения
        /// </summary>
        /// <param name="connectionString">Строка подключения к базе данных</param>
        /// <param name="logger">Логгер для записи событий и ошибок</param>
        internal ApiAppContext(string? connectionString, ILogger<ApiAppContext> logger)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            _connectionString = connectionString;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Настройка модели базы данных и связей между сущностями
        /// </summary>
        /// <param name="modelBuilder">Построитель модели</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Применяем все конфигурации из сборки
            modelBuilder.ApplyConfiguration(new ImageConfiguration());

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Настройка параметров подключения к базе данных
        /// </summary>
        /// <param name="optionsBuilder">Построитель параметров контекста</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Если контекст создан через DI, параметры уже настроены
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_connectionString, npgsqlOptions =>
                {
                    // Настройка повторных попыток подключения при сбое
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,                           // Максимальное количество попыток
                        maxRetryDelay: TimeSpan.FromSeconds(5),     // Задержка между попытками
                        errorCodesToAdd: null);                     // Дополнительные коды ошибок
                });
            }

            // Настройка логирования в зависимости от режима
#pragma warning disable CS0162
            if (USE_DB_DEBUG)
            {
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();

                // SQL запросы как Information
                optionsBuilder.LogTo(
                    message => _logger.LogInformation("[SQL] {Message}", message),
                    [RelationalEventId.CommandExecuted],
                    LogLevel.Information);

                // Предупреждения как Warning
                optionsBuilder.LogTo(
                    message => _logger.LogWarning("[SQL WARN] {Message}", message),
                    [RelationalEventId.CommandExecuting, RelationalEventId.ConnectionCreating],
                    LogLevel.Warning);

                // Ошибки как Error
                optionsBuilder.LogTo(
                    message => _logger.LogError("[SQL ERROR] {Message}", message),
                    [RelationalEventId.CommandError, RelationalEventId.ConnectionError, RelationalEventId.TransactionError],
                    LogLevel.Error);
            }
            else
            {
                // В продакшене - только ошибки
                optionsBuilder.LogTo(
                    message => _logger.LogError("[DB ERROR] {Message}", message),
                    [RelationalEventId.CommandError, RelationalEventId.ConnectionError],
                    LogLevel.Error);
            }
#pragma warning restore CS0162
        }
    }
}