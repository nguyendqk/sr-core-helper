using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace FTELSRCore.Infrastructure.MiddleWares
{
    public class MeasureExecutionTimeMiddleWare(RequestDelegate next, ILogger<MeasureExecutionTimeMiddleWare> logger)
    {
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

        public async Task Invoke(HttpContext context)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            long start = Stopwatch.GetTimestamp();

            try
            {
                await _next(context);
            }
            finally
            {
                long elapsedMs =
                    (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                double elapseds = elapsedMs / 1000.0;

                StringBuilder message = new();

                if (context.Response.StatusCode >= (int)HttpStatusCode.BadRequest)
                {
                    message.Append($"| Method: {context.Request.Method.ToUpper()} | Path: {context.Request.Path} {Environment.NewLine}");

                    if (!string.IsNullOrWhiteSpace(context?.Request?.QueryString.Value))
                    {
                        message.AppendLine($"[QueryString]: {context?.Request?.QueryString}");
                    }

                    string requestForUser = await GetRequestForUserWithFromBody(context);

                    if (!string.IsNullOrWhiteSpace(requestForUser))
                    {
                        message.AppendLine($"[RequestBody]: {JsonConvert.SerializeObject(requestForUser)}");
                    }
                }

                if (elapseds >= 10)
                {
                    logger.Warning(nameof(MeasureExecutionTimeMiddleWare), nameof(Invoke),
                        message: $"[PERFORMANCE] Long Running Request took {elapsedMs} milliseconds " + message.ToString());
                }

                logger.Response(className: nameof(MeasureExecutionTimeMiddleWare), methodName: nameof(Invoke),
                    latency: elapsedMs, message: context.Request.Path);
            }
        }

        private static async Task<string> GetRequestForUserWithFromBody(HttpContext context)
        {
            try
            {
                if (!(context.Request.ContentLength is not null && context.Request.ContentLength > 0))
                {
                    return string.Empty;
                }

                context.Request.EnableBuffering();

                context.Request.Body.Position = 0;

                using var readerBody = new StreamReader(
                    context.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true
                );

                return await readerBody.ReadToEndAsync();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}