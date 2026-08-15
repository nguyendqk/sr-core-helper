using FTELSRCore.Caches.Helpers;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using ZiggyCreatures.Caching.Fusion;

namespace FTELSRCore.Caches
{
    public class CoreCacheExtension(IFusionCache fusionCache, ILogger<CoreCacheExtension> logger) : ICoreCacheExtension
    {
        private readonly ActivitySource _activitySource = new(OpenTelemetryConstant.CoreCacheActivitySource);

        #region ::::::::::::::::: GET :::::::::::::::::

        /// <summary>
        /// Lấy thông tin cache hoặc tạo cache.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="key"></param>
        /// <param name="expiredMinutes"></param>
        /// <param name="func"></param>
        /// <param name="step"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <returns></returns>
        ///
        public async ValueTask<TOut> GetOrCreateAsync<TOut>(string key,
                                                            double expiredMinutes,
                                                            Func<ValueTask<TOut>> func,
                                                            StepCache step = StepCache.None,
                                                            FusionCacheEntryOptions options = null,
                                                            int cancellationTokenTime = 3,
                                                            CancellationToken cancellationToken = default) where TOut : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            using Activity activity = _activitySource.StartActivity("cache.get", ActivityKind.Internal);

            activity?.SetTag("cache.key", key);
            activity?.SetTag("cache.name", nameof(GetOrCreateAsync));

            if (activity is { } a) a.DisplayName = $"GET {key}";

            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            using CancellationTokenSource cancellationTokenSource =
                 CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

            options = FusionCacheEntryOptions(expiredMinutes, options);

            switch (step)
            {
                case StepCache.Local:
                    {
                        options.SkipDistributedCacheRead = true;
                        options.SkipDistributedCacheWrite = true;

                        break;
                    }
                case StepCache.Distributed:
                    {
                        options.SkipMemoryCacheRead = true;
                        options.SkipMemoryCacheWrite = true;

                        options.AllowBackgroundDistributedCacheOperations = true;

                        options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(cancellationTokenTime);

                        options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(cancellationTokenTime + 1);

                        break;
                    }
                default:
                    {
                        options.AllowBackgroundDistributedCacheOperations = true;

                        options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(cancellationTokenTime);

                        options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(cancellationTokenTime + 1);

                        break;
                    }
            }

            try
            {
                string resultString =
                    await fusionCache.GetOrSetAsync<string>(
                        key: key,
                        factory: async (ctx, ct) =>
                        {
                            TOut dataResult =
                                await GetResultAsync(func: func, cancellationToken: ct).ConfigureAwait(false);

                            string serialized = dataResult?.ToJSon();

                            if (string.IsNullOrWhiteSpace(serialized)
                                || serialized == "null"
                                || serialized == "{}"
                                || serialized == "[]")
                            {
                                ctx.Options.SkipDistributedCacheWrite = true;
                                ctx.Options.SkipMemoryCacheWrite = true;

                                return null;
                            }

                            return serialized;
                        },
                        options: options,
                        token: cancellationTokenSource.Token).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(resultString)
                    || !resultString.JSonTryParse(out TOut result))
                {
                    return null;
                }

                return result;
            }
            catch (FusionCacheSerializationException jsonEx)
            {
                logger.Info(nameof(CoreCacheExtension), nameof(GetOrCreateAsync),
                    message: $"FusionCacheSerializationException {key} - Ex: {jsonEx.Message}");

                TOut result =
                    await GetResultAsync(
                        func: func,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                string resultString = result?.ToJSon();

                if (string.IsNullOrWhiteSpace(resultString)
                    || resultString == "null"
                    || resultString == "{}"
                    || resultString == "[]")
                {
                    return null;
                }

                await SetCacheByKeyAsync(key: key,
                                         step: step,
                                         options: options,
                                         value: resultString,
                                         expiredMinutes: expiredMinutes,
                                         cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                return result;
            }
            catch (OperationCanceledException)
            {
                logger.Info(nameof(CoreCacheExtension), nameof(GetOrCreateAsync),
                    message: $"OperationCanceledException {key} with SLA {cancellationTokenTime}s");

                return null;
            }
            catch (Exception exception)
            {
                logger.ConnectionErrorRedis(nameof(CoreCacheExtension), nameof(GetOrCreateAsync), e: exception, message: $"key: {key}");

                return null;
            }
        }

        /// <summary>
        /// Hàm xử lý lấy dữ liệu.
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        private static ValueTask<TOut> GetResultAsync<TOut>(
            Func<ValueTask<TOut>> func, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ExecuteAsync(cancellationToken: cancellationToken);

            async ValueTask<TOut> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                TOut dataInput = await func();

                if (dataInput is null)
                {
                    return default;
                }

                string convertString = dataInput.ToJSon();

                if (string.IsNullOrWhiteSpace(convertString)
                    || convertString == "null"
                    || convertString == "{}"
                    || convertString == "[]")
                {
                    return default;
                }

                return dataInput;
            }
        }

