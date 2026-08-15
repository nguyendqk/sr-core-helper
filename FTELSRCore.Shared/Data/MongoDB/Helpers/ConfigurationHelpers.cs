// Ignore Spelling: Mongo

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Globalization;

namespace FTELSRCore.Data.MongoDB.Helpers
{
    public static class ConfigurationHelpers
    {
        /// <summary>
        /// Cache MongoClient theo connection string cho health-check, tránh tạo mới
        /// (và mở pool kết nối mới) ở mỗi lần gọi IsCheckConnection.
        /// </summary>
        private static readonly ConcurrentDictionary<string, MongoClient> _healthCheckClients = new();

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="configDatabase"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        ///
        public static IMongoCollection<TModel> SetCollection<TModel>(IMongoDatabase configDatabase, string table) where TModel : class
        {
            return configDatabase.GetCollection<TModel>(table);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        /// <exception cref="CustomException"></exception>
        ///
        public static MongoClientSettings GetSettingConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new CustomException("Không tìm thấy cấu hình kết nối.");

            MongoClientSettings settings =
               MongoClientSettings.FromConnectionString(connectionString);

            // Thời gian chờ socket
            settings.SocketTimeout = TimeSpan.FromSeconds(30);

            // Thời gian chờ kết nối
            settings.ConnectTimeout = TimeSpan.FromSeconds(30);

            // Thời gian chờ lựa chọn server
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);

            // Số kết nối tối đa trong pool
            settings.MaxConnectionPoolSize = 2000;

            // Số kết nối tối thiểu trong pool
            settings.MinConnectionPoolSize = 100;

            return settings;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="connectionDatabase"></param>
        /// <param name="databaseName"></param>
        /// <param name="timeWait"></param>
        /// <returns></returns>
        ///
        public static bool IsCheckConnection(string connectionDatabase, string databaseName, int timeWait = 1000)
        {
            if (string.IsNullOrWhiteSpace(connectionDatabase) || string.IsNullOrWhiteSpace(databaseName))
            {
                return false;
            }

            MongoClient client = _healthCheckClients.GetOrAdd(connectionDatabase, connectionString => new MongoClient(connectionString));

            IMongoDatabase database = client.GetDatabase(databaseName);

            bool pingResult = false;

            try
            {
                // CancellationToken gắn với timeWait để driver thực sự huỷ lệnh ping khi hết
                // hạn, thay vì để nó tiếp tục chạy nền trên client đã bị "bỏ rơi".
                using CancellationTokenSource cancellationTokenSource = new(timeWait);

                database
                    .RunCommandAsync((Command<BsonDocument>)"{ping:1}", cancellationToken: cancellationTokenSource.Token)
                    .GetAwaiter()
                    .GetResult();

                pingResult = true;
            }
            catch (OperationCanceledException)
            {
                pingResult = false;
            }
            catch (Exception exception)
            {
                CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(ConfigurationHelpers), nameof(IsCheckConnection), exception: exception, description: $"Database: {database?.DatabaseNamespace}");
            }

            return pingResult;
        }

        public class VietnamDateTimeSerializer : SerializerBase<DateTime?>
        {
            private static readonly TimeZoneInfo VnTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            private readonly BsonType _representation;

            public VietnamDateTimeSerializer() : this(BsonType.DateTime)
            {
            }

            public VietnamDateTimeSerializer(BsonType representation) => _representation = representation;

            public BsonType Representation => _representation;

            public override DateTime? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var bsonType = context.Reader.GetCurrentBsonType();

                if (bsonType == BsonType.Null)
                {
                    context.Reader.ReadNull();
                    return null;
                }

                DateTime utcDateTime;
                switch (bsonType)
                {
                    case BsonType.DateTime:
                        {
                            utcDateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(context.Reader.ReadDateTime());
                            break;
                        }
                    case BsonType.String:
                        {
                            utcDateTime = DateTime.Parse(context.Reader.ReadString(), CultureInfo.InvariantCulture).ToUniversalTime();
                            break;
                        }
                    case BsonType.Int64:
                        {
                            utcDateTime = DateTime.FromBinary(context.Reader.ReadInt64()).ToUniversalTime();
                            break;
                        }
                    case BsonType.Document:
                        {
                            utcDateTime = ReadDocumentAsDateTime(context.Reader);

                            break;
                        }

                    default:
                        throw new NotSupportedException($"Cannot deserialize BsonType {bsonType} to DateTime?");
                }

                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VnTimeZone);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime? value)
            {
                if (!value.HasValue)
                {
                    context.Writer.WriteNull();
                    return;
                }

                DateTime utcDateTime;

                // Nếu là UTC rồi thì giữ nguyên
                if (value.Value.Kind == DateTimeKind.Utc)
                {
                    utcDateTime = value.Value;
                }
                // Nếu là Local hoặc Unspecified, coi như đang ở múi giờ Việt Nam và convert sang UTC
                else
                {    // Phải chuyển sang Unspecified trước khi dùng custom timezone
                    var unspecifiedDateTime = DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);

                    utcDateTime = TimeZoneInfo.ConvertTimeToUtc(unspecifiedDateTime, VnTimeZone);
                }

                switch (_representation)
                {
                    case BsonType.DateTime:
                        context.Writer.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(utcDateTime));
                        break;

                    case BsonType.String:
                        context.Writer.WriteString(utcDateTime.ToString("o"));
                        break;

                    case BsonType.Int64:
                        context.Writer.WriteInt64(utcDateTime.ToBinary());
                        break;

                    default:
                        throw new NotSupportedException($"Representation {_representation} is not supported for DateTime?");
                }
            }

            /// <summary>
            /// Xử lý extended JSON:  { "$date": &lt;ms&gt; }  hoặc  { "$date": { "$numberLong": "..." } }
            /// </summary>
            private static DateTime ReadDocumentAsDateTime(IBsonReader reader)
            {
                reader.ReadStartDocument();

                long milliseconds = 0;

                while (reader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    string name = reader.ReadName();

                    if (name == "$date")
                    {
                        milliseconds = reader.CurrentBsonType switch
                        {
                            BsonType.Int64 => reader.ReadInt64(),
                            BsonType.DateTime => reader.ReadDateTime(),
                            BsonType.String => new DateTimeOffset(
                                DateTime.Parse(reader.ReadString(), CultureInfo.InvariantCulture,
                                               System.Globalization.DateTimeStyles.RoundtripKind))
                                .ToUnixTimeMilliseconds(),

                            // { "$date": { "$numberLong": "..." } }
                            BsonType.Document => ReadNumberLong(reader),

                            _ => SkipAndReturn(reader, 0L)
                        };
                    }
                    else
                    {
                        reader.SkipValue();
                    }
                }

                reader.ReadEndDocument();

                return BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(milliseconds);
            }

            private static long ReadNumberLong(IBsonReader reader)
            {
                reader.ReadStartDocument();
                long result = 0;

                while (reader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    string name = reader.ReadName();
                    if (name == "$numberLong")
                        long.TryParse(reader.ReadString(), out result);
                    else
                        reader.SkipValue();
                }

                reader.ReadEndDocument();
                return result;
            }

            private static T SkipAndReturn<T>(IBsonReader reader, T defaultValue)
            {
                reader.SkipValue();
                return defaultValue;
            }
        }
    }
}