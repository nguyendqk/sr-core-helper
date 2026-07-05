using Microsoft.AspNetCore.Http;
using System.Text;

namespace FTELSRCore.Infrastructure.MiddleWares.Helpers
{
    public class ReadRequestBodyHelper
    {
        public static async Task<string> ReadAsync(HttpContext httpContext)
        {
            const long MaxSizeContent = 5 * 1024 * 1024;

            try
            {
                if (httpContext.Response.HasStarted)
                {
                    return string.Empty;
                }

                if (httpContext.Request.ContentType?.Contains("multipart/form-data") is true)
                {
                    return "File upload";
                }

                if (httpContext.Request.ContentLength is > MaxSizeContent)
                {
                    return "Body too large";
                }

                httpContext.Request.Body.Position = 0;

                using var readerBody = new StreamReader(
                    httpContext.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                string body = await readerBody.ReadToEndAsync();

                httpContext.Request.Body.Position = 0;

                return body;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}