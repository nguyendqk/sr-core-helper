using FTELSRCore.Models.Https;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FTELSRCore.Utilizes
{
    public abstract class FormatOptions
    {
        public string UserName { get; init; }

        public string Password { get; init; }

        public string Host { get; init; }

        public string Scheme { get; init; }

        public int Port { get; init; }

        public string SubDirectory { get; init; }

        public string SystemOwner { get; init; } = CommonBaseConstant.System;
    }

    public abstract class FormatOptions<TPath> : FormatOptions
    {
        public TPath Paths { get; init; }
    }

    public static class HttpClientUtilizes
    {
        public static HttpOptionModel GetUri(this FormatOptions option, string requestUri, HttpClient httpClient, string token = "")
        {
            var uriBuilder = new UriBuilder
            {
                Host = option.Host,
                Scheme = option.Scheme,
            };

            if (option.Port > 0)
            {
                uriBuilder.Port = option.Port;
            }

            if (!string.IsNullOrEmpty(option.SubDirectory))
            {
                requestUri = option.SubDirectory + DelimiterConstant.CHAR_APOSTROPHE + requestUri;
            }

            var baseAddress = uriBuilder.ToString().Replace("[", string.Empty).Replace("]", string.Empty);

            return new HttpOptionModel()
            {
                Token = token,
                Uri = requestUri,
                Client = httpClient,
                BaseAddress = baseAddress,
                SystemOwner = option.SystemOwner
            };
        }

        public static HttpOptionModel<TValue> GetUri<TValue>(this FormatOptions option, string requestUri, TValue value, HttpClient httpClient, string token = "")
        {
            var uriBuilder = new UriBuilder
            {
                Host = option.Host,
                Scheme = option.Scheme,
            };

            if (option.Port > 0)
            {
                uriBuilder.Port = option.Port;
            }

            if (!string.IsNullOrEmpty(option.SubDirectory))
            {
                requestUri = option.SubDirectory + DelimiterConstant.CHAR_APOSTROPHE + requestUri;
            }

            var baseAddress = uriBuilder.ToString().Replace("[", string.Empty).Replace("]", string.Empty);

            return new HttpOptionModel<TValue>()
            {
                Value = value,
                Token = token,
                Uri = requestUri,
                Client = httpClient,
                BaseAddress = baseAddress,
                SystemOwner = option.SystemOwner
            };
        }

        public static string ToQueryString(object obj)
        {
            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var queryString = new StringBuilder();

            foreach (var property in properties)
            {
                string name = Uri.EscapeDataString(property.Name);
                string value = Uri.EscapeDataString(property.GetValue(obj)?.ToString() ?? string.Empty);

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                queryString.Append(queryString.Length > 0 ? '&' : '?');

                queryString.Append($"{name}={value}");
            }

            return queryString.ToString();
        }

        public static void LogHttpResult(
            this ILogger logger, string className, string methodName, object message, bool isSucceeded = true)
        {
            switch (isSucceeded)
            {
                case true:
                    {
                        logger.HttpResult(className, methodName, message);
                        break;
                    }
                case false:
                    {
                        logger.HttpErrorResult(className, methodName, message);
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Config check time EXP
    /// </summary>
    ///
    public static class TokenExpirationHelperUtilizes
    {
        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        private static (bool, string exp) IsCheckValidToken(string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                return (false, string.Empty);
            }

            string[] jwtSplit = jwt.Split(DelimiterConstant.CHAR_DOT);

            if (jwtSplit.Length < 2)
            {
                return (false, string.Empty);
            }

            string payload = jwtSplit[1];

            byte[] jsonBytes = ParseBase64WithoutPadding(payload);

            Dictionary<string, object> keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            return !keyValuePairs.TryGetValue("exp", out var exp) ? (false, string.Empty) : (true, exp.ToString());
        }

        /// <summary>
        /// Kiểm tra thời gian còn hiệu lực của token.
        /// </summary>
        /// <param name="jwt"></param>
        /// <returns></returns>
        ///
        public static bool IsExpiration(string jwt)
        {
            try
            {
                (bool checkTokenResult, string exp) = IsCheckValidToken(jwt);

                if (checkTokenResult is false)
                {
                    return true;
                }

                // tránh trường hợp exp chứa thông tin milliseconds.
                string exp2 = exp.Split('.')[0];

                DateTime expTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp2.Replace(".", ""))).DateTime;

                DateTime timeUtc = CommonBaseConstant.DateTimeUtc(0);

                TimeSpan diff = expTime - timeUtc;

                return diff.TotalMinutes < 3;
            }
            catch (Exception exception)
            {
                CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(TokenExpirationHelperUtilizes), nameof(IsExpiration), exception: exception);

                return false;
            }
        }

        /// <summary>
        /// Lấy thời gian sống của token.
        /// </summary>
        /// <param name="jwt"></param>
        /// <returns></returns>
        ///
        public static double GetExpirationTime(string jwt)
        {
            try
            {
                (bool checkTokenResult, string exp) = IsCheckValidToken(jwt);

                if (checkTokenResult is false)
                {
                    return CachedBaseConstant.RandomTimeCache(5);
                }

                // tránh trường hợp exp chứa thông tin milliseconds.
                string exp2 = exp.Split('.')[0];

                DateTime timeUtc = CommonBaseConstant.DateTimeUtc(0);

                DateTime expTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp2.Replace(".", ""))).DateTime;

                TimeSpan diff = expTime - timeUtc;

                return Math.Round(diff.TotalMinutes - 2, 0);
            }
            catch (Exception exception)
            {
                CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(TokenExpirationHelperUtilizes), nameof(GetExpirationTime), exception: exception);

                return CachedBaseConstant.RandomTimeCache(5);
            }
        }
    }

    /// <summary>
    /// Config part data.
    /// </summary>
    ///
    public static class HttpContentExtensionsUtilizes
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

        internal static HttpVersionPolicy SetHttpVersion(HttpVersionPolicy versionPolicy = HttpVersionPolicy.RequestVersionOrLower)
        {
            return EnvironmentExtensions.GetEnvironment() switch
            {
                EnvironmentExtensions.ELocal => HttpVersionPolicy.RequestVersionOrLower,
                _ => versionPolicy
            };
        }

        internal static async Task<T> ReadAsJSonAsync<T>(this HttpContent content, ILogger logger)
        {
            string json = string.Empty;

            try
            {
                json = await content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(json)
                    || !json.JSonTryParse(out T result))
                {
                    return default;
                }

                return result;
            }
            catch (Exception exception)
            {
                logger.HttpErrorResult(
                    e: exception,
                    methodName: nameof(ReadAsJSonAsync),
                    className: nameof(HttpContentExtensionsUtilizes),
                    message: $"{nameof(ReadAsJSonAsync)}: {json}");

                return default;
            }
        }

        internal static async Task<T> ReadAsStreamAsync<T>(this HttpContent content, ILogger logger)
        {
            try
            {
                await using Stream readAsStream = await content.ReadAsStreamAsync();

                T result =
                    await JsonSerializer.DeserializeAsync<T>(
                        utf8Json: readAsStream, options: _jsonSerializerOptions).ConfigureAwait(false);

                return result;
            }
            catch (Exception exception)
            {
                string readAsString = await content.ReadAsStringAsync();

                logger.HttpErrorResult(
                    className: nameof(HttpContentExtensionsUtilizes),
                    methodName: nameof(ReadAsStreamAsync),
                    e: exception,
                    message: $"ReadAsStreamAsync: {readAsString}");

                return default;
            }
        }

        internal static HttpClient ConfigHttpClient(this HttpOptionModel option)
        {
            HttpClient client = option.Client;

            if (!string.IsNullOrEmpty(option.BaseAddress))
            {
                client.BaseAddress = new Uri(option.BaseAddress);
            }

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(option.Token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(option.AuthType, option.Token);
            }

            return client;
        }

        internal static async Task<TResponse> ResponseResult<TResponse>(this HttpResponseMessage httpResponseMessage, ILogger logger)
        {
            string mediaType = httpResponseMessage.Content.Headers.ContentType?.MediaType ?? string.Empty;

            if (string.IsNullOrWhiteSpace(mediaType) || mediaType.Contains("text/html"))
            {
                string readAsString =
                    await httpResponseMessage.Content.ReadAsStringAsync();

                throw new CustomException(message: readAsString, statusCode: (int)httpResponseMessage.StatusCode);
            }

            return await httpResponseMessage.Content.ReadAsStreamAsync<TResponse>(logger: logger);
        }

        internal static void ErrorException(
            ref ErrorModel errorModel, HttpOptionModel option, Exception _)
        {
            errorModel.Succeeded = false;
            errorModel.Code = (int)HttpStatusCode.InternalServerError;
            errorModel.Message = $"Hệ thống {option.SystemOwner} đang gặp sự cố tạm thời, vui lòng thử lại sau";
        }

        internal static void ErrorException(
            ref ErrorModel errorModel, HttpOptionModel option, CustomException exception)
        {
            errorModel.Succeeded = false;
            errorModel.Code = exception.Code;
            errorModel.Message = exception.Message ?? $"Hệ thống {option.SystemOwner} đang gặp sự cố tạm thời, vui lòng thử lại sau";
        }

        internal static void ErrorCanceledException(
            ref ErrorModel errorModel, HttpOptionModel option, OperationCanceledException _)
        {
            errorModel.Succeeded = false;
            errorModel.Code = (int)HttpStatusCode.RequestTimeout;
            errorModel.Message = $"Hệ thống {option.SystemOwner} đang xử lý chậm hơn bình thường. Vui lòng thử lại sau ít phút";
        }

        internal static void EnsureSuccessOrException(this HttpResponseMessage httpResponseMessage, ref ErrorModel errorModel)
        {
            if (httpResponseMessage is null)
            {
                throw new CustomException(message: nameof(EnsureSuccessOrException));
            }

            errorModel.Code = (int)httpResponseMessage.StatusCode;
            errorModel.Message = httpResponseMessage.ReasonPhrase;
            errorModel.Succeeded = httpResponseMessage.IsSuccessStatusCode;

            //if (httpResponseMessage is { StatusCode: >= HttpStatusCode.InternalServerError })
            //{
            //    httpResponseMessage.EnsureSuccessStatusCode();
            //}
        }
    }
}