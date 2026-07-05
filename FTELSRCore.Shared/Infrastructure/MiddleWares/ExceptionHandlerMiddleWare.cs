using FTELSRCore.Infrastructure.MiddleWares.Helpers;
using FTELSRCore.Wrappers.ErrorCodes;
using FTELSRCore.Wrappers.ErrorCodes.Catalogs;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace FTELSRCore.Infrastructure.MiddleWares
{
    public class ExceptionHandlerMiddleWare(RequestDelegate _next, ILogger<ExceptionHandlerMiddleWare> logger, ExceptionHandlerMiddleWareModel middleWareModel)
    {
        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception exception)
            {
                StringBuilder message = new($"Method: {httpContext.Request.Method.ToUpper()} | Path: {httpContext.Request.Path} {Environment.NewLine}");

                if (!string.IsNullOrWhiteSpace(httpContext?.Request?.QueryString.Value))
                {
                    message.AppendLine($"[QueryString]: {httpContext?.Request?.QueryString}");
                }

                string requestBody = await ReadRequestBodyHelper.ReadAsync(httpContext);

                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    message.AppendLine($"[RequestBody]: {requestBody}");
                }

                logger.ErrorException(nameof(ExceptionHandlerMiddleWare), nameof(Invoke), e: exception, message: message.ToString());

                if (httpContext.Response.HasStarted)
                {
                    return;
                }

                HttpResponse response = httpContext.Response;

                response.ContentType = MediaTypeNames.Application.Json;

                Result responseModel =
                    Result.FailSystem(
                        message: exception.Message,
                        statusCode: (int)HttpStatusCode.InternalServerError,
                        metadata: BuildMetaHelper.Build(httpContext: httpContext),
                        serviceName: middleWareModel.ServiceName ?? CommonBaseConstant.System);

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

                CatalogsErorrCodeModel wrapperByCode =
                    ResponseWrapperByCodeMapper.FromStatusCode(
                        statusCode: (HttpStatusCode)responseModel.Code, sourceType: ErrorSourceType.General);

                responseModel.Error = new ResultFTELCoreErrorModel
                {
                    Code = wrapperByCode.Code,
                    Retryable = wrapperByCode.Retryable,
                };

                responseModel.Messages = EnvironmentExtensions.GetEnvironment() switch
                {
                    EnvironmentExtensions.EProd or EnvironmentExtensions.EStag =>
                    [
                        "Có sự cố xảy ra vui lòng thử lại sau"
                    ],
                    _ => responseModel.Messages
                };

                await response.WriteAsJsonAsync(responseModel);
            }
        }
    }

    public record ExceptionHandlerMiddleWareModel
    {
        public string ServiceName { get; set; }
    }
}