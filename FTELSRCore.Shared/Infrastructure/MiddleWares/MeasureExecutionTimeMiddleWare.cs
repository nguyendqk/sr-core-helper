using FTELSRCore.Infrastructure.MiddleWares.Helpers;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace FTELSRCore.Infrastructure.MiddleWares
{
    public class MeasureExecutionTimeMiddleWare(RequestDelegate _next, ILogger<MeasureExecutionTimeMiddleWare> logger)
    {
        public async Task Invoke(HttpContext httpContext)
        {
            long start = Stopwatch.GetTimestamp();

            try
            {
                await _next(httpContext);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                double elapseds = elapsedMs / 1000.0;

                StringBuilder message =
                    new($"Method: {httpContext.Request.Method.ToUpper()} | Path: {httpContext.Request.Path} {Environment.NewLine}");

                if (httpContext.Response.StatusCode >= (int)HttpStatusCode.BadRequest)
                {
                    if (!string.IsNullOrWhiteSpace(httpContext?.Request?.QueryString.Value))
                    {
                        message.AppendLine($"[QueryString]: {httpContext?.Request?.QueryString}");
                    }

                    string requestBody = await ReadRequestBodyHelper.ReadAsync(httpContext);

                    if (!string.IsNullOrWhiteSpace(requestBody))
                    {
                        message.AppendLine($"[RequestBody]: {requestBody}");
                    }
                }

                if (elapseds >= 10)
                {
                    logger.Warning(nameof(MeasureExecutionTimeMiddleWare), nameof(Invoke),
                        message: $"[PERFORMANCE] Long Running Request took {elapsedMs} milliseconds for {message}");
                }

                logger.Response(className: nameof(MeasureExecutionTimeMiddleWare), methodName: nameof(Invoke),
                    latency: elapsedMs, message: message.ToString());
            }
        }
    }
}