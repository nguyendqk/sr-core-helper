using MongoDB.Driver;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Net.Sockets;

namespace FTELSRCore.Data.MongoDB.Helpers.Policies
{
    public class MongoResiliencePolicyFactory
    {
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
                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                e: args.Outcome.Exception,
                                message: $"[CB OPEN] blocking DB for {args.BreakDuration.TotalSeconds:F0}s");

                            return default;
                        },
                        OnClosed = args =>
                        {
                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                e: args.Outcome.Exception,
                                message: "[CB CLOSED] DB restored");

                            return default;
                        },
                        OnHalfOpened = args =>
                        {
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
                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                e: args.Outcome.Exception,
                                message: $"[CB OPEN] blocking DB for {args.BreakDuration.TotalSeconds:F0}s");

                            return default;
                        },
                        OnClosed = args =>
                        {
                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                e: args.Outcome.Exception,
                                message: "[CB CLOSED] DB restored");

                            return default;
                        },
                        OnHalfOpened = args =>
                        {
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
                        MaxRetryAttempts = 1,
                        Delay = TimeSpan.FromMilliseconds(300),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        OnRetry = args =>
                        {
                            logger.Warning(
                                className: nameof(MongoResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                e: args.Outcome.Exception,
                                message: $"[RETRY {args.AttemptNumber + 1}/{3}] wait {args.RetryDelay.TotalMilliseconds:F0}ms");

                            return default;
                        }
                    });
        }

        private static bool IsRetryable(Exception ex, bool handleAllTransient)
        {
            if (ex is MongoConnectionException
                or MongoNotPrimaryException
                or MongoNodeIsRecoveringException
                or SocketException)
            {
                return true;
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