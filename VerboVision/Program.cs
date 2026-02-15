using Microsoft.OpenApi;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using VerboVision.DataLayer;
using VerboVision.DataLayer.DB.Context;
using VerboVision.DataLayer.DB.Management;
using VerboVision.DataLayer.Helper;
using VerboVision.PresentationLayer.Middlewares;

// Подключаем возможность использовать методы и поля с модификатором internal для дружественной сборки (в нашем слчае для тестов)
[assembly: InternalsVisibleTo("VerboVision.Tests")]

namespace VerboVision
{
    /// <summary>
    /// Главный класс приложения, точка входа в программу
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Точка входа в приложение. Настраивает и запускает веб-хост.
        /// </summary>
        /// <param name="args">Аргументы командной строки, переданные при запуске</param>
        /// <returns>Асинхронная задача, представляющая выполнение программы</returns>
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Вводим Rate Limit для защиты от перегрузки по колл-ву запросов
            builder.Services.AddRateLimiter(options =>
            {
                // HTTP код ошибки, которая возвращается клиенту при превышении колл-ва запросов
                options.RejectionStatusCode = 429;

                // Глобальное ограничение по запросам применяется для каждого IP адреса
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Request.HttpContext.Connection.RemoteIpAddress!.ToString(),
                        partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,           // FixedWindowRateLimiter будем автоматически обновлять счетчики
                            PermitLimit = 100,                  // Максимум колл-во запросов
                            QueueLimit = 0,                     // Отключаем механизм очередей (если он включен, API не выдает HTTP ошибку,
                                                                // запросы ожидают находясь в очередях)
                            Window = TimeSpan.FromSeconds(15)   // Таймаут, на который блокируется подключение
                        }));
            });

            // Устанавливаем фильтры логирования
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.Warning);

            // Добавляем сервисы в DI контейнер
            builder.Services.AddDataLayer(builder.Configuration["PostgreSQL:ConnectionString"]!);

            // Add services to the container.
            builder.Services.AddControllers();

            // Добавляем Swagger
            builder.Services.AddSwaggerGen(options =>
            {
                // Добавляем XML комментарии
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "VerboVision.xml"));

                // Заполняем шапку SwaggerUI
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Verbo Vision",
                    Description = "Проект тестового задания (C# 9, WebAPI)",
                    Contact = new OpenApiContact
                    {
                        Name = "GitHub",
                        Url = new Uri("https://github.com/Andrew-1111111/VerboVision")
                    }
                });
            });

            var app = builder.Build();

            // Подключаем middleware для глобальной обработки ошибок
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Включаем ограничитель частоты запросов (Rate Limiting)
            app.UseRateLimiter();

            // Режим разработки
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();   // Генерация документации Swagger
                app.UseSwaggerUI(); // Интерфейс Swagger UI для тестирования API
            }
            else if (app.Environment.IsProduction())
            {
                // Перенаправляет HTTP-запросы на HTTPS
                app.UseHttpsRedirection();
            }

            // Подключаем маршрутизацию к контроллерам
            app.MapControllers();

            if (app.Environment.IsDevelopment())
            {
                // Получаем логгер из уже построенного приложения
                using var scope = app.Services.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApiAppContext>>();

                // Создаем или удаляем базу данных
                await DatabaseManager.CreateAsync(builder.Configuration["PostgreSQL:ConnectionString"]!, logger);
                //await DatabaseManager.DeleteAsync(builder.Configuration["PostgreSQL:ConnectionString"]!, logger);
            }

            // Запускаем асинхронную обработку запросов
            await app.RunAsync();
        }
    }
}