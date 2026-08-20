using FTELSRCore.Models.Https;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Web;
using static FTELSRCore.Infrastructure.Extensions.Helpers.SerilogProviderExtensions.Formatters.SRKafkaLogFormatter;

namespace FTELSRCore.Utilizes
{
    public static class CallApiWithHttp<TRequest, TResponse> where TResponse : class
    {
        #region :::::::::::::::::::::::::::::: GET ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu GET tới <c>option.Uri</c>, tự động build query string từ <typeparamref name="TRequest"/>
        /// (option.Value) qua <see cref="ParseModelToQueryString"/>, đính kèm Bearer token nếu có, đo thời gian
        /// thực thi và ghi log tracing (URL/Option/Result) khi hoàn tất.
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (query params), HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> GetAsJSonAsync(
            HttpOptionModel<TRequest> option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            string urlQueryString = option.Value is null
                ? option.Uri
                : $"{option.Uri}?{ParseModelToQueryString(option.Value)}";

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, urlQueryString)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () =>
                            await client.SendAsync(
                                request: httpRequestMessage,
                                completionOption: option.CompletionOption,
                                cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: urlQueryString,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option: option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Get.Method,
                    methodName: nameof(GetAsJSonAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"URL\":\"{0}\",\"Option\":{1},\"Result\":{2}}}",
                                    urlQueryString, System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Giống <see cref="GetAsJSonAsync"/> (GET, build query string từ TRequest, Bearer token, đo thời gian,
        /// log tracing) nhưng trả về thêm <see cref="HttpResponseHeaders"/> của response để caller đọc header nếu cần.
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (query params), HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response, ErrorModel, HttpResponseHeaders) — data/headers null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel, HttpResponseHeaders)> GetAsJSonAndHeaderAsync(
            HttpOptionModel<TRequest> option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            string urlQueryString = option.Value is null
                ? option.Uri
                : $"{option.Uri}?{ParseModelToQueryString(option.Value)}";

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel, HttpResponseHeaders Headers) result = (null, errorModel, null);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, urlQueryString)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async ()
                            => await client.SendAsync(
                                request: httpRequestMessage,
                                completionOption: option.CompletionOption,
                                cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: urlQueryString,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel, httpResponseMessage.Headers);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: exception.Message);

