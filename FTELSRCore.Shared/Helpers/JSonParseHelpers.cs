using MongoDB.Bson;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonTokenType = System.Text.Json.JsonTokenType;

namespace FTELSRCore.Helpers
{
    public static partial class JSonParseHelpers
    {
        /// <summary>
        /// Chuyển đổi đối tượng sang chuỗi JSON.
        /// Hỗ trợ đặc biệt với kiểu <see cref="BsonValue"/> của MongoDB bằng cách sử dụng phương thức ToJson().
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng cần chuyển đổi.</typeparam>
        /// <param name="obj">Đối tượng cần chuyển sang chuỗi JSON.</param>
        /// <returns>Chuỗi JSON biểu diễn đối tượng, hoặc chuỗi rỗng nếu đối tượng là null.</returns>
        ///
        public static string ToJSon<T>(this T obj, ILogger logger = null)
        {
            if (obj is null) return string.Empty;

            try
            {
                return obj switch
                {
                    BsonValue bsonValue => bsonValue.ToJson(),

                    _ => JsonSerializer.Serialize(
                        value: obj, options: _defaultJsonOptions)
                };
            }
            catch (NotSupportedException)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            }
            catch (Exception exception)
            {
                string message =
                    $"ToJSon to {typeof(T).Name} fail: " + Newtonsoft.Json.JsonConvert.SerializeObject(obj);

                switch (logger)
                {
                    case not null:
                        {
                            logger.ErrorException(
                                className: nameof(JSonParseHelpers), methodName: nameof(ToJSon), message: message, e: exception);

                            break;
                        }
                    default:
                        {
                            CommonBaseConstant.ConfigLoggerExceptionByConsole(
                                className: nameof(JSonParseHelpers), methodName: nameof(ToJSon), description: message, exception: exception);

                            break;
                        }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Thử chuyển đổi một đối tượng <see cref="BsonDocument"/> thành kiểu dữ liệu cụ thể.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu đầu ra sau khi giải tuần tự.</typeparam>
        /// <param name="obj">Đối tượng <see cref="BsonDocument"/> cần chuyển đổi.</param>
        /// <param name="result">Kết quả sau khi chuyển đổi. Nếu thất bại, giá trị mặc định sẽ được gán.</param>
        /// <param name="logger"></param>
        /// <returns>
        /// Trả về <c>true</c> nếu giải tuần tự thành công, ngược lại trả về <c>false</c>.
        /// </returns>
        ///
        public static bool JSonTryParse<T>(this BsonDocument obj, out T result, ILogger logger = null)
        {
            result = default;

            // Kiểm tra null
            if (obj is null) return false;

            // Kiểm tra BsonDocument rỗng (không có elements)
            if (obj.ElementCount == 0) return false;

            // Kiểm tra nếu chỉ có một element và là null
            if (obj.ElementCount == 1)
            {
                BsonElement firstElement = obj.Elements.First();

                if (firstElement.Value.IsBsonNull) return false;
            }

            try
            {
                string json = obj.ToJSon();

                // Double check với JSON string
                if (string.IsNullOrWhiteSpace(json)
                    || json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase)
                    || json.Trim() == "{}" || json.Trim() == "[]")
                {
                    return false;
                }

                json.JSonTryParse(out result);

                return true;
            }
            catch (Exception exception)
            {
                string message =
                    $"JSonTryParse BsonDocument to {typeof(T).Name} fail: ObjDataBson" + Newtonsoft.Json.JsonConvert.SerializeObject(obj);

                switch (logger)
                {
                    case not null:
                        {
                            logger.ErrorException(
                                className: nameof(JSonParseHelpers), methodName: nameof(JSonTryParse), message: message, e: exception);

                            break;
                        }
                    default:
                        {
                            CommonBaseConstant.ConfigLoggerExceptionByConsole(
                                className: nameof(JSonParseHelpers), methodName: nameof(JSonTryParse), description: message, exception: exception);

                            break;
                        }
                }

                result = default;

                return false;
            }
        }

        /// <summary>
        /// Thử chuyển đổi một chuỗi JSON thành kiểu dữ liệu cụ thể.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu đầu ra sau khi giải tuần tự.</typeparam>
        /// <param name="obj">Chuỗi JSON cần chuyển đổi.</param>
        /// <param name="result">Kết quả sau khi chuyển đổi. Nếu thất bại, giá trị mặc định sẽ được gán.</param>
        /// <param name="logger"></param>
        /// <returns>
        /// Trả về <c>true</c> nếu giải tuần tự thành công, ngược lại trả về <c>false</c>.
        /// </returns>
        ///
        public static bool JSonTryParse<T>(this string obj, out T result, ILogger logger = null)
        {
            // Double check với JSON string
            if (string.IsNullOrWhiteSpace(obj)
                || obj.Trim().Equals("null", StringComparison.OrdinalIgnoreCase)
                || obj.Trim() == "{}" || obj.Trim() == "[]")
            {
                result = default;
                return false;
            }

            try
            {
                result =
                    JsonSerializer.Deserialize<T>(
                        json: obj, options: _jsonSerializerOptions);

                return true;
            }
            catch (Exception exception)
            {
                string message =
                    $"JSonTryParse string to {typeof(T).Name} fail: ObjDataString" + Newtonsoft.Json.JsonConvert.SerializeObject(obj);

                switch (logger)
                {
                    case not null:
                        {
                            logger.ErrorException(
                                className: nameof(JSonParseHelpers), methodName: nameof(JSonTryParse), message: message, e: exception);

                            break;
                        }
                    default:
                        {
                            CommonBaseConstant.ConfigLoggerExceptionByConsole(
                                className: nameof(JSonParseHelpers), methodName: nameof(JSonTryParse), description: message, exception: exception);

                            break;
                        }
                }

                result = default;

                return false;
            }
        }
    }

