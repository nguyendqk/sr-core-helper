using Microsoft.AspNetCore.Http;

namespace FTELSRCore.Infrastructure.MiddleWares.Helpers
{
    public class BuildMetaHelper
    {
        public static ResultFTELCoreMetadataModel Build(HttpContext httpContext)
        {
            return new ResultFTELCoreMetadataModel
            {
                Request_Id = httpContext?.TraceIdentifier,
                Trace_Id = ResolveTraceId(httpContext: httpContext),
                Timestamp = CommonBaseConstant.DateTimeUtc().ToString("o")
            };
        }

        private static string ResolveTraceId(HttpContext httpContext)
        {
            try
            {
                if (httpContext?.Request?.Headers is null)
                {
                    return null;
                }

                return httpContext.Request.Headers.TryGetValue(
                            key: HeaderConstant.CorrelationIdHeaderKey, out var values)
                      ? values.FirstOrDefault() : null;
            }
            catch
            {
                return null;
            }
        }
    }
}