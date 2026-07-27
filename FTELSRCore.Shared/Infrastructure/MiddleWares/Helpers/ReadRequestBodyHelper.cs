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

                if (!httpContext.Request.Body.CanSeek)
                {
                    return string.Empty;
                }

                httpContext.Request.Body.Position = 0;

                using var boundedBody = new MemoryStream();

                byte[] buffer = new byte[81920];

                long totalBytesRead = 0;

                bool tooLarge = false;

                int bytesRead;

                while ((bytesRead = await httpContext.Request.Body.ReadAsync(buffer)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead > MaxSizeContent)
                    {
                        tooLarge = true;

                        break;
                    }

                    await boundedBody.WriteAsync(buffer.AsMemory(0, bytesRead));
                }

                httpContext.Request.Body.Position = 0;

                if (tooLarge)
                {
                    return "Body too large";
                }

                return Encoding.UTF8.GetString(boundedBody.ToArray());
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}