        /// <summary>
        /// Lấy thông tin dữ liệu trong cache theo key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="step"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public async Task<string> GetCacheByKeyAsync(string key,
                                                     StepCache step = StepCache.None,
                                                     FusionCacheEntryOptions options = null,
                                                     int cancellationTokenTime = 1,
                                                     CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using Activity activity = _activitySource.StartActivity("cache.get", ActivityKind.Internal);

            activity?.SetTag("cache.key", key);
            activity?.SetTag("cache.name", nameof(GetCacheByKeyAsync));

            if (activity is { } a) a.DisplayName = $"GET {key}";

            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            options = options is null
                ? CoreCacheHelper.FusionCacheEntryOptionsDefault().Duplicate()
                : options.Duplicate();

            try
            {
                switch (step)
                {
                    case StepCache.Local:
                        {
                            options.SkipDistributedCacheRead = true;

                            break;
                        }
                    case StepCache.Distributed:
                        {
                            options.SkipMemoryCacheRead = true;

                            options.AllowBackgroundDistributedCacheOperations = true;

                            options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(cancellationTokenTime);

                            options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(cancellationTokenTime + 1);

                            break;
                        }
                    default:
                        {
                            options.AllowBackgroundDistributedCacheOperations = true;

                            options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(cancellationTokenTime);

                            options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(cancellationTokenTime + 1);

                            break;
                        }
                }

                MaybeValue<string> result =
                    await fusionCache.TryGetAsync<string>(
                        key: key,
                        options: options,
                        token: CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime).Token);

                string value = string.Empty;

                switch (result.HasValue)
                {
                    case true:
                        {
                            value = result.Value;

                            break;
                        }
                    case false:
                        {
                            break;
                        }
                }

