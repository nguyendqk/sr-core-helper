using System.Text;

namespace FTELSRCore.Helpers
{
    public static class ObfuscationHelpers
    {
        /// <summary>
        /// Xử lý chuyển đổi dữ liệu.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="CustomException"></exception>
        ///
        public static bool DecodeDataFromSR<T>(
            this string data, out T result, string key, ILogger logger = null)
        {
            static string XorDecodeFromBase64(string encodedText, string key)
            {
                // 1) base64 -> bytes
                byte[] encodedBytes = Convert.FromBase64String(encodedText);

                // 2) XOR bytes với key
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] outBytes = new byte[encodedBytes.Length];

                for (int i = 0; i < encodedBytes.Length; i++)
                {
                    outBytes[i] = (byte)(encodedBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                // 3) bytes -> string (JSON)
                return Encoding.UTF8.GetString(outBytes);
            }

            if (string.IsNullOrWhiteSpace(key)
                || string.IsNullOrWhiteSpace(data)
                || data.Trim().Equals("null", StringComparison.OrdinalIgnoreCase)
                || data.Trim() == "{}" || data.Trim() == "[]")
            {
                result = default;

                return false;
            }

            try
            {
                string plainJson = XorDecodeFromBase64(encodedText: data, key: key);

                return plainJson.JSonTryParse(out result, logger: logger);
            }
            catch (Exception exception)
            {
                string message = $"DecodeDataFromSR string to {typeof(T).Name} fail:" + data;

                switch (logger)
                {
                    case not null:
                        {
                            logger.ErrorException(
                                className: nameof(ObfuscationHelpers), methodName: nameof(DecodeDataFromSR), message: message, e: exception);

                            break;
                        }
                    default:
                        {
                            CommonBaseConstant.ConfigLoggerExceptionByConsole(
                                className: nameof(ObfuscationHelpers), methodName: nameof(DecodeDataFromSR), description: message, exception: exception);

                            break;
                        }
                }

                result = default;

                return false;
            }
        }

        /// <summary>
        ///  Xử lý chuyển dữ liệu thô thành dữ liệu đã được mã hóa.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        ///
        public static bool EncodeDataFromSR<T>(
            this T data, out string result, string key, ILogger logger = null)
        {
            static string XorDecodeFromBase64(T encodedText, string key)
            {
                // 1) object -> JSON string
                string json = encodedText?.ToJSon() ?? string.Empty;

                // 2) JSON string -> bytes
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                // 3) XOR bytes với key
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] xored = new byte[jsonBytes.Length];

                for (int i = 0; i < jsonBytes.Length; i++)
                {
                    xored[i] = (byte)(jsonBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                // 4) bytes -> base64
                return Convert.ToBase64String(xored);
            }

            if (string.IsNullOrWhiteSpace(key) || data is null)
            {
                result = default;

                return false;
            }

            try
            {
                string value = XorDecodeFromBase64(encodedText: data, key: key);

                if (string.IsNullOrWhiteSpace(value))
                {
                    result = default;

                    return false;
                }

                result = value;

                return true;
            }
            catch (Exception exception)
            {
                string message =
                    $"EncodeDataFromSR string to {typeof(T).Name} fail: data" + Newtonsoft.Json.JsonConvert.SerializeObject(data);

                switch (logger)
                {
                    case not null:
                        {
                            logger.ErrorException(
                                className: nameof(ObfuscationHelpers), methodName: nameof(EncodeDataFromSR), message: message, e: exception);

                            break;
                        }
                    default:
                        {
                            CommonBaseConstant.ConfigLoggerExceptionByConsole(
                                className: nameof(ObfuscationHelpers), methodName: nameof(EncodeDataFromSR), description: message, exception: exception);

                            break;
                        }
                }

                result = default;

                return false;
            }
        }
    }
}