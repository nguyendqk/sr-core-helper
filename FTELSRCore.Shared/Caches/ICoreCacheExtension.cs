using ZiggyCreatures.Caching.Fusion;

namespace FTELSRCore.Caches
{
    public interface ICoreCacheExtension
    {
        /// <summary>
        /// Lấy thông tin cache hoặc tạo cache.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="func"></param>
        /// <param name="options"></param>
        /// <param name="step"></param>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="expiredMinutes"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <returns></returns>
        ///
        ValueTask<TOut> GetOrCreateAsync<TOut>(string key,
                                               double expiredMinutes,
                                               Func<ValueTask<TOut>> func,
                                               StepCache step = StepCache.None,
                                               FusionCacheEntryOptions options = null,
                                               int cancellationTokenTime = 3,
                                               CancellationToken cancellationToken = default) where TOut : class;

        /// <summary>
        /// Lấy thông tin dữ liệu trong cache theo key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="step"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <returns></returns>
        ///
        Task<string> GetCacheByKeyAsync(string key,
                                        StepCache step = StepCache.None,
                                        FusionCacheEntryOptions options = null,
                                        int cancellationTokenTime = 1,
                                        CancellationToken cancellationToken = default);

        /// <summary>
        /// Dán cache vào hệ thống.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="step"></param>
        /// <param name="options"></param>
        /// <param name="expiredMinutes"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <returns></returns>
        ///
        Task SetCacheByKeyAsync(string key,
                                string value,
                                double expiredMinutes = 1,
                                StepCache step = StepCache.None,
                                FusionCacheEntryOptions options = null,
                                int cancellationTokenTime = 1,
                                CancellationToken cancellationToken = default);

        /// <summary>
        /// Gán cache vào hệ thống với cấu hình tự xử lý.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <param name="step"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <returns></returns>
        ///
        Task SetCacheByKeyAsync(string key,
                                string value,
                                FusionCacheEntryOptions options,
                                StepCache step = StepCache.None,
                                int cancellationTokenTime = 1,
                                CancellationToken cancellationToken = default);

        /// <summary>
        /// Xoá danh sách cache theo yêu cầu
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <returns></returns>
        ///
        Task ClearAllCacheAsync(string[] keys, int cancellationTokenTime = 1, CancellationToken cancellationToken = default);
    }

    public enum StepCache : byte
    {
        None = 0,

        Local = 1,

        Distributed = 2
    }
}