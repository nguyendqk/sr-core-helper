namespace FTELSRCore.Constants
{
    /// <summary>
    /// Chứa các hằng số liên quan đến thời gian cache.
    /// </summary>
    ///
    public static class CachedBaseConstant
    {
        /// <summary>
        /// Thời gian cache ngắn (15 phút).
        /// </summary>
        public const double ShortTime = 15;

        /// <summary>
        /// Thời gian cache trung bình (60 phút).
        /// </summary>
        public const double MediumTime = 60;

        /// <summary>
        /// Thời gian cache dài (480 phút).
        /// </summary>
        public const double LongTime = 480;

        /// <summary>
        /// Giảm thời gian cache một lượng ngẫu nhiên dựa trên tỷ lệ phần trăm (jitter)
        /// nhằm tránh nhiều cache hết hạn cùng lúc (cache stampede).
        /// </summary>
        /// <param name="time">Thời gian cache cơ bản.</param>
        /// <param name="percent">
        /// Tỷ lệ phần trăm tối đa được phép giảm từ thời gian cache.
        /// Giá trị mặc định là 0.1 (tương đương 10%).
        /// </param>
        /// <returns>
        /// Thời gian cache sau khi được giảm ngẫu nhiên trong khoảng:
        /// <c>time * (1 - percent)</c> đến <c>time</c>.
        /// </returns>
        public static double RandomTimeCache(double time, double percent = 0.1)
        {
            return time - (time * Random.Shared.NextDouble() * percent);
        }
    }
}