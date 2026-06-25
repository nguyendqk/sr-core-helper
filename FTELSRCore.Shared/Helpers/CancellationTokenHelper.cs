namespace FTELSRCore.Helpers
{
    public static class CancellationTokenHelper
    {
        /// <summary>
        /// Tạo CancellationTokenSource liên kết với token bên ngoài và timeout nếu có.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="timeoutSeconds">Thời gian timeout tính bằng giây. Nếu là 0, không cấu hình timeout.</param>
        /// <returns>CancellationTokenSource đã được thiết lập.</returns>
        ///
        public static CancellationTokenSource CreateLinkedTokenWithTimeout(CancellationToken cancellationToken, int timeoutSeconds = 0)
        {
            CancellationTokenSource cancellationSet = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (timeoutSeconds > 0)
            {
                cancellationSet.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            }

            return cancellationSet;
        }
    }
}