using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace FTELSRCore.Infrastructure.MiddleWares
{
    public class CorrelationIdMiddleWare(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

        public async Task Invoke(HttpContext httpContext)
        {
            string correlationId;

            if (httpContext.Request.Headers.TryGetValue(
                HeaderConstant.CorrelationIdHeaderKey, out StringValues correlationIds))
            {
                correlationId = correlationIds.FirstOrDefault(
                    x => x.Equals(HeaderConstant.CorrelationIdHeaderKey));
            }
            else
            {
                correlationId = Guid.NewGuid().ToString();

                httpContext.Request.Headers[HeaderConstant.CorrelationIdHeaderKey] = correlationId;
            }

            httpContext.Response.OnStarting(
                () =>
                {
                    if (!httpContext.Response.Headers.TryGetValue(HeaderConstant.CorrelationIdHeaderKey, out correlationIds))
                    {
                        httpContext.Response.Headers[HeaderConstant.CorrelationIdHeaderKey] = correlationId;
                    }

                    return Task.CompletedTask;
                });

            using (LogContext.PushProperty(HeaderConstant.CorrelationIdHeaderKey, correlationId))
            {
                if (httpContext.Response.HasStarted)
                {
                    return;
                }

                await _next.Invoke(httpContext);
            }
        }
    }
}