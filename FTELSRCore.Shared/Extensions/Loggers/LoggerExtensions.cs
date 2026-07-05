using FTELSRCore.Extensions.Loggers.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static FTELSRCore.Infrastructure.Extensions.Helpers.SerilogProviderExtensions.Formatters.SRKafkaLogFormatter;

namespace FTELSRCore.Extensions.Loggers
{
    internal enum LatencyRating
    {
        Fast = 1,
        Normal = 2,
        Slow = 3,
        TimeoutRisk = 4
    }

    internal static class EventIds
    {
        public const int Request = 101;

        public const int Response = 102;

        public const int Warning = 103;

        public const int Debug = 104;

        public const int FailLogic = 107;

        #region ::::::::::::::: EventIds => ERROR :::::::::::::::

        public const int Error = 106;

        public const int ErrorException = 106001;

        public const int ErrorResult = 106002;

        public const int HttpErrorResult = 106003;

        public const int MediaRErrorResult = 106004;

        public const int KafkaErrorResult = 106005;

        public const int ConnectionError = 106006;

        #endregion ::::::::::::::: EventIds => ERROR :::::::::::::::

        #region ::::::::::::::: EventIds => INFO :::::::::::::::

        public const int Info = 105;

        public const int HttpResult = 105001;

        public const int Connection = 105002;

        public const int MediaRResult = 105003;

        #endregion ::::::::::::::: EventIds => INFO :::::::::::::::
    }

    public static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, string, object, Exception> _debug;

        private static readonly Action<ILogger, string, string, object, Exception> _warning;

        #region +++++++++++++++++ REQUEST +++++++++++++++++

        private static readonly Action<ILogger, string, string, Exception> _requestWithoutParams;

        private static readonly Action<ILogger, string, string, string, object, Exception> _request;

        #endregion +++++++++++++++++ REQUEST +++++++++++++++++

        #region +++++++++++++++++ RESPONSE +++++++++++++++++

        private static readonly Action<ILogger, string, string, object, Exception> _response;

        private static readonly Action<ILogger, string, string, Exception> _responseWithoutParams;

        private static readonly Action<ILogger, string, string, long, string, object, Exception> _responseWithTracing;

        #endregion +++++++++++++++++ RESPONSE +++++++++++++++++

        #region +++++++++++++++++ INFOMATION +++++++++++++++++

        private static readonly Action<ILogger, string, string, object, Exception> _info;

        private static readonly Action<ILogger, string, string, string, object, Exception> _failLogic;

        #endregion +++++++++++++++++ INFOMATION +++++++++++++++++

        #region +++++++++++++++++ HTTP +++++++++++++++++

        private static readonly Action<ILogger, string, string, object, Exception> _httpResult;

        #endregion +++++++++++++++++ HTTP +++++++++++++++++

        #region +++++++++++++++++ MEDIAR +++++++++++++++++

        private static readonly Action<ILogger, string, string, long, string, object, Exception> _mediaRResultWithTracing;

        #endregion +++++++++++++++++ MEDIAR +++++++++++++++++

        #region +++++++++++++++++ ERORR +++++++++++++++++

        private static readonly Action<ILogger, string, string, string, object, Exception> _errorResult;

        private static readonly Action<ILogger, string, string, string, object, Exception> _error;

        #endregion +++++++++++++++++ ERORR +++++++++++++++++

        #region +++++++++++++++++ KAFKA +++++++++++++++++

        private static readonly Action<ILogger, string, string, string, object, Exception> _kafka;

        private static readonly Action<ILogger, string, string, object, Exception> _kafkaErrorWithoutTopic;

        private static readonly Action<ILogger, string, string, string, object, Exception> _kafkaErrorResult;

        #endregion +++++++++++++++++ KAFKA +++++++++++++++++

        private static readonly Action<ILogger, string, string, object, Exception> _connection;