                return (null, errorModel, null);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel, null);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel, null);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Get.Method,
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"URL\":\"{0}\",\"Option\":{1},\"Result\":{2}}}",
                                    urlQueryString, System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Gửi yêu cầu GET với query string build từ TRequest (dùng <see cref="HttpClientUtilizes.ToQueryString"/>),
        /// cho phép truyền thêm các header tùy chỉnh — được add trực tiếp vào <c>HttpRequestMessage.Headers</c>
        /// (không phải <c>DefaultRequestHeaders</c> của client dùng chung).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (query params), HttpClient.</param>
        /// <param name="headers">Danh sách header tùy chỉnh cần thêm vào request.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> GetAsJSonCustomHeaderAsync(
            HttpOptionModel<TRequest> option, IEnumerable<KeyValuePair<string, string>> headers, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            string urlQueryString = option.Value is null
                ? option.Uri
                : $"{option.Uri}{HttpClientUtilizes.ToQueryString(option.Value)}";

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using var requestMessage = new HttpRequestMessage(method: HttpMethod.Get, urlQueryString)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                foreach (KeyValuePair<string, string> header in headers)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                                request: requestMessage,
                                completionOption: option.CompletionOption,
                                cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        measureByKey: option.Uri,
                        desiredTime: desiredTime,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Get.Method,
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: GET ::::::::::::::::::::::::::::::

        #region :::::::::::::::::::::::::::::: POST ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu POST với body dạng <c>application/x-www-form-urlencoded</c> (<see cref="FormUrlEncodedContent"/>)
        /// từ <c>Dictionary&lt;string, string&gt;</c>. Tự thiết lập BaseAddress/Authorization trên client trước khi gửi.
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (dữ liệu form), Client.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PostFormUrlEncodedAsync(
            HttpOptionModel<Dictionary<string, string>> option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.Client;

                if (!string.IsNullOrEmpty(option.BaseAddress))
                {
                    client.BaseAddress = new Uri(option.BaseAddress);
                }

                if (!string.IsNullOrEmpty(option.Token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(scheme: option.AuthType, option.Token);
                }

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, option.Uri)
                {
                    Version = HttpVersion.Version20,
                    Content = new FormUrlEncodedContent(option.Value),
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        measureByKey: option.Uri,
                        desiredTime: desiredTime,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostFormUrlEncodedAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostFormUrlEncodedAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostFormUrlEncodedAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Post.Method,
                    methodName: nameof(PostFormUrlEncodedAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Gửi yêu cầu POST với body JSON (serialize <c>option.Value</c> bằng <see cref="System.Text.Json.JsonSerializer"/>,
        /// Content-Type <c>application/json</c>).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (request body), HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PostAsJSonAsync(
            HttpOptionModel<TRequest> option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            string json = System.Text.Json.JsonSerializer.Serialize(option.Value);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage =
                    new HttpRequestMessage(HttpMethod.Post, option.Uri)
                    {
                        Content = content,
                        Version = HttpVersion.Version20,
                        VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                    };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        measureByKey: option.Uri,
                        desiredTime: desiredTime,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Post.Method,
                    methodName: nameof(PostAsJSonAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Gửi yêu cầu POST dạng <c>multipart/form-data</c>: đọc toàn bộ nội dung mỗi <see cref="IFormFile"/> vào
        /// <c>byte[]</c> rồi add làm <see cref="ByteArrayContent"/>, đồng thời duyệt reflection các property khác của
        /// TRequest (option.Value) để add làm field form bổ sung (mảng string được add từng item riêng).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (field form bổ sung), HttpClient.</param>
        /// <param name="files">Danh sách file cần upload; file rỗng (Length == 0) sẽ bị bỏ qua.</param>
        /// <param name="fileParameterName">Tên field form dùng cho phần nội dung file.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PostAsFileAsync(
            HttpOptionModel<TRequest> option, IEnumerable<IFormFile> files, string fileParameterName, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                // Create multipart content
                MultipartFormDataContent content = [];

                // Add the file content
                foreach (var file in files ?? [])
                {
                    if (file.Length == 0)
                        continue;

                    // Use a 'using' block to ensure the file stream is disposed of correctly after use
                    await using var fileStream = file.OpenReadStream();

                    byte[] fileBytes = new byte[fileStream.Length];

                    _ = await fileStream.ReadAsync(fileBytes.AsMemory(0, (int)fileStream.Length),
                        cancellationToken: cancellationToken);

                    var fileContent = new ByteArrayContent(fileBytes);

                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                    content.Add(fileContent, fileParameterName, file.FileName);
                }

                // Add additional form fields (if needed)
                if (option.Value != null)
                {
                    foreach (var property in typeof(TRequest).GetProperties())
                    {
                        var value = property.GetValue(option.Value);
                        switch (value)
                        {
                            case null:
                                continue;
                            case IEnumerable<string> stringList when property.PropertyType != typeof(string):
                                {
                                    // Nếu là dạng mảng, add riêng từng item
                                    foreach (var item in stringList)
                                    {
                                        content.Add(new StringContent(item), property.Name);
                                    }

                                    break;
                                }
                            default:
                                content.Add(new StringContent(value.ToString()!), property.Name);
                                break;
                        }
                    }
                }

                var cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: new HttpRequestMessage(HttpMethod.Post, option.Uri)
                            {
                                Content = content,
                                Version = HttpVersion.Version20,
                                VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                            },
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token), logger: logger, measureByKey: $"{option.BaseAddress}{option.Uri}",
                        desiredTime: desiredTime,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Post.Method,
                    methodName: nameof(PostAsFileAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Biến thể của <see cref="PostAsFileAsync"/>: dùng <see cref="StreamContent"/> để stream trực tiếp từ
        /// <c>file.OpenReadStream()</c> thay vì đọc hết vào <c>byte[]</c> (tiết kiệm bộ nhớ với file lớn), tự set
        /// <c>ContentDisposition</c> (Name/FileName/FileNameStar) rõ ràng cho từng file, dùng HTTP/1.0.
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (field form bổ sung), HttpClient.</param>
        /// <param name="files">Danh sách file cần upload; file rỗng (Length == 0) sẽ bị bỏ qua.</param>
        /// <param name="fileParameterName">Tên field form dùng cho phần nội dung file.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PostAsFileV2Async(
            HttpOptionModel<TRequest> option, IEnumerable<IFormFile> files, string fileParameterName, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                // Create multipart content
                MultipartFormDataContent content = [];

                // Add the file content
                foreach (var file in files ?? [])
                {
                    if (file.Length == 0)
                        continue;
                    var stream = file.OpenReadStream();
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType =
                        new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

                    streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = $"\"{fileParameterName}\"",
                        FileName = $"\"{file.FileName}\"",
                        FileNameStar = file.FileName
                    };

                    content.Add(streamContent, fileParameterName, file.FileName);
                }

                // Add additional form fields (if needed)
                if (option.Value != null)
                {
                    foreach (var property in typeof(TRequest).GetProperties())
                    {
                        var value = property.GetValue(option.Value);
                        if (value == null || value is IFormFile) continue;

                        if (value is IEnumerable<string> stringList && property.PropertyType != typeof(string))
                        {
                            // Nếu là dạng mảng, add riêng từng item
                            foreach (var item in stringList)
                            {
                                content.Add(new StringContent(item), property.Name);
                            }
                        }
                        else
                        {
                            content.Add(new StringContent(value.ToString()!), property.Name);
                        }
                    }
                }

                var cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: new HttpRequestMessage(HttpMethod.Post, option.Uri)
                            {
                                Content = content,
                                Version = HttpVersion.Version10,
                                VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                            },
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token),
                        logger: logger,
                        measureByKey: $"{option.BaseAddress}{option.Uri}",
                        desiredTime: desiredTime,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileV2Async),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileV2Async),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileV2Async),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Post.Method,
                    methodName: nameof(PostAsFileV2Async),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Gửi yêu cầu POST với body JSON, cho phép truyền thêm header tùy chỉnh — được add vào
        /// <c>client.DefaultRequestHeaders</c> (lưu ý: set trên client dùng chung, khác với
        /// <see cref="GetAsJSonCustomHeaderAsync"/> add trực tiếp vào HttpRequestMessage.Headers).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (request body), HttpClient.</param>
        /// <param name="headers">Danh sách header tùy chỉnh cần thêm vào client trước khi gửi.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PostWithHeadersAsJSonAsync(
            HttpOptionModel<TRequest> option, Dictionary<string, string> headers, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            string json = System.Text.Json.JsonSerializer.Serialize(option.Value);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);

                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                using CancellationTokenSource cancellationTokenSource =
                      CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, option.Uri)
                {
                    Content = content,
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                     message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Post.Method,
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: POST ::::::::::::::::::::::::::::::

        #region :::::::::::::::::::::::::::::: PUT ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu PUT với body JSON (serialize <c>option.Value</c> bằng <see cref="System.Text.Json.JsonSerializer"/>).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (request body), HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PutAsJSonAsync(
            HttpOptionModel<TRequest> option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            string json = System.Text.Json.JsonSerializer.Serialize(option.Value);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, option.Uri)
                {
                    Content = content,
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Put.Method,
                    methodName: nameof(PutAsJSonAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: PUT ::::::::::::::::::::::::::::::

        #region :::::::::::::::::::::::::::::: DELETE ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu DELETE tới <c>option.Uri</c>, build query string từ TRequest (option.Value) qua
        /// <see cref="ParseModelToQueryString"/> giống <see cref="GetAsJSonAsync"/> (DELETE không có body).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (query params), HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        public static async Task<(TResponse, ErrorModel)> DeleteAsJSonAsync(
            HttpOptionModel<TRequest> option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            string urlQueryString = option.Value is null
                ? option.Uri
                : $"{option.Uri}?{ParseModelToQueryString(option.Value)}";

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using CancellationTokenSource cancellationTokenSource =
                     CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, urlQueryString)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        measureByKey: option.Uri,
                        desiredTime: desiredTime,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Delete.Method,
                    methodName: nameof(DeleteAsJSonAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"URL\":\"{0}\",\"Option\":{1},\"Result\":{2}}}",
                                    urlQueryString, System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: DELETE ::::::::::::::::::::::::::::::

        #region :::::::::::::::::::::::::::::: PATCH ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu PATCH với body JSON (serialize <c>option.Value</c> bằng <see cref="System.Text.Json.JsonSerializer"/>).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, Value (request body), HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PatchAsJSonAsync(
            HttpOptionModel<TRequest> option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            string json = System.Text.Json.JsonSerializer.Serialize(option.Value);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, option.Uri)
                {
                    Content = content,
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Patch.Method,
                    methodName: nameof(PatchAsJSonAsync),
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: PATCH ::::::::::::::::::::::::::::::

        /// <summary>
        /// Parse data to queryString
        /// </summary>
        /// <param name="data"></param>
        ///
        private static string ParseModelToQueryString(TRequest data)
        {
            StringBuilder result = new();

            Type type = data.GetType();

            foreach (PropertyInfo item in type.GetProperties())
            {
                var value = item.GetValue(data, index: null);

                if (value is null || value.Equals("null") || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    continue;
                }

                JsonPropertyNameAttribute jsonAttr = item.GetCustomAttribute<JsonPropertyNameAttribute>();

                string key = !string.IsNullOrWhiteSpace(jsonAttr?.Name) ? jsonAttr?.Name?.Trim() : item.Name?.Trim();

                _ = value is string
                    ? result.Append($"{key}={HttpUtility.UrlEncode(value.ToString())}&")
                    : result.Append($"{key}={value.ToString()}&");
            }

            if (!result.ToString().EndsWith('&'))
            {
                return result.ToString();
            }

            StringBuilder resultCut = new();

            _ = resultCut.Append(result.ToString(), 0, result.Length - 1);

            return resultCut.ToString();
        }
    }

    public static class CallApi<TResponse> where TResponse : class
    {
        #region :::::::::::::::::::::::::::::: GET ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu GET tới <c>option.Uri</c> (không có TRequest nên không build query string), đính kèm Bearer
        /// token nếu có, đo thời gian thực thi và ghi log tracing khi hoàn tất.
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> GetAsJSonAsync(
            HttpOptionModel option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, option.Uri)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                   message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Get.Method,
                    methodName: nameof(GetAsJSonAsync),
                    className: nameof(CallApi<TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Giống <see cref="GetAsJSonAsync(HttpOptionModel, ILogger, HttpVersionPolicy, int, int, CancellationToken)"/>
        /// nhưng trả về thêm <see cref="HttpResponseHeaders"/> của response.
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response, ErrorModel, HttpResponseHeaders) — data/headers null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel, HttpResponseHeaders)> GetAsJSonAndHeaderAsync(
            HttpOptionModel option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel, HttpResponseHeaders Headers) result = (null, errorModel, null);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, option.Uri)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel,
                    httpResponseMessage.Headers);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: exception.Message);

                return (null, errorModel, null!);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel, null!);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel, null!);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Get.Method,
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    className: nameof(CallApi<TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Gửi yêu cầu GET tới <c>option.Uri</c>, cho phép truyền thêm header tùy chỉnh — được add trực tiếp vào
        /// <c>HttpRequestMessage.Headers</c> (không phải <c>DefaultRequestHeaders</c> của client dùng chung).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, HttpClient.</param>
        /// <param name="headers">Danh sách header tùy chỉnh cần thêm vào request.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> GetAsJSonCustomHeaderAsync(
           HttpOptionModel option, IEnumerable<KeyValuePair<string, string>> headers, ILogger logger,
           HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
           int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using CancellationTokenSource cancellationTokenSource =
                      CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using HttpRequestMessage requestMessage = new(method: HttpMethod.Get, option.Uri)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                foreach (KeyValuePair<string, string> header in headers)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: requestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token)
                        .ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Get.Method,
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    className: nameof(CallApi<TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: GET ::::::::::::::::::::::::::::::

        #region :::::::::::::::::::::::::::::: POST ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu POST tới <c>option.Uri</c> <b>không có body</b> (không có TRequest nên không set Content) —
        /// dùng cho endpoint dạng trigger/action không cần dữ liệu gửi kèm.
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PostAsJSonAsync(
            HttpOptionModel option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, option.Uri)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Post.Method,
                    methodName: nameof(PostAsJSonAsync),
                    className: nameof(CallApi<TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Gửi yêu cầu POST với body là <see cref="MultipartFormDataContent"/> do caller tự xây dựng sẵn (không tự
        /// build từ TRequest như <see cref="CallApiWithHttp{TRequest,TResponse}.PostAsFileAsync"/>). Nếu
        /// <paramref name="form"/> null, trả về ngay (default) mà không gửi request.
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, HttpClient.</param>
        /// <param name="form">Nội dung multipart/form-data đã được caller chuẩn bị sẵn.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi hoặc form null.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PostFormDataAsJSonAsync(
            HttpOptionModel option, MultipartFormDataContent form, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            if (form is null)
            {
                return result;
            }

            try
            {
                HttpClient client = option.Client;

                if (!string.IsNullOrEmpty(option.BaseAddress))
                {
                    client.BaseAddress = new Uri(option.BaseAddress);
                }

                if (!string.IsNullOrEmpty(option.Token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(scheme: option.AuthType, option.Token);
                }

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, option.Uri)
                {
                    Content = form,
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () =>
                            await client.SendAsync(
                                request: httpRequestMessage,
                                completionOption: option.CompletionOption,
                                cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostFormDataAsJSonAsync),
                     message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostFormDataAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostFormDataAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Post.Method,
                    methodName: nameof(PostFormDataAsJSonAsync),
                    className: nameof(CallApi<TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        /// <summary>
        /// Gửi yêu cầu POST tới <c>option.Uri</c> <b>không có body</b>, cho phép truyền thêm header tùy chỉnh —
        /// được add vào <c>client.DefaultRequestHeaders</c>.
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, HttpClient.</param>
        /// <param name="headers">Danh sách header tùy chỉnh cần thêm vào client trước khi gửi.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PostWithHeadersAsJSonAsync(
            HttpOptionModel option, Dictionary<string, string> headers, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                using CancellationTokenSource cancellationTokenSource =
                     CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, option.Uri)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                     message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Post.Method,
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    className: nameof(CallApi<TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: POST ::::::::::::::::::::::::::::::

        #region :::::::::::::::::::::::::::::: PUT ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu PUT tới <c>option.Uri</c> <b>không có body</b> (không có TRequest nên không set Content).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PutAsJSonAsync(
            HttpOptionModel option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, option.Uri)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                     message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Put.Method,
                    methodName: nameof(PutAsJSonAsync),
                    className: nameof(CallApi<TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: PUT ::::::::::::::::::::::::::::::

        #region :::::::::::::::::::::::::::::: DELETE ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu DELETE tới <c>option.Uri</c> (không có TRequest nên không build query string).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> DeleteAsJSonAsync(
            HttpOptionModel option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using CancellationTokenSource cancellationTokenSource =
                    CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, option.Uri)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                     message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Delete.Method,
                    methodName: nameof(DeleteAsJSonAsync),
                    className: nameof(CallApi<TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: DELETE ::::::::::::::::::::::::::::::

        #region :::::::::::::::::::::::::::::: PATCH ::::::::::::::::::::::::::::::

        /// <summary>
        /// Gửi yêu cầu PATCH tới <c>option.Uri</c> <b>không có body</b> (không có TRequest nên không set Content).
        /// </summary>
        /// <param name="option">Thông tin cấu hình request: Uri, BaseAddress, Token, HttpClient.</param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy">Chính sách thương lượng phiên bản HTTP.</param>
        /// <param name="desiredTime">Ngưỡng thời gian (giây) để cảnh báo request chậm.</param>
        /// <param name="cancellationTokenTime">Timeout (giây) cho toàn bộ request.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Tuple (dữ liệu response đã deserialize, ErrorModel) — data null nếu lỗi.</returns>
        ///
        public static async Task<(TResponse, ErrorModel)> PatchAsJSonAsync(
            HttpOptionModel option, ILogger logger,
            HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower, int desiredTime = 3,
            int cancellationTokenTime = 15, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long start = Stopwatch.GetTimestamp();

            ErrorModel errorModel = new();

            (TResponse data, ErrorModel errorModel) result = (null, errorModel);

            try
            {
                HttpClient client = option.ConfigHttpClient();

                using var cancellationTokenSource =
                      CancellationTokenHelper.CreateLinkedTokenWithTimeout(cancellationToken, cancellationTokenTime);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, option.Uri)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpContentExtensionsUtilizes.SetHttpVersion(versionPolicy)
                };

                HttpResponseMessage httpResponseMessage =
                    await MeasureExecutionTimeExtensions.InvokeForHTTP(
                        func: async () => await client.SendAsync(
                            request: httpRequestMessage,
                            completionOption: option.CompletionOption,
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false),
                        logger: logger,
                        desiredTime: desiredTime,
                        measureByKey: option.Uri,
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

                httpResponseMessage.EnsureSuccessOrException(ref errorModel);

                result = (data: await httpResponseMessage.ResponseResult<TResponse>(logger: logger), errorModel);

                return result;
            }
            catch (OperationCanceledException exception)
            {
                HttpContentExtensionsUtilizes.ErrorCanceledException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                     message: exception.Message);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    message: exception.Message, e: exception);

                return (null, errorModel);
            }
            finally
            {
                logger.HttpResultWithTracing(
                    httpMethod: HttpMethod.Patch.Method,
                    methodName: nameof(PatchAsJSonAsync),
                    className: nameof(CallApi<TResponse>),

                    uri: option.Uri,
                    systemOwner: option.SystemOwner,
                    statusCode: result.errorModel?.Code.ToString(),
                    direction: HttpClientUtilizes.HasPort(option.BaseAddress) switch
                    {
                        true => DirectionType.Inbound,

                        false => DirectionType.Outbound,
                    },
                    responseTimeMs: (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency,
                    message: string.Format(
                                    "{{\"Option\":{0},\"Result\":{1}}}",
                                    System.Text.Json.JsonSerializer.Serialize(option), JsonConvert.SerializeObject(result)));
            }
        }

        #endregion :::::::::::::::::::::::::::::: PATCH ::::::::::::::::::::::::::::::
    }
}