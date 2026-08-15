using System.Diagnostics;

namespace FTELSRCore.CQRS.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse>(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull, IRequest<TResponse>
        where TResponse : notnull, IResult
    {
        private static readonly ActivitySource ActivitySource = new(OpenTelemetryConstant.LoggingBehaviorActivitySource);

        public async Task<TResponse> Handle(TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            using Activity activity = ActivitySource.StartActivity(typeof(TRequest).Name, ActivityKind.Internal);

            activity?.SetTag("mediatr.name", $"CQRS [{typeof(TRequest).Name}]");

            try
            {
                TResponse result =
                    await MeasureExecutionTimeExtensions.InvokeForMediaR(
                        logger: logger,
                        func: async () =>
                        {
                            return await next();
                        },
                        measureByKey: $"{nameof(LoggingBehavior<TRequest, TResponse>)}_{typeof(TRequest).Name}",
                        desiredTime: 5, cancellationToken: cancellationToken);

                activity?.SetStatus(ActivityStatusCode.Ok);

                return result;
            }
            catch (Exception exception)
            {
                activity?.SetStatus(ActivityStatusCode.Error);

                activity?.AddException(exception);

                throw;
            }
        }
    }
}