        static LoggerExtensions()
        {
            _debug = LoggerMessage.Define<string, string, object>(
                LogLevel.Debug,
                new EventId(EventIds.Debug, nameof(Debug)),
                "{ClassName} - {MethodName} -- debug: {Message}");

            _warning = LoggerMessage.Define<string, string, object>(
                LogLevel.Warning,
                new EventId(EventIds.Warning, nameof(Warning)),
                "{ClassName} - {MethodName} -- warning: {Message}");

            #region +++++++++++++++++ REQUEST +++++++++++++++++

            _request = LoggerMessage.Define<string, string, string, object>(
                LogLevel.Information,
                new EventId(EventIds.Request, nameof(Request)),
                "{ClassName} - {MethodName} -- request {RequestName}: {Parameters}");

            _requestWithoutParams = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(EventIds.Request, "RequestWithoutParams"),
                "{ClassName} - {MethodName} -- request");

            #endregion +++++++++++++++++ REQUEST +++++++++++++++++

            #region +++++++++++++++++ RESPONSE +++++++++++++++++

            _response = LoggerMessage.Define<string, string, object>(
                LogLevel.Information,
                new EventId(EventIds.Response, nameof(Response)),
                "{ClassName} - {MethodName} -- response: {Message}");

            _responseWithoutParams = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(EventIds.Response, "ResponseWithoutParams"),
                "{ClassName} - {MethodName} -- response");

            _responseWithTracing = LoggerMessage.Define<string, string, long, string, object>(
                LogLevel.Information,
                new EventId(EventIds.Response, "ResponseWithTracing"),
                "{ClassName} - {MethodName} - [Latency:{ResponseTimeMs}ms -> LatencyRating:{LatencyRating}] -- response: {Message}");

            #endregion +++++++++++++++++ RESPONSE +++++++++++++++++

            #region +++++++++++++++++ INFOMATION +++++++++++++++++

            _info = LoggerMessage.Define<string, string, object>(
                LogLevel.Information,
                new EventId(EventIds.Info, nameof(Info)),
                "{ClassName} - {MethodName} -- info: {Message}");

            _failLogic = LoggerMessage.Define<string, string, string, object>(
                LogLevel.Information,
                new EventId(EventIds.FailLogic, nameof(FailLogic)),
                "{ClassName} - {MethodName} - [ErrorCategory:{ErrorCategory}] -- fail logic: {Message}");

            #endregion +++++++++++++++++ INFOMATION +++++++++++++++++

            #region +++++++++++++++++ HTTP +++++++++++++++++

            _httpResult = LoggerMessage.Define<string, string, object>(
                LogLevel.Information,
                new EventId(EventIds.HttpResult, nameof(HttpResult)),
                "------------HTTP------------ {ClassName} - {MethodName} -- response HTTP result: {Message}");

            #endregion +++++++++++++++++ HTTP +++++++++++++++++

            #region +++++++++++++++++ MEDIAR +++++++++++++++++

            _mediaRResultWithTracing = LoggerMessage.Define<string, string, long, string, object>(
                LogLevel.Information,
                new EventId(EventIds.MediaRResult, nameof(MediaRResult)),
                "------------MEDIAR------------ {ClassName} - {MethodName} - [Latency:{ResponseTimeMs} ms -> LatencyRating:{LatencyRating}] -- response MEDIAR Key: {Message}");

            #endregion +++++++++++++++++ MEDIAR +++++++++++++++++

            #region +++++++++++++++++ ERORR +++++++++++++++++

            _errorResult = LoggerMessage.Define<string, string, string, object>(
                LogLevel.Error,
                new EventId(EventIds.ErrorResult, nameof(ErrorResult)),
                "{ClassName} - {MethodName} - [ErrorCategory:{ErrorCategory}] -- response error result: {Message}");

            _error = LoggerMessage.Define<string, string, string, object>(
                LogLevel.Error,
                new EventId(EventIds.Error, nameof(Error)),
                "{ClassName} - {MethodName} - [ErrorCategory:{ErrorCategory}] -- error: {Message}");

            #endregion +++++++++++++++++ ERORR +++++++++++++++++

            #region +++++++++++++++++ KAFKA +++++++++++++++++

            _kafka = LoggerMessage.Define<string, string, string, object>(
               LogLevel.Information,
               new EventId(EventIds.Info, "Kafka"),
               "------------KAFKA------------ {ClassName} - {MethodName} - [Topic:{Topic}] -- info: {Message}");

            _kafkaErrorResult = LoggerMessage.Define<string, string, string, object>(
               LogLevel.Error,
               new EventId(EventIds.KafkaErrorResult, nameof(KafkaErrorResult)),
               "------------KAFKA------------ {ClassName} - {MethodName} - [Topic:{Topic}] -- response KAFKA error result: {Message}");

            _kafkaErrorWithoutTopic = LoggerMessage.Define<string, string, object>(
               LogLevel.Error,
               new EventId(EventIds.KafkaErrorResult, nameof(KafkaErrorResult)),
               "------------KAFKA------------ {ClassName} - {MethodName} -- response KAFKA error result: {Message}");

            #endregion +++++++++++++++++ KAFKA +++++++++++++++++

            _connection = LoggerMessage.Define<string, string, object>(
                LogLevel.Information,
                new EventId(EventIds.Connection, nameof(Connection)),
                "------------CONNECTION------------ {ClassName} - {MethodName} -- message: {Message}.");
        }

        private static string LatencyRatingData(long latency)
        {
            return latency switch
            {
                < 1_000 => nameof(LatencyRating.Fast),
                < 3_000 => nameof(LatencyRating.Normal),
                < 10_000 => nameof(LatencyRating.Slow),
                _ => nameof(LatencyRating.TimeoutRisk)
            };
        }

        public static void Warning(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _warning(logger, className, methodName, message, e);
        }

        public static void Debug(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _debug(logger, className, methodName, message, e);
        }

        #region +++++++++++++++++ REQUEST +++++++++++++++++

        public static void Request(this ILogger logger, string className, string methodName, Exception e = null)
        {
            _requestWithoutParams(logger, className, methodName, e);
        }

        public static void Request(this ILogger logger, string className, string methodName, string requestName, object parameters, Exception e = null)
        {
            _request(logger, className, methodName, requestName, parameters, e);
        }

        #endregion +++++++++++++++++ REQUEST +++++++++++++++++

        #region +++++++++++++++++ REPONSE +++++++++++++++++

        public static void Response(this ILogger logger, string className, string methodName, Exception e = null)
        {
            _responseWithoutParams(logger, className, methodName, e);
        }

        public static void Response(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _response(logger, className, methodName, message, e);
        }

        public static void Response(this ILogger logger, string className, string methodName, long latency, object message, Exception e = null)
        {
            _responseWithTracing(logger, className, methodName, latency, LatencyRatingData(latency: latency), message, e);
        }

        #endregion +++++++++++++++++ REPONSE +++++++++++++++++

        #region +++++++++++++++++ INFORMATION +++++++++++++++++

        public static void Info(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _info(logger, className, methodName, message, e);
        }

        public static void FailLogic(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _failLogic(logger, className, methodName, LoggerErrorCategoriesHelper.BusinessCategory.BIZ_LOGIC, message, e);
        }

        public static void Info<T>(this ILogger logger, string className, string methodName, FilterDefinition<T> parameters, Exception e = null) where T : class
        {
            try
            {
                BsonDocument bsonFilterElements =
                    parameters.Render(
                        new RenderArgs<T>(
                            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
                            BsonSerializer.SerializerRegistry));

                _info(logger, className, methodName, bsonFilterElements?.ToJSon(), e);
            }
            catch (Exception exception)
            {
                ErrorException(logger, className, methodName, message: $"Error while rendering FilterDefinition of {typeof(T)?.Name}", e: exception);
            }
        }

        public static void Info(this ILogger logger, string className, string methodName, List<BsonDocument> parameters, Exception e = null)
        {
            try
            {
                string planJson = parameters.ToJson(new JsonWriterSettings
                {
                    Indent = false,
                    OutputMode = JsonOutputMode.RelaxedExtendedJson
                });

                _info(logger, className, methodName, planJson, e);
            }
            catch (Exception exception)
            {
                ErrorException(logger, className, methodName, message: $"Error while rendering BsonDocument[]", e: exception);
            }
        }

        #endregion +++++++++++++++++ INFORMATION +++++++++++++++++

        #region +++++++++++++++++ HTTP +++++++++++++++++

        public static void HttpResult(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _httpResult(logger, className, methodName, message, e);
        }

        public static void HttpErrorResult(this ILogger logger, string className, string methodName, object message)
        {
            logger.Log(LogLevel.Error,
                new EventId(EventIds.HttpErrorResult, nameof(HttpErrorResult)),
                "------------HTTP------------ {ClassName} - {MethodName} - [ErrorCategory:{ErrorCategory}] -- response HTTP error result: {Message}",
                className, methodName, LoggerErrorCategoriesHelper.BusinessCategory.BIZ_LOGIC, message);
        }

        public static void HttpErrorResult(this ILogger logger, string className, string methodName, object message, Exception e)
        {
            logger.Log(LogLevel.Error,
                new EventId(EventIds.HttpErrorResult, nameof(HttpErrorResult)),
                "------------HTTP------------ {ClassName} - {MethodName} - [ErrorCategory:{ErrorCategory}] -- response HTTP error result: {Message}\n-- {ErrorMessage}\n-- {StackTrace}",
                className, methodName, LoggerErrorCategoriesHelper.BusinessCategory.BIZ_LOGIC, message, e?.Message?.Trim(), e?.StackTrace?.Trim());
        }

        public static void HttpResultWithTracing(
            this ILogger logger,
            string className, string methodName,
            string statusCode, string httpMethod,
            long responseTimeMs,
            string uri,
            object message,
            string systemOwner = "",
            DirectionType direction = DirectionType.Inbound)
        {
            logger.Log(
                LogLevel.Information,
                new EventId(EventIds.HttpResult, nameof(HttpResultWithTracing)),
                "------------HTTP------------ {ClassName} - {MethodName} " +
                "- [HttpMethod:{HttpMethod} :: Endpoint:{Endpoint} :: SystemOwner:{SystemOwner}({Direction}) :: HttpStatusCode:{HttpStatusCode} :: Latency:{ResponseTimeMs}ms -> LatencyRating:{LatencyRating}] " +
                "- [ErrorCategory: {ErrorCategory}] -- response HTTP result: {Message}",
                className, methodName, httpMethod, uri, systemOwner, direction, statusCode,
                responseTimeMs, LatencyRatingData(latency: responseTimeMs),
                LoggerErrorCategoriesHelper.ApiCategory.ResolveCategory(statusCode: (int.TryParse(statusCode, out var statusCodeToInt) ? statusCodeToInt : 0)), message);
        }

        #endregion +++++++++++++++++ HTTP +++++++++++++++++

        #region +++++++++++++++++ MEDIAR +++++++++++++++++

        public static void MediaRResult(this ILogger logger, string className, string methodName, long latency, string message, Exception e = null)
        {
            _mediaRResultWithTracing(logger, className, methodName, latency, LatencyRatingData(latency: latency), message, e);
        }

        #endregion +++++++++++++++++ MEDIAR +++++++++++++++++

        #region +++++++++++++++++ KAFKA +++++++++++++++++

        public static void KafkaErrorResult(this ILogger logger, string className, string methodName, string topic, object message, Exception e = null)
        {
            _kafkaErrorResult(logger, className, methodName, topic, message, e);
        }

        public static void KafkaErrorWithoutTopic(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _kafkaErrorWithoutTopic(logger, className, methodName, message, e);
        }

        public static void Kafka(this ILogger logger, string className, string methodName, string topic, object message, Exception e = null)
        {
            _kafka(logger, className, methodName, topic, message, e);
        }

        #endregion +++++++++++++++++ KAFKA +++++++++++++++++

        #region +++++++++++++++++ ERORR +++++++++++++++++

        public static void ErrorResult(this ILogger logger, string className, string methodName, object message, string errorCategory = "", Exception e = null)
        {
            _errorResult(logger, className, methodName, (!string.IsNullOrWhiteSpace(errorCategory) ? errorCategory : LoggerErrorCategoriesHelper.BusinessCategory.BIZ_LOGIC), message, e);
        }

        public static void Error(this ILogger logger, string className, string methodName, object message, string errorCategory = "", Exception e = null)
        {
            _error(logger, className, methodName, (!string.IsNullOrWhiteSpace(errorCategory) ? errorCategory : LoggerErrorCategoriesHelper.BusinessCategory.BIZ_LOGIC), message, e);
        }

        public static void ErrorException(this ILogger logger, string className, string methodName, Exception e, string errorCategory = "", object message = null)
        {
            logger.Log(
                LogLevel.Error,
                new EventId(EventIds.ErrorException, nameof(ErrorException)),
                "{ClassName} - {MethodName} - [ErrorCategory:{ErrorCategory}] -- error exception message: {Message}\n-- {ErrorMessage}\n-- {StackTrace}",
                className, methodName, (!string.IsNullOrWhiteSpace(errorCategory) ? errorCategory : LoggerErrorCategoriesHelper.BusinessCategory.BIZ_LOGIC),
                message, e?.Message?.Trim(), e?.StackTrace?.Trim());
        }

        #endregion +++++++++++++++++ ERORR +++++++++++++++++

        #region +++++++++++++++++ CONNECTION +++++++++++++++++

        public static void Connection(
            this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _connection(logger, className, methodName, message, e);
        }

        public static void ConnectionErrorSQL(
            this ILogger logger, string className, string methodName, Exception e, string message = "")
        {
            ConnectionError(logger, className, methodName, LoggerErrorCategoriesHelper.InfrastructureCategory.DB_SQLSERVER, e: e, message: message);
        }

        public static void ConnectionErrorMongoDB(
            this ILogger logger, string className, string methodName, Exception e, string message = "")
        {
            ConnectionError(logger, className, methodName, LoggerErrorCategoriesHelper.InfrastructureCategory.DB_MONGODB, e: e, message: message);
        }

        public static void ConnectionErrorRedis(this ILogger logger, string className, string methodName, Exception e, string message = "")
        {
            ConnectionError(logger, className, methodName, LoggerErrorCategoriesHelper.InfrastructureCategory.DB_REDIS, e: e, message: message);
        }

        public static void ConnectionErrorKafka(this ILogger logger, string className, string methodName, Exception e, string message = "", string topic = "")
        {
            switch (!string.IsNullOrWhiteSpace(topic))
            {
                case true:
                    {
                        logger.Log(
                            LogLevel.Error,
                            new EventId(EventIds.ConnectionError, nameof(ConnectionError)),
                            "------------CONNECTION------------ {ClassName} - {MethodName} - [Topic:{Topic} - ErrorCategory:{ErrorCategory}] -- error exception message: {Message}\n-- {ErrorMessage}\n-- {StackTrace}",
                            className, methodName, topic,
                            LoggerErrorCategoriesHelper.InfrastructureCategory.MQ_KAFKA, message, e?.Message?.Trim(), e?.StackTrace?.Trim());

                        break;
                    }
                case false:
                    {
                        logger.Log(
                            LogLevel.Error,
                            new EventId(EventIds.ConnectionError, nameof(ConnectionError)),
                            "------------CONNECTION------------ {ClassName} - {MethodName} - [ErrorCategory:{ErrorCategory}] -- error exception message: {Message}\n-- {ErrorMessage}\n-- {StackTrace}",
                            className, methodName,
                            LoggerErrorCategoriesHelper.InfrastructureCategory.MQ_KAFKA, message, e?.Message?.Trim(), e?.StackTrace?.Trim());

                        break;
                    }
            }
        }

        public static void ConnectionErrorElasticSearch(this ILogger logger, string className, string methodName, Exception e, string message = "")
        {
            ConnectionError(logger, className, methodName, LoggerErrorCategoriesHelper.InfrastructureCategory.DB_ELASTICSEARCH, e: e, message: message);
        }

        public static void ConnectionErrorRabbitMQ(this ILogger logger, string className, string methodName, Exception e, string message = "")
        {
            ConnectionError(logger, className, methodName, LoggerErrorCategoriesHelper.InfrastructureCategory.MQ_RABBITMQ, e: e, message: message);
        }

        private static void ConnectionError(this ILogger logger, string className, string methodName, string errorCategory, Exception e, object message)
        {
            logger.Log(
                LogLevel.Error,
                new EventId(EventIds.ConnectionError, nameof(ConnectionError)),
                "------------CONNECTION------------ {ClassName} - {MethodName} - [ErrorCategory:{ErrorCategory}] -- error exception message: {Message}\n-- {ErrorMessage}\n-- {StackTrace}",
                className, methodName, errorCategory,
                message, e?.Message?.Trim(), e?.StackTrace?.Trim());
        }

        #endregion +++++++++++++++++ CONNECTION +++++++++++++++++
    }
}