    public static partial class JSonParseHelpers
    {
        /// <summary>
        /// Cấu hình mặc định cho quá trình tuần tự hóa JSON.
        /// - Không phân biệt hoa thường tên thuộc tính.
        /// - Bỏ qua thuộc tính có giá trị null.
        /// - Bỏ qua vòng lặp tham chiếu trong biểu đồ đối tượng.
        /// </summary>
        ///
        internal static readonly JsonSerializerOptions _defaultJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        internal static readonly JsonSerializerOptions _jsonSerializerOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = {
                    new BooleanConverter(),
                    new IntConverter(), new IntNullableConverter(),
                    new LongConverter(), new LongNullableConverter(),
                    new DoubleConverter(), new DoubleNullableConverter(),
                    new DecimalConverter(), new DecimalNullableConverter(),
                    new DateTimeConverter(), new DateTimeNullAbleConverter()
                }
            };

        internal static readonly string[] _dateTimeFormats =
        [
            // Date only
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "dd-MM-yyyy",
            "yyyyMMdd",

            // DateTime without timezone
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-ddTHH:mm",

            // DateTime with milliseconds
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss.ff",
            "yyyy-MM-ddTHH:mm:ss.ff",
            "yyyy-MM-dd HH:mm:ss.f",
            "yyyy-MM-ddTHH:mm:ss.f",

            // ISO 8601 with timezone
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fZ",
            "yyyy-MM-ddTHH:mm:ss.ffZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ss.ffffZ",
            "yyyy-MM-ddTHH:mm:ss.fffffZ",
            "yyyy-MM-ddTHH:mm:ss.ffffffZ",
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",

            // With timezone offset
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ss.fffzzz",
            "yyyy-MM-dd HH:mm:sszzz",
            "yyyy-MM-dd HH:mm:ss.fffzzz",

            // RFC 1123
            "ddd, dd MMM yyyy HH:mm:ss 'GMT'",

            // Sortable
            "yyyy-MM-ddTHH:mm:ss",
            "s"
        ];

        #region -------- CONFIG JSON --------

        private sealed class IntConverter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                int numberInt;
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        {
                            if (reader.TryGetInt32(out numberInt))
                            {
                                return numberInt;
                            }

                            if (reader.TryGetDouble(out var numberDouble))
                            {
                                return (int)numberDouble;
                            }

                            throw new JsonException($"Parse int fail with data type {nameof(JsonTokenType.Number)}.");
                        }
                    case JsonTokenType.String:
                        {
                            return int.TryParse(reader.GetString(), out numberInt)
                               ? numberInt
                               : throw new JsonException($"Parse int fail with data type {nameof(JsonTokenType.String)}.");
                        }
                    case JsonTokenType.Null:
                        {
                            return 0;
                        }

