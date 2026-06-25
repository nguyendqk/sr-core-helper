using System.Diagnostics;

namespace FTELSRCore.Extensions
{
    public static class MeasureExecutionTimeExtensions
    {
        /// <summary>
        /// Thực thi hàm bất đồng bộ, đo thời gian và ghi log nếu vượt ngưỡng thời gian mong muốn.
        /// </summary>
        /// <typeparam name="TOut">Kiểu kết quả trả về.</typeparam>
        /// <param name="func">Hàm bất đồng bộ cần thực thi.</param>
        /// <param name="logger">Đối tượng logger để ghi log.</param>
        /// <param name="measureByKey">Khóa định danh cho đoạn đo.</param>
        /// <param name="desiredTime">Thời gian mong muốn tối đa (đơn vị: seconds).</param>
        /// <param name="cancellationToken">Token huỷ bỏ nếu cần.</param>
        /// <returns>Giá trị kết quả trả về từ hàm func.</returns>
        ///
        public static ValueTask<TOut> InvokeForMediaR<TOut>(
            Func<ValueTask<TOut>> func, ILogger logger, string measureByKey, int desiredTime = 5, CancellationToken cancellationToken = default) where TOut : notnull
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ExecuteAsync(cancellationToken: cancellationToken);

            async ValueTask<TOut> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                long start = Stopwatch.GetTimestamp();

                TOut result = await func();

                long elapsedMs =
                    (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                double elapseds = elapsedMs / 1000.0;

                if (elapseds > desiredTime)
                {
                    logger.Warning(
                        methodName: nameof(InvokeForMediaR),
                        className: nameof(MeasureExecutionTimeExtensions),
                        message: $"[PERFORMANCE] Long Running Request [{measureByKey}] took {elapseds} seconds.");
                }

                logger.MediaRResult(
                    className: nameof(MeasureExecutionTimeExtensions),
                    methodName: nameof(InvokeForMediaR),
                    latency: elapsedMs,
                    message: measureByKey);

                return result;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="func"></param>
        /// <param name="logger"></param>
        /// <param name="measureByKey"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public static ValueTask<TOut> InvokeForHTTP<TOut>(
            Func<ValueTask<TOut>> func, ILogger logger, string measureByKey, int desiredTime = 5, CancellationToken cancellationToken = default) where TOut : notnull
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ExecuteAsync(cancellationToken: cancellationToken);

            async ValueTask<TOut> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                long start = Stopwatch.GetTimestamp();

                TOut result = await func();

                double elapsed = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;

                if (elapsed > desiredTime)
                {
                    logger.Warning(
                        methodName: nameof(InvokeForHTTP),
                        className: nameof(MeasureExecutionTimeExtensions),
                        message: $"[PERFORMANCE] Long Running Request [{measureByKey}] took {elapsed} seconds.");
                }

                return result;
            }
        }
    }
}