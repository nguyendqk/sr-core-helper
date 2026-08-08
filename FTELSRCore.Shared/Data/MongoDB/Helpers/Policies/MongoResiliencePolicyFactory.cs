using MongoDB.Driver;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Diagnostics;
using System.Net.Sockets;

namespace FTELSRCore.Data.MongoDB.Helpers.Policies
{
    public class MongoResiliencePolicyFactory
    {
        private static readonly ActivitySource ActivitySource = new(OpenTelemetryConstant.MongoResilienceActivitySource);

        /// <summary>
        /// Pipeline cho Mongo Read: retry 3 lần exponential+jitter, CB 60%/5req/10s → break 20s.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="builder"></param>
        ///
        public static void ConfigureReadPolicy(ResiliencePipelineBuilder builder, ILogger logger)
        {
            builder
                .AddCircuitBreaker(
                    new CircuitBreakerStrategyOptions
                    {
                        ShouldHandle = args => new ValueTask<bool>(
                            args.Outcome.Exception is { } ex && IsConnectionLevel(ex)),
                        FailureRatio = 0.6,
                        MinimumThroughput = 5,
                        BreakDuration = TimeSpan.FromSeconds(20),
                        SamplingDuration = TimeSpan.FromSeconds(10),
                        OnOpened = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("mongodb.circuit_breaker.open", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mongodb");
                            activity?.SetTag("resilience.state", "open");
                            activity?.SetTag("resilience.type", "circuit_breaker");
                            activity?.SetTag("resilience.break_duration_ms", args.BreakDuration.TotalMilliseconds);

                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                e: args.Outcome.Exception,
                                message: $"[CB OPEN] blocking DB for {args.BreakDuration.TotalSeconds:F0}s");

                            return default;
                        },
                        OnClosed = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("mongodb.circuit_breaker.closed", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mongodb");
                            activity?.SetTag("resilience.state", "closed");
                            activity?.SetTag("resilience.type", "circuit_breaker");

                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                e: args.Outcome.Exception,
                                message: "[CB CLOSED] DB restored");

                            return default;
                        },
                        OnHalfOpened = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("mongodb.circuit_breaker.half_open", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mongodb");
                            activity?.SetTag("resilience.type", "circuit_breaker");
                            activity?.SetTag("resilience.state", "half_open");

                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                message: "[CB HALF-OPEN] probing DB");

                            return default;
                        }
                    })
                .AddRetry(
                    new RetryStrategyOptions
                    {
                        ShouldHandle = args => new ValueTask<bool>(
                            args.Outcome.Exception is { } ex && IsRetryable(ex, true)),
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromMilliseconds(150),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        OnRetry = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("mongodb.retry", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mongodb");
                            activity?.SetTag("resilience.type", "retry");
                            activity?.SetTag("retry.attempt", args.AttemptNumber + 1);
                            activity?.SetTag("retry.delay_ms", args.RetryDelay.TotalMilliseconds);

                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                e: args.Outcome.Exception,
                                message: $"[RETRY {args.AttemptNumber + 1}/{3}] wait {args.RetryDelay.TotalMilliseconds:F0}ms");

                            return default;
                        }
                    });
        }

        /// <summary>
        /// Pipeline cho Mongo Write: retry 1 lần CHỈ connection/failover error, CB 50%/10req/15s → break 60s.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="logger"></param>
        ///
        public static void ConfigureWritePolicy(ResiliencePipelineBuilder builder, ILogger logger)
        {
            const int writeMaxRetryAttempts = 1;

            builder
                .AddCircuitBreaker(
                    new CircuitBreakerStrategyOptions
                    {
                        ShouldHandle = args => new ValueTask<bool>(
                            args.Outcome.Exception is { } ex && IsConnectionLevel(ex)),
                        FailureRatio = 0.5,
                        MinimumThroughput = 10,
                        SamplingDuration = TimeSpan.FromSeconds(15),
                        BreakDuration = TimeSpan.FromSeconds(60),
                        OnOpened = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("mongodb.circuit_breaker.open", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mongodb");
                            activity?.SetTag("resilience.state", "open");
                            activity?.SetTag("resilience.type", "circuit_breaker");
                            activity?.SetTag("resilience.break_duration_ms", args.BreakDuration.TotalMilliseconds);

                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                e: args.Outcome.Exception,
                                message: $"[CB OPEN] blocking DB for {args.BreakDuration.TotalSeconds:F0}s");

                            return default;
                        },
                        OnClosed = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("mongodb.circuit_breaker.closed", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mongodb");
                            activity?.SetTag("resilience.state", "closed");
                            activity?.SetTag("resilience.type", "circuit_breaker");

                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                e: args.Outcome.Exception,
                                message: "[CB CLOSED] DB restored");

                            return default;
                        },
                        OnHalfOpened = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("mongodb.circuit_breaker.half_open", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mongodb");
                            activity?.SetTag("resilience.state", "half_open");
                            activity?.SetTag("resilience.type", "circuit_breaker");

                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                message: "[CB HALF-OPEN] probing DB");

                            return default;
                        }
                    })
                .AddRetry(
                    new RetryStrategyOptions
                    {
                        ShouldHandle = args => new ValueTask<bool>(
                            args.Outcome.Exception is { } ex && IsRetryable(ex, false)),
                        MaxRetryAttempts = writeMaxRetryAttempts,
                        Delay = TimeSpan.FromMilliseconds(300),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        OnRetry = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("mongodb.retry", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mongodb");
                            activity?.SetTag("resilience.type", "retry");
                            activity?.SetTag("retry.attempt", args.AttemptNumber + 1);
                            activity?.SetTag("retry.max_attempts", writeMaxRetryAttempts);
                            activity?.SetTag("retry.delay_ms", args.RetryDelay.TotalMilliseconds);

                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                e: args.Outcome.Exception,
                                message: $"[RETRY {args.AttemptNumber + 1}/{writeMaxRetryAttempts}] wait {args.RetryDelay.TotalMilliseconds:F0}ms");

                            return default;
                        }
                    });
        }

        private static bool IsRetryable(Exception ex, bool handleAllTransient)
        {
            // MongoNotPrimaryException/MongoNodeIsRecoveringException: server từ chối NGAY vì đổi
            // vai trò (không phải primary) — thao tác chưa từng được xử lý, an toàn retry cả ghi.
            if (ex is MongoNotPrimaryException or MongoNodeIsRecoveringException)
            {
                return true;
            }

            // MongoConnectionException/SocketException: mất kết nối có thể xảy ra SAU KHI server
            // đã xử lý ghi nhưng TRƯỚC KHI client nhận ack — retry ghi trong trường hợp này có thể
            // tạo bản ghi trùng/áp update 2 lần. Chỉ coi là retryable ở luồng đọc (handleAllTransient),
            // không áp dụng cho ghi.
            if (ex is MongoConnectionException or SocketException)
            {
                return handleAllTransient;
            }

            if (handleAllTransient)
            {
                return ex is MongoExecutionTimeoutException or TimeoutException;
            }

            return false;
        }

        private static bool IsConnectionLevel(Exception ex) =>
            ex is MongoConnectionException
                or MongoNotPrimaryException
                or MongoNodeIsRecoveringException
                or SocketException;
    }
}