using FTELSRCore.Models.Https;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Web;

namespace FTELSRCore.Utilizes
{
    public static class CallApiWithHttp<TRequest, TResponse> where TResponse : class
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    uri: option.Uri,
                    methodName: nameof(GetAsJSonAsync),
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {urlQueryString} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                    message: string.Empty);

                return (null, errorModel, null);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel, null);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel, null);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    uri: option.Uri,
                    latency: elapsedMs,
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {urlQueryString} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostFormUrlEncodedAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostFormUrlEncodedAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostFormUrlEncodedAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

                StringContent content = new(json, Encoding.UTF8, "application/json");

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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

                StringContent content = new(json, Encoding.UTF8, "application/json");

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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"result: {result.data?.ToJSon()} -- param: {json}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="files"></param>
        /// <param name="fileParameterName"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="files"></param>
        /// <param name="fileParameterName"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileV2Async),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileV2Async),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostAsFileV2Async),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="headers"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

                StringContent content = new(json, Encoding.UTF8, "application/json");

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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="headers"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

                StringContent content = new(json, Encoding.UTF8, "application/json");

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
                     message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApiWithHttp<TRequest, TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

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
        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                   message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                    message: string.Empty);

                return (null, errorModel, null!);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel, null!);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel, null!);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonAndHeaderAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="headers"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(GetAsJSonCustomHeaderAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
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
                    methodName: nameof(PostAsJSonAsync),
                    message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="form"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
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
                     message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostFormDataAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostFormDataAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostFormDataAsJSonAsync),
                    statusCode: result.errorModel?.Code.ToString(),
                    latency: elapsedMs, uri: option.Uri,
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                     message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PutAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                     message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(DeleteAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                     message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PatchAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <param name="headers"></param>
        /// <param name="logger"></param>
        /// <param name="versionPolicy"></param>
        /// <param name="desiredTime"></param>
        /// <param name="cancellationTokenTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

                if (cancellationTokenTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(cancellationTokenTime);
                }

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
                     message: string.Empty);

                return (null, errorModel);
            }
            catch (CustomException exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            catch (Exception exception)
            {
                HttpContentExtensionsUtilizes.ErrorException(ref errorModel, option, exception);

                logger.HttpErrorResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    message: string.Empty, e: exception);

                return (null, errorModel);
            }
            finally
            {
                long elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

                logger.HttpResult(
                    className: nameof(CallApi<TResponse>),
                    methodName: nameof(PostWithHeadersAsJSonAsync),
                    uri: option.Uri,
                    latency: elapsedMs,
                    statusCode: result.errorModel?.Code.ToString(),
                    message: $"{JsonConvert.SerializeObject(option)} -- {JsonConvert.SerializeObject(result)}");
            }
        }
    }
}