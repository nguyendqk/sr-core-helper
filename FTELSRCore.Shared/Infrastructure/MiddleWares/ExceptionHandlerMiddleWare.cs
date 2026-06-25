using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace FTELSRCore.Infrastructure.MiddleWares
{
    public class ExceptionHandlerMiddleWare(RequestDelegate next, ILogger<ExceptionHandlerMiddleWare> logger)
    {
        private const long MaxSizeContent = 5 * 1024 * 1024; // 5MB là model tối da log dữ liệu.

        public async Task Invoke(HttpContext context)
        {
            string requestForUser = await GetRequestForUserWithFromBody(context);

            if (!string.IsNullOrWhiteSpace(requestForUser))
            {
                context.Request.Body.Position = 0;
            }

            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                StringBuilder message = new($"Method: {context.Request.Method.ToUpper()} | Path: {context.Request.Path} {Environment.NewLine}");

                if (!string.IsNullOrWhiteSpace(context?.Request?.QueryString.Value))
                {
                    message.AppendLine($"[QueryString]: {context?.Request?.QueryString}");
                }

                if (!string.IsNullOrWhiteSpace(requestForUser))
                {
                    message.AppendLine($"[RequestBody]: {JsonConvert.SerializeObject(requestForUser)}");
                }

                logger.ErrorException(nameof(ExceptionHandlerMiddleWare), nameof(Invoke), e: exception, message: message.ToString());

                HttpResponse response = context.Response;

                if (context.Response.HasStarted)
                {
                    return;
                }

                response.ContentType = MediaTypeNames.Application.Json;

                Result responseModel = Result.FailSystem(message: exception.Message,
                                                         statusCode: (int)HttpStatusCode.InternalServerError);

                switch (exception)
                {
                    case UnauthorizedAccessException:
                        {
                            response.StatusCode = (int)HttpStatusCode.Forbidden;
                            responseModel.Code = (int)HttpStatusCode.Forbidden;
                            responseModel.Status = nameof(HttpStatusCode.Forbidden);
                            break;
                        }
                    case KeyNotFoundException:
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            responseModel.Code = (int)HttpStatusCode.NotFound;
                            responseModel.Status = nameof(HttpStatusCode.NotFound);
                            break;
                        }
                    case CustomException customerException:
                        {
                            response.StatusCode = customerException.Code;
                            responseModel.Code = customerException.Code;
                            responseModel.Status = customerException.Status;
                            break;
                        }
                    default:
                        {
                            response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            responseModel.Code = (int)HttpStatusCode.InternalServerError;
                            responseModel.Status = nameof(HttpStatusCode.InternalServerError);
                            break;
                        }
                }

                responseModel.Messages = EnvironmentExtensions.GetEnvironment() switch
                {
                    EnvironmentExtensions.EProd or EnvironmentExtensions.EStag =>
                    [
                        "Có sự cố xảy ra vui lòng thử lại sau !"
                    ],
                    _ => responseModel.Messages
                };

                await response.WriteAsync(responseModel.ToJSon());
            }
        }

        private static async Task<string> GetRequestForUserWithFromBody(HttpContext context)
        {
            try
            {
                if (context.Response.HasStarted)
                {
                    return string.Empty;
                }

                if (context.Request.ContentLength > MaxSizeContent)
                {
                    return "Body to large";
                }

                context.Request.EnableBuffering();

                using StreamReader reader = new(context.Request.Body, leaveOpen: true);

                return await reader.ReadToEndAsync();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}