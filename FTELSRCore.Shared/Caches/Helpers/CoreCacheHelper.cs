using ZiggyCreatures.Caching.Fusion;

namespace FTELSRCore.Caches.Helpers
{
    public static class CoreCacheHelper
    {
        private static readonly FusionCacheEntryOptions FusionCacheEntryOptions =
            new FusionCacheEntryOptions
            {
                // TTL logic của dữ liệu
                Duration = TimeSpan.FromMinutes(5),

                // Bật fail-safe để khi Redis/backend lỗi vẫn trả stale data
                IsFailSafeEnabled = true,

                // Stale data được phép dùng tối đa 30 phút khi có sự cố
                FailSafeMaxDuration = TimeSpan.FromMinutes(30),

                // Khi đang fail-safe, "ghim" 30s để tránh request nào cũng retry dồn dập
                FailSafeThrottleDuration = TimeSpan.FromSeconds(60),

                // Nếu backend đôi lúc chậm hợp lệ
                FactoryHardTimeout = TimeSpan.FromSeconds(2),

                //Nếu thấy stale trả quá thường xuyên
                FactorySoftTimeout = TimeSpan.FromMilliseconds(200),

                // Timeout mềm cho mỗi thao tác Redis:
                // Quá 1s thì ưu tiên fallback nhanh để giữ latency API
                DistributedCacheSoftTimeout = TimeSpan.FromSeconds(1),

                // Timeout cứng cho mỗi thao tác Redis:
                // Quá 2s thì dừng chờ Redis để tránh nghẽn request/thread
                DistributedCacheHardTimeout = TimeSpan.FromSeconds(2),

                // Thực hiện chạy các lệnh nền xử lý tác vụ.
                AllowBackgroundBackplaneOperations = true,

                // Không chạy Redis operation kiểu nền để behavior dễ kiểm soát khi sự cố
                AllowBackgroundDistributedCacheOperations = false
            };

        private static readonly FusionCacheOptions FusionCacheOptions =
            new FusionCacheOptions
            {
                // Circuit breaker cho Redis (L2):
                // Khi Redis lỗi (timeout/OOM/connection), FusionCache sẽ "mở mạch"
                // và tạm không hit Redis trong 2p
                DistributedCacheCircuitBreakerDuration = TimeSpan.FromMinutes(2),

                // Circuit breaker cho backplane (nếu có dùng pub/sub đồng bộ node)
                BackplaneCircuitBreakerDuration = TimeSpan.FromSeconds(20),

                DefaultEntryOptions = FusionCacheEntryOptions
            };

        public static FusionCacheOptions FusionCacheOptionsDefault() => FusionCacheOptions;

        public static FusionCacheEntryOptions FusionCacheEntryOptionsDefault() => FusionCacheEntryOptions;
    }
}