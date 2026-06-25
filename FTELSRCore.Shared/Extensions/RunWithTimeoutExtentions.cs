namespace FTELSRCore.Extensions
{
    public static class RunWithTimeoutExtensions
    {
        /// <summary>
        /// Hạm cho phép thực thi với thời gian chờ mong muốn nếu qua thì timeout nhưng vẫn chạy tiến trìn hoàn tất.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public static async Task<(T Data, bool Result)> RunWithTimeoutAsync<T>(Func<Task<T>> action, int timeoutMs, CancellationToken cancellationToken = default)
        {
            Task<T> workTask = action();

            switch (timeoutMs is 0)
            {
                case true:
                    {
                        return (Data: await workTask, Result: true);
                    }
                case false:
                    {
                        Task timeoutTask =
                            Task.Delay(millisecondsDelay: timeoutMs, cancellationToken: cancellationToken);

                        if (await Task.WhenAny(workTask, timeoutTask) == timeoutTask)
                        {
                            return (Data: default, Result: true);
                        }

                        return (Data: await workTask, Result: true);
                    }
            }
        }
    }
}