                    default:
                        {
                            throw new JsonException("Json int default");
                        }
                }
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }

        private sealed class IntNullableConverter : JsonConverter<int?>
        {
            public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                int numberInt;
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        {
                            if (reader.TryGetInt32(out numberInt))
                            {
                                return numberInt;
                            }

                            if (reader.TryGetDouble(out var numberDouble))
                            {
                                return (int)numberDouble;
                            }

                            throw new JsonException($"Parse int? fail with data type {nameof(JsonTokenType.Number)}.");
                        }
                    case JsonTokenType.String:
                        {
                            return int.TryParse(reader.GetString(), out numberInt)
                               ? numberInt
                               : throw new JsonException($"Parse int? fail with data type {nameof(JsonTokenType.Number)}.");
                        }
                    case JsonTokenType.Null:
                        {
                            return 0;
                        }
                    default:
                        {
                            throw new JsonException("Json int default");
                        }
                }
            }

            public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                {
                    writer.WriteNumberValue(value.GetValueOrDefault());
                    return;
                }

                writer.WriteNullValue();
            }
        }

        private sealed class LongConverter : JsonConverter<long>
        {
            public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                long numberlong;

                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        {
                            if (reader.TryGetInt64(out numberlong))
                            {
                                return numberlong;
                            }

                            if (reader.TryGetDouble(out var numberDouble))
                            {
                                return (int)numberDouble;
                            }

                            throw new JsonException($"Parse long fail with data type {nameof(JsonTokenType.Number)}.");
                        }
                    case JsonTokenType.String:
                        {
                            return long.TryParse(reader.GetString(), out numberlong)
                                ? numberlong
                                : throw new JsonException($"Parse long fail with data type {nameof(JsonTokenType.String)}.");
                        }
                    case JsonTokenType.Null:
                        {
                            return 0;
                        }
                    default:
                        {
                            throw new JsonException("Json int default");
                        }
                }
            }

            public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }

        private sealed class LongNullableConverter : JsonConverter<long?>
        {
            public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                long numberlong;

                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        {
                            if (reader.TryGetInt64(out numberlong))
                            {
                                return numberlong;
                            }

                            if (reader.TryGetDouble(out var numberDouble))
                            {
                                return (int)numberDouble;
                            }

                            throw new JsonException($"Parse long? fail with data type {nameof(JsonTokenType.Number)}.");
                        }
                    case JsonTokenType.String:
                        {
                            return long.TryParse(reader.GetString(), out numberlong)
                                ? numberlong
                                : throw new JsonException($"Parse long? fail with data type {nameof(JsonTokenType.String)}.");
                        }

                    case JsonTokenType.Null:
                        {
                            return 0;
                        }
                    default:
                        {
                            throw new JsonException("Json int default");
                        }
                }
            }

            public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                {
                    writer.WriteNumberValue(value.GetValueOrDefault());
                    return;
                }

                writer.WriteNullValue();
            }
        }

        private sealed class DoubleConverter : JsonConverter<double>
        {
            public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                double numberDouble;
                return reader.TokenType switch
                {
                    JsonTokenType.Number => reader.TryGetDouble(out numberDouble)
                                                ? numberDouble
                                                : throw new JsonException($"Parse double fail with data type {nameof(JsonTokenType.Number)}."),

                    JsonTokenType.String => double.TryParse(reader.GetString(), out numberDouble)
                                                ? numberDouble
                                                : throw new JsonException($"Parse double fail with data type {nameof(JsonTokenType.String)}."),

                    JsonTokenType.Null => 0,

                    _ => throw new JsonException("Json double default"),
                };
            }

            public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }

        public class DateTimeNullAbleConverter : JsonConverter<DateTime?>
        {
            private const string DefaultFormat = "yyyy-MM-ddTHH:mm:ss";

            public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        {
                            return ConvertDateTimeByString(reader.GetString());
                        }
                    case JsonTokenType.StartObject:
                        {
                            reader.Read(); // Đọc property name "$date"

                            if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "$date")
                            {
                                reader.Read(); // Đọc value

                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    string value = reader.GetString();

                                    reader.Read(); // Đọc EndObject

                                    return ConvertDateTimeByString(value);
                                }
                            }

                            break;
                        }
                }

                return null;
            }

            public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                    writer.WriteStringValue(value.Value.ToString(DefaultFormat));
                else
                    writer.WriteNullValue();
            }

            private static DateTime? ConvertDateTimeByString(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                try
                {
                    if (value.Contains('Z'))
                    {
                        value = value[..^1];
                    }

                    if (value.Contains('.'))
                    {
                        value = value.Split('.')[0];
                    }

                    // Thử parse với nhiều styles khác nhau
                    DateTimeStyles[] stylesToTry =
                    [
                        DateTimeStyles.None,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        DateTimeStyles.RoundtripKind
                    ];

                    foreach (var style in stylesToTry)
                    {
                        if (DateTime.TryParseExact(
                            value, _dateTimeFormats, CultureInfo.InvariantCulture, style, out DateTime result))
                        {
                            return result;
                        }
                    }

                    return null;
                }
                catch (FormatException)
                {
                    throw new FormatException($"Invalid date format: {value}");
                }
            }
        }

        public class DateTimeConverter : JsonConverter<DateTime>
        {
            private const string DefaultFormat = "yyyy-MM-ddTHH:mm:ss";

            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        {
                            return ConvertDateTimeByString(reader.GetString());
                        }
                    case JsonTokenType.StartObject:
                        {
                            reader.Read(); // Đọc property name "$date"

                            if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "$date")
                            {
                                reader.Read(); // Đọc value

                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    string value = reader.GetString();

                                    reader.Read(); // Đọc EndObject

                                    return ConvertDateTimeByString(value);
                                }
                            }

                            break;
                        }
                }

                return DateTime.MinValue;
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString(DefaultFormat));
            }

            private static DateTime ConvertDateTimeByString(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return DateTime.MinValue;
                }

                try
                {
                    if (value.Contains('Z'))
                    {
                        value = value[..^1];
                    }

                    if (value.Contains('.'))
                    {
                        value = value.Split('.')[0];
                    }

                    DateTimeStyles[] stylesToTry =
                    [
                        DateTimeStyles.None,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        DateTimeStyles.RoundtripKind
                    ];

                    foreach (var style in stylesToTry)
                    {
                        if (DateTime.TryParseExact(
                            value, _dateTimeFormats, CultureInfo.InvariantCulture, style, out DateTime result))
                        {
                            return result;
                        }
                    }

                    return DateTime.MinValue;
                }
                catch (FormatException)
                {
                    throw new FormatException($"Invalid date format: {value}");
                }
            }
        }

        private sealed class DoubleNullableConverter : JsonConverter<double?>
        {
            public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.Number => reader.TryGetDouble(out double numberDouble)
                                                ? numberDouble
                                                : throw new JsonException($"Parse double? fail with data type {nameof(JsonTokenType.Number)}."),

                    JsonTokenType.String => double.TryParse(reader.GetString(), out double numberDouble)
                                                ? numberDouble
                                                : throw new JsonException($"Parse double? fail with data type {nameof(JsonTokenType.String)}."),

                    JsonTokenType.Null => 0,

                    _ => throw new JsonException("Json double default"),
                };
            }

            public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                {
                    writer.WriteNumberValue(value.GetValueOrDefault());
                    return;
                }

                writer.WriteNullValue();
            }
        }

        private sealed class DecimalConverter : JsonConverter<decimal>
        {
            public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                decimal numberDecimal;

                return reader.TokenType switch
                {
                    JsonTokenType.Number => reader.TryGetDecimal(out numberDecimal)
                                                ? numberDecimal
                                                : throw new JsonException($"Parse decimal fail with data type {nameof(JsonTokenType.Number)}."),

                    JsonTokenType.String => decimal.TryParse(reader.GetString(), out numberDecimal)
                                                ? numberDecimal
                                                : throw new JsonException($"Parse decimal fail with data type {nameof(JsonTokenType.String)}."),

                    JsonTokenType.Null => 0,

                    _ => throw new JsonException("Json decimal default"),
                };
            }

            public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }

        private sealed class DecimalNullableConverter : JsonConverter<decimal?>
        {
            public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.Number =>
                        reader.TryGetDecimal(out decimal numberDecimal)
                        ? numberDecimal
                        : throw new JsonException($"Parse decimal? fail with data type {nameof(JsonTokenType.Number)}."),

                    JsonTokenType.String =>
                        decimal.TryParse(reader.GetString(), out decimal numberDecimal)
                        ? numberDecimal
                        : throw new JsonException($"Parse decimal? fail with data type {nameof(JsonTokenType.String)}."),

                    JsonTokenType.Null => 0,

                    _ => throw new JsonException("Json decimal? default"),
                };
            }

            public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                {
                    writer.WriteNumberValue(value.GetValueOrDefault());
                    return;
                }

                writer.WriteNullValue();
            }
        }

        private sealed class BooleanConverter : JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.True => true,

                    JsonTokenType.False => false,

                    JsonTokenType.Number =>
                        reader.TryGetInt32(out var _)
                        ? true
                        : throw new JsonException($"Parse bool fail with data type {nameof(JsonTokenType.Number)}."),

                    JsonTokenType.String => reader.GetString() switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => throw new JsonException()
                    },

                    _ => throw new JsonException("Json bool default"),
                };
            }

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            {
                writer.WriteBooleanValue(value);
            }
        }

        #endregion -------- CONFIG JSON --------
    }
}