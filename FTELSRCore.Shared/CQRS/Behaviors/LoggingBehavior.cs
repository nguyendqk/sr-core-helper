namespace FTELSRCore.CQRS.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull, IRequest<TResponse>
        where TResponse : notnull, IResult
    {
        public async Task<TResponse> Handle(TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            return
               await MeasureExecutionTimeExtensions.InvokeForMediaR(
                   func: async () =>
                   {
                       return await next();
                   }, logger: logger, measureByKey: $"{nameof(LoggingBehavior<TRequest, TResponse>)}_{typeof(TRequest).Name}", desiredTime: 5, cancellationToken: cancellationToken);
        }
    }
}