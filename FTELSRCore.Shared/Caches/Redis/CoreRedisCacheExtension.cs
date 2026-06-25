using StackExchange.Redis;

namespace FTELSRCore.Caches.Redis
{
    public class CoreRedisCacheExtension(ICoreCacheExtension coreCacheExtension, IConnectionMultiplexer connectionMultiplexer, ILogger<CoreRedisCacheExtension> logger) : ICoreRedisCacheExtension
    {
        /// <summary>
        /// Lấy danh sách dữ liệu trong cache hiện tại.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="pageSize"></param>
        /// <param name="configurationOptions"></param>
        /// <returns></returns>
        ///
        public async Task<Dictionary<string, string>> GetAllDataAsync(
            string pattern = "*", int database = 1, string instanceName = "SR:v2:", int pageSize = 10_000)
        {
            logger.Request(nameof(CoreRedisCacheExtension), nameof(GetAllDataAsync));

            IServer server =
                connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().FirstOrDefault());

            IEnumerable<string> keys =
                server.Keys(
                    database: database,
                    pattern: pattern, pageSize: pageSize).Select(key => (string)key);

            Dictionary<string, string> result = [];

            foreach (string key in keys)
            {
                string value =
                    await coreCacheExtension.GetCacheByKeyAsync(
                        key: key.Replace(instanceName, string.Empty)).ConfigureAwait(false);

                result.Add(key: key, value: value);
            }

            logger.Response(nameof(CoreRedisCacheExtension), nameof(GetAllDataAsync));

            return result;
        }

        /// <summary>
        /// Lấy danh sách key trong cache hiện tại.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        ///
        public async Task<IEnumerable<string>> GetAllKeyAsync(
            string pattern = "*", int database = 1, int pageSize = 10_000)
        {
            logger.Request(nameof(CoreRedisCacheExtension), nameof(GetAllKeyAsync));

            IServer server =
                connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().FirstOrDefault());

            return
                server.Keys(
                    database: database,
                    pattern: pattern, pageSize: pageSize).Select(key => (string)key);
        }

        /// <summary>
        /// Thực hiện thao tác xóa tay key trong cache.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="instanceName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public async Task<bool> ClearDataWithKeys(
            List<string> pattern, string instanceName = "SR:v2:", CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.Request(nameof(CoreRedisCacheExtension), nameof(ClearDataWithKeys));

            var keys = pattern.Select(key =>
            {
                return key?.Replace(instanceName, string.Empty);
            }).ToArray();

            await coreCacheExtension.ClearAllCacheAsync(keys: keys, cancellationToken: cancellationToken);

            logger.Response(nameof(CoreRedisCacheExtension), nameof(ClearDataWithKeys));

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="script"></param>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        ///
        public Task<RedisResult> LUAAtomicCacheAsync(
            string script, RedisKey[] keys, RedisValue[] values, int database = 1)
        {
            logger.Request(nameof(CoreRedisCacheExtension), nameof(LUAAtomicCacheAsync));

            IDatabase table = connectionMultiplexer.GetDatabase(db: database);

            return table.ScriptEvaluateAsync(
                script: script, keys: keys, values: values);
        }
    }
}