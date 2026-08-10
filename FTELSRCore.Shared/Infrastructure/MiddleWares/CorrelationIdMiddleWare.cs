using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Context;
using System.Diagnostics;

namespace FTELSRCore.Infrastructure.MiddleWares
{
    public class CorrelationIdMiddleWare(RequestDelegate _next)
    {
        public async Task Invoke(HttpContext httpContext)
        {
            string correlationId;

            if (httpContext.Request.Headers.TryGetValue(
                    key: HeaderConstant.CorrelationIdHeaderKey, out StringValues correlationIds))
            {
                correlationId = correlationIds.FirstOrDefault();
            }
            else
            {
                correlationId = Activity.Current?.TraceId.ToString();

                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString("N");
                }

                httpContext.Request.Headers[HeaderConstant.CorrelationIdHeaderKey] = correlationId;
            }

            httpContext.Response.OnStarting(
                () =>
                {
                    if (httpContext.Response.Headers.TryGetValue(
                            key: HeaderConstant.CorrelationIdHeaderKey, out correlationIds) is false)
                    {
                        httpContext.Response.Headers[HeaderConstant.CorrelationIdHeaderKey] = correlationId;
                    }

                    return Task.CompletedTask;
                });

            using (LogContext.PushProperty(name: HeaderConstant.CorrelationIdHeaderKey, value: correlationId))
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