                return value;
            }
            catch (OperationCanceledException)
            {
                logger.Info(nameof(CoreCacheExtension), nameof(GetCacheByKeyAsync),
                    message: $"OperationCanceledException {key} with SLA {cancellationTokenTime}s");

                return null;
            }
            catch (Exception exception)
            {
                logger.ConnectionErrorRedis(nameof(CoreCacheExtension), nameof(GetCacheByKeyAsync), e: exception, message: $"key: {key}");

                return string.Empty;
            }
        }

        #endregion ::::::::::::::::: GET :::::::::::::::::

        #region ::::::::::::::::: SET :::::::::::::::::

        /// <summary>
        /// Dán cache vào hệ thống.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="step"></param>
        /// <param name="options"></param>
        /// <param name="expiredMinutes"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public async Task SetCacheByKeyAsync(string key,
                                             string value,
                                             double expiredMinutes = 1,
                                             StepCache step = StepCache.None,
                                             FusionCacheEntryOptions options = null,
                                             int cancellationTokenTime = 1,
                                             CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using Activity activity = _activitySource.StartActivity("cache.set", ActivityKind.Internal);

            activity?.SetTag("cache.key", key);
            activity?.SetTag("cache.name", nameof(SetCacheByKeyAsync));

            if (activity is { } a) a.DisplayName = $"SET {key}";

            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            try
            {
                options = FusionCacheEntryOptions(expiredMinutes, options);

                switch (step)
                {
                    case StepCache.Local:
                        {
                            options.SkipDistributedCacheWrite = true;

                            break;
                        }
                    case StepCache.Distributed:
                        {
                            options.SkipMemoryCacheWrite = true;

                            options.AllowBackgroundDistributedCacheOperations = true;

                            options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(cancellationTokenTime);

                            options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(cancellationTokenTime + 1);

                            break;
                        }
                    default:
                        {
                            options.AllowBackgroundDistributedCacheOperations = true;

                            options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(cancellationTokenTime);

                            options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(cancellationTokenTime + 1);

                            break;
                        }
                }

                await fusionCache.SetAsync(key: key,
                                           value: value,
                                           options: options,
                                           token: CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime).Token);
            }
            catch (OperationCanceledException)
            {
                logger.Info(nameof(CoreCacheExtension), nameof(SetCacheByKeyAsync),
                    message: $"OperationCanceledException {key} with SLA {cancellationTokenTime}s");
            }
            catch (Exception exception)
            {
                logger.ConnectionErrorRedis(nameof(CoreCacheExtension), nameof(SetCacheByKeyAsync), e: exception, message: $"key: {key}");
            }
        }

        /// <summary>
        /// Gán cache vào hệ thống với cấu hình tự xử lý.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <param name="step"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public async Task SetCacheByKeyAsync(string key,
                                             string value,
                                             FusionCacheEntryOptions options,
                                             StepCache step = StepCache.None,
                                             int cancellationTokenTime = 1,
                                             CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using Activity activity = _activitySource.StartActivity("cache.set", ActivityKind.Internal);

            activity?.SetTag("cache.key", key);
            activity?.SetTag("cache.name", nameof(SetCacheByKeyAsync));

            if (activity is { } a) a.DisplayName = $"SET {key}";

            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            try
            {
                switch (step)
                {
                    case StepCache.Local:
                        {
                            options.SkipDistributedCacheWrite = true;

                            break;
                        }
                    case StepCache.Distributed:
                        {
                            options.SkipMemoryCacheWrite = true;

                            options.AllowBackgroundDistributedCacheOperations = true;

                            options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(cancellationTokenTime);

                            options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(cancellationTokenTime + 1);

                            break;
                        }
                    default:
                        {
                            options.AllowBackgroundDistributedCacheOperations = true;

                            options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(cancellationTokenTime);

                            options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(cancellationTokenTime + 1);

                            break;
                        }
                }

                await fusionCache.SetAsync(key: key,
                                           value: value,
                                           options: options,
                                           token: CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime).Token);
            }
            catch (OperationCanceledException)
            {
                logger.Info(nameof(CoreCacheExtension), nameof(SetCacheByKeyAsync),
                    message: $"OperationCanceledException {key} with SLA {cancellationTokenTime}s");
            }
            catch (Exception exception)
            {
                logger.ConnectionErrorRedis(nameof(CoreCacheExtension), nameof(SetCacheByKeyAsync), e: exception, message: $"key: {key}");
            }
        }

        #endregion ::::::::::::::::: SET :::::::::::::::::

        #region ::::::::::::::::: DELETE :::::::::::::::::

        /// <summary>
        /// Xoá danh sách cache theo yêu cầu
        /// </summary>
        /// <param name="keys">Danh sách cache</param>
        /// <param name="cancellationTokenTime">Thời gian hủy token</param>
        /// <param name="cancellationToken">Token hủy</param>
        ///
        public async Task ClearAllCacheAsync(
            string[] keys, int cancellationTokenTime = 1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            keys = keys?.Distinct()?.ToArray();

            using Activity activity = _activitySource.StartActivity("cache.clear", ActivityKind.Internal);

            activity?.SetTag("cache.name", nameof(ClearAllCacheAsync));
            activity?.SetTag("cache.key", string.Join(", ", keys));

            if (activity is { } a) a.DisplayName = $"DELETE {string.Join(", ", keys)}";

            try
            {
                if (keys.IsNullOrEmpty())
                {
                    return;
                }

                foreach (string item in keys)
                {
                    await ClearCacheAsync(
                        key: item,
                        cancellationTokenTime: cancellationTokenTime,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                logger.Info(nameof(CoreCacheExtension), nameof(SetCacheByKeyAsync),
                    message: $"OperationCanceledException {string.Join(DelimiterConstant.CHAR_COMMA, keys)} with SLA {cancellationTokenTime}s");
            }
            catch (Exception exception)
            {
                logger.ConnectionErrorRedis(nameof(CoreCacheExtension), nameof(ClearAllCacheAsync), e: exception, message: $"keys: {string.Join(", ", keys)}");
            }
        }

        /// <summary>
        /// Xoá cache theo yêu cầu
        /// </summary>
        /// <param name="key">Khóa cache</param>
        /// <param name="cancellationTokenTime">Thời gian hủy token</param>
        /// <param name="cancellationToken">Token hủy</param>
        /// <returns></returns>
        ///
        private async Task ClearCacheAsync(
            string key, int cancellationTokenTime = 1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using Activity activity = _activitySource.StartActivity("cache.clear", ActivityKind.Internal);

            activity?.SetTag("cache.key", key);
            activity?.SetTag("cache.name", nameof(ClearCacheAsync));

            if (activity is { } a) a.DisplayName = $"DELETE {key}";

            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            using CancellationTokenSource cancellation =
                CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

            try
            {
                await fusionCache.RemoveAsync(
                    key: key,
                    options: new FusionCacheEntryOptions
                    {
                        IsFailSafeEnabled = false,
                        AllowBackgroundBackplaneOperations = false,
                        AllowBackgroundDistributedCacheOperations = false
                    }, token: cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                logger.Info(nameof(CoreCacheExtension), nameof(ClearCacheAsync),
                    message: $"OperationCanceledException {key} with SLA {cancellationTokenTime}s");
            }
            catch (Exception exception)
            {
                logger.ConnectionErrorRedis(nameof(CoreCacheExtension), nameof(ClearCacheAsync), e: exception, message: $"key: {key}");
            }
        }

        #endregion ::::::::::::::::: DELETE :::::::::::::::::

        #region ::::::::::::::::: CONFIGURATION :::::::::::::::::

        /// <summary>
        ///  Nếu cache dưới 15 phút thì không cần set thời gian bảo hiểm.
        /// </summary>
        /// <param name="durationTime">Thời gian muốn cache lại</param>
        /// <returns></returns>
        ///
        private static bool IsFailSafeEnabled(TimeSpan durationTime)
        {
            return durationTime >= TimeSpan.FromMinutes(15);
        }

        /// <summary>
        /// Cấu hình dành cho FusionCacheEntryOptions
        /// </summary>
        /// <param name="expiredMinutes"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        ///
        private static FusionCacheEntryOptions FusionCacheEntryOptions(
            double expiredMinutes, FusionCacheEntryOptions options = null)
        {
            options = options is null
                ? CoreCacheHelper.FusionCacheEntryOptionsDefault().Duplicate()
                : options.Duplicate();

            options.Duration = expiredMinutes <= 0 ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(expiredMinutes);

            options.IsFailSafeEnabled = IsFailSafeEnabled(options.Duration);

            options.FailSafeMaxDuration =
                options.IsFailSafeEnabled is true ? options.Duration + TimeSpan.FromMinutes(15) : TimeSpan.Zero;

            return options;
        }

        #endregion ::::::::::::::::: CONFIGURATION :::::::::::::::::
    }

    #region ::::::::::::::::: HELPER :::::::::::::::::

    internal static class JsonExts
    {
        internal static bool JsonIsNullOrEmpty(this JToken value)
        {
            return value == null ||
                   value.Type == JTokenType.Array && !value.HasValues ||
                   value.Type == JTokenType.Object && !value.HasValues ||
                   value.Type == JTokenType.String && (string.IsNullOrEmpty(value.ToString()) ||
                                                       string.IsNullOrWhiteSpace(value.ToString())) ||
                   value.Type == JTokenType.Null;
        }
    }

    #endregion ::::::::::::::::: HELPER :::::::::::::::::
}