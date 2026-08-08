using Microsoft.Data.SqlClient;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Data.Common;
using System.Diagnostics;
using System.Net.Sockets;

namespace FTELSRCore.Data.SQL.Helpers.Policies
{
    public class SqlResiliencePolicyFactory
    {
        private static readonly ActivitySource ActivitySource = new(OpenTelemetryConstant.SqlResilienceActivitySource);

        /// <summary>
        /// SQL errors cần retry (connection + transient app-level)
        /// </summary>
        ///
        private static readonly HashSet<int> RetryableSqlErrors =
        [
            -2,     // Command timeout
            -1,     // Connection broken
            64,     // Communication link failure
            233,    // Connection initialization error
            1205,   // Deadlock
            20000,  // Instance not found
            40613,  // Azure SQL database unavailable
            49918,  // Cannot process request, not enough resources
            49919,  // Cannot process create/update request
            49920   // Cannot process request, too many operations
        ];

        /// <summary>
        /// SQL errors chỉ ra server/connection thực sự gặp sự cố (trigger CB)
        /// </summary>
        ///
        private static readonly HashSet<int> ConnectionLevelSqlErrors =
        [
            -1,     // Connection broken
            64,     // Communication link failure
            233,    // Connection initialization error
            40613,  // Azure SQL database unavailable
            49918,  // Not enough resources
            49919,  // Cannot process create/update request
            49920   // Too many operations
        ];

        /// <summary>
        /// Pipeline cho SQL Read: retry 3 lần exponential+jitter, CB 60%/5req/10s → break 20s.
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
                        SamplingDuration = TimeSpan.FromSeconds(10),
                        BreakDuration = TimeSpan.FromSeconds(20),
                        OnOpened = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("sql.circuit_breaker.open", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mssql");
                            activity?.SetTag("resilience.state", "open");
                            activity?.SetTag("resilience.type", "circuit_breaker");
                            activity?.SetTag("resilience.break_duration_ms", args.BreakDuration.TotalMilliseconds);

                            logger.Warning(
                                className: nameof(SqlResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                e: args.Outcome.Exception,
                                message: $"[CB OPEN] blocking DB for {args.BreakDuration.TotalSeconds:F0}s");

                            return default;
                        },
                        OnClosed = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("sql.circuit_breaker.closed", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mssql");
                            activity?.SetTag("resilience.state", "closed");
                            activity?.SetTag("resilience.type", "circuit_breaker");

                            logger.Warning(
                                className: nameof(SqlResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                message: "[CB CLOSED] DB restored");

                            return default;
                        },
                        OnHalfOpened = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("sql.circuit_breaker.half_open", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mssql");
                            activity?.SetTag("resilience.state", "half_open");
                            activity?.SetTag("resilience.type", "circuit_breaker");

                            logger.Warning(
                                className: nameof(SqlResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                message: "[CB HALF-OPEN] probing DB");

                            return default;
                        }
                    })
                .AddRetry(
                    new RetryStrategyOptions
                    {
                        ShouldHandle = args => new ValueTask<bool>(args.Outcome.Exception is { } ex && IsRetryable(ex, true)),
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromMilliseconds(200),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        OnRetry = args =>
                        {
                            using Activity activity = ActivitySource.StartActivity("sql.retry", ActivityKind.Internal);

                            activity?.SetTag("db.system", "mssql");
                            activity?.SetTag("retry.max_attempts", 3);
                            activity?.SetTag("resilience.type", "retry");
                            activity?.SetTag("retry.attempt", args.AttemptNumber + 1);
                            activity?.SetTag("retry.delay_ms", args.RetryDelay.TotalMilliseconds);

                            logger.Warning(
                                className: nameof(SqlResiliencePolicyFactory),
                                methodName: nameof(ConfigureReadPolicy),
                                e: args.Outcome.Exception,
                                message: $"[RETRY {args.AttemptNumber + 1}/{3}] wait {args.RetryDelay.TotalMilliseconds:F0}ms");

                            return default;
                        }
                    });
        }

        /// <summary>
        /// Pipeline cho SQL Write: retry 1 lần CHỈ connection error, CB 50%/10req/15s → break 60s.
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
                        ShouldHandle = args => new ValueTask<bool>(args.Outcome.Exception is { } ex && IsConnectionLevel(ex)),
                        FailureRatio = 0.5,
                        MinimumThroughput = 10,
                        BreakDuration = TimeSpan.FromSeconds(60),
                        SamplingDuration = TimeSpan.FromSeconds(15),
                        OnOpened = args =>
                        {
                            logger.Warning(
                                className: nameof(SqlResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                e: args.Outcome.Exception,
                                message: $"[CB OPEN] blocking DB for {args.BreakDuration.TotalSeconds:F0}s");

                            return default;
                        },
                        OnClosed = args =>
                        {
                            logger.Warning(
                                className: nameof(SqlResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                message: "[CB CLOSED] DB restored");

                            return default;
                        },
                        OnHalfOpened = args =>
                        {
                            logger.Info(
                                className: nameof(SqlResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                message: "[CB HALF-OPEN] probing DB");

                            return default;
                        }
                    })
                .AddRetry(
                    new RetryStrategyOptions
                    {
                        ShouldHandle = args => new ValueTask<bool>(args.Outcome.Exception is { } ex && IsRetryable(ex, false)),
                        MaxRetryAttempts = 1,
                        Delay = TimeSpan.FromMilliseconds(300),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        OnRetry = args =>
                        {
                            logger.Warning(
                                className: nameof(SqlResiliencePolicyFactory),
                                methodName: nameof(ConfigureWritePolicy),
                                e: args.Outcome.Exception,
                                message: $"[RETRY {args.AttemptNumber + 1}/{1}] wait {args.RetryDelay.TotalMilliseconds:F0}ms");

                            return default;
                        }
                    });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="handleAllTransient"></param>
        /// <returns></returns>
        private static bool IsRetryable(Exception ex, bool handleAllTransient)
        {
            if (UnwrapSqlException(ex) is { } sqlEx)
            {
                return handleAllTransient
                    ? RetryableSqlErrors.Contains(sqlEx.Number)
                    : ConnectionLevelSqlErrors.Contains(sqlEx.Number);
            }

            if (ex is SocketException) return true;

            if (handleAllTransient)
            {
                return ex is TimeoutException || (ex is DbException && ex is not SqlException);
            }

            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static bool IsConnectionLevel(Exception ex)
        {
            if (UnwrapSqlException(ex) is { } sqlEx)
            {
                return ConnectionLevelSqlErrors.Contains(sqlEx.Number);
            }

            return ex is SocketException;
        }

        /// <summary>
        /// EF Core bọc lỗi ghi (SaveChangesAsync) trong DbUpdateException, SqlException thật
        /// nằm ở InnerException — cần bóc tách để policy nhận diện đúng lỗi connection-level.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static SqlException UnwrapSqlException(Exception ex)
        {
            Exception current = ex;

            while (current is not null)
            {
                if (current is SqlException sqlException)
                {
                    return sqlException;
                }

                current = current.InnerException;
            }

            return null;
        }
    }
}