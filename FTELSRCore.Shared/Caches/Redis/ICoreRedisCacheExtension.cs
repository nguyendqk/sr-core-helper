using StackExchange.Redis;

namespace FTELSRCore.Caches.Redis
{
    public interface ICoreRedisCacheExtension
    {
        /// <summary>
        /// Lấy danh sách dữ liệu trong cache hiện tại.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="pageSize"></param>
        /// <param name="configurationOptions"></param>
        /// <returns></returns>
        ///
        Task<Dictionary<string, string>> GetAllDataAsync(
            string pattern = "*", int database = 1, string instanceName = "SR:v2:", int pageSize = 10_000);

        /// <summary>
        /// Lấy danh sách key trong cache hiện tại.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="pageSize"></param>
        /// <param name="configurationOptions"></param>
        /// <returns></returns>
        ///
        Task<IEnumerable<string>> GetAllKeyAsync(
            string pattern = "*", int database = 1, int pageSize = 10_000);

        /// <summary>
        /// Thực hiện thao tác xóa tay key trong cache.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="instanceName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<bool> ClearDataWithKeys(
            List<string> pattern, string instanceName = "SR:v2:", CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="script"></param>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        ///
        Task<RedisResult> LUAAtomicCacheAsync(
            string script, RedisKey[] keys, RedisValue[] values, int database = 1);
    }
}