namespace VerboVision.DataLayer.Helper
{
    /// <summary>
    /// Класс, реализующий асинхронный таймаут
    /// </summary>
    internal class AsyncExt
    {
        /// <summary>
        /// Выполняет асинхронную задачу с ограничением по времени выполнения
        /// </summary>
        /// <param name="task">Задача для выполнения</param>
        /// <param name="timeout">Максимальное время ожидания</param>
        /// <returns>Задача, представляющая асинхронную операцию</returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если задача равна null</exception>
        /// <exception cref="TimeoutException">Выбрасывается, если время выполнения превысило указанный таймаут</exception>
        internal static async Task TimeoutAsync(Task? task, TimeSpan timeout)
        {
            // Проверка, что задача не null
            ArgumentNullException.ThrowIfNull(task);

            // Создаем источник токена отмены для задачи с таймаутом
            using var timeoutCTS = new CancellationTokenSource();

            // Ожидаем завершения любой из задач: исходной задачи или задачи таймаута
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCTS.Token));

            if (completedTask == task)
            {
                // Если исходная задача завершилась первой, отменяем задачу таймаута
                timeoutCTS.Cancel();

                // Await задачи важен для проброса возможных исключений из исходной задачи
                await task;  // Очень важно для распространения исключений
            }
            else
            {
                // Если первой завершилась задача таймаута, выбрасываем исключение
                throw new TimeoutException("Операция завершилась по истечении времени ожидания.");
            }
        }

        /// <summary>
        /// Выполняет асинхронную задачу ValueTask с ограничением по времени выполнения
        /// </summary>
        /// <param name="task">ValueTask задача для выполнения</param>
        /// <param name="timeout">Максимальное время ожидания</param>
        /// <returns>Задача, представляющая асинхронную операцию</returns>
        /// <exception cref="TimeoutException">Выбрасывается, если время выполнения превысило указанный таймаут</exception>
        internal static async Task TimeoutAsync(ValueTask task, TimeSpan timeout)
        {
            // Конвертируем ValueTask в Task для использования с Task.WhenAny
            await TimeoutAsync(task.AsTask(), timeout);
        }

        /// <summary>
        /// Выполняет асинхронную задачу с ограничением по времени выполнения и возвращает результат
        /// </summary>
        /// <typeparam name="TResult">Тип результата задачи</typeparam>
        /// <param name="task">Задача для выполнения</param>
        /// <param name="timeout">Максимальное время ожидания</param>
        /// <returns>Результат выполнения задачи</returns>
        /// <exception cref="TimeoutException">Выбрасывается, если время выполнения превысило указанный таймаут</exception>
        internal static async Task<TResult> TimeoutAsync<TResult>(Task<TResult> task, TimeSpan timeout)
        {
            // Создаем источник токена отмены для задачи с таймаутом
            using var timeoutCTS = new CancellationTokenSource();

            // Ожидаем завершения любой из задач: исходной задачи или задачи таймаута
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCTS.Token));

            if (completedTask == task)
            {
                // Если исходная задача завершилась первой, отменяем задачу таймаута
                timeoutCTS.Cancel();

                // Возвращаем результат и пробрасываем возможные исключения
                return await task;  // Очень важно для распространения исключений
            }
            else
            {
                // Если первой завершилась задача таймаута, выбрасываем исключение
                throw new TimeoutException("Операция завершилась по истечении времени ожидания.");
            }
        }

        /// <summary>
        /// Выполняет асинхронную задачу ValueTask с ограничением по времени выполнения и возвращает результат
        /// </summary>
        /// <typeparam name="TResult">Тип результата задачи</typeparam>
        /// <param name="task">ValueTask задача для выполнения</param>
        /// <param name="timeout">Максимальное время ожидания</param>
        /// <returns>Результат выполнения задачи</returns>
        /// <exception cref="TimeoutException">Выбрасывается, если время выполнения превысило указанный таймаут</exception>
        internal static async Task<TResult> TimeoutAsync<TResult>(ValueTask<TResult> task, TimeSpan timeout)
        {
            // Конвертируем ValueTask в Task для использования с Task.WhenAny
            return await TimeoutAsync(task.AsTask(), timeout);
        }
    }
}