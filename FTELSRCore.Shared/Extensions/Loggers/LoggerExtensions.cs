using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

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
        private static readonly Action<ILogger, string, string, string, string, Exception> _request;

        private static readonly Action<ILogger, string, string, string, Exception> _failLogic;

        private static readonly Action<ILogger, string, string, Exception> _requestWithoutParams;

        private static readonly Action<ILogger, string, string, string, Exception> _response;
        private static readonly Action<ILogger, string, string, Exception> _responseWithoutParams;
        private static readonly Action<ILogger, string, string, long, string, string, Exception> _responseWithTracing;

        private static readonly Action<ILogger, string, string, string, Exception> _info;
        private static readonly Action<ILogger, string, string, string, Exception> _infoWithFilterDefinition;

        private static readonly Action<ILogger, string, string, string, Exception> _warning;
        private static readonly Action<ILogger, string, string, string, Exception> _debug;

        private static readonly Action<ILogger, string, string, string, Exception> _errorResult;

        private static readonly Action<ILogger, string, string, long, string, string, Exception> _mediaRResultWithTracing;

        private static readonly Action<ILogger, string, string, string, Exception> _kafkaErrorResult;

        private static readonly Action<ILogger, string, string, string, Exception> _httpResult;
        private static readonly Action<ILogger, string, string, string, Exception> _httpErrorResult;
        private static readonly Action<ILogger, string, string, string, long, string, string, Exception> _httpResultWithTracing;

        private static readonly Action<ILogger, string, string, string, Exception> _error;
        private static readonly Action<ILogger, string, string, string, Exception> _errorException;

        private static readonly Action<ILogger, string, string, string, Exception> _connection;
        private static readonly Action<ILogger, string, string, string, Exception> _connectionError;

        static LoggerExtensions()
        {
            _request = LoggerMessage.Define<string, string, string, string>(
                LogLevel.Information,
                new EventId(EventIds.Request, nameof(Request)),
                "{ClassName} - {MethodName} -- request {RequestName}: {Parameters}");

            _requestWithoutParams = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(EventIds.Request, "RequestWithoutParams"),
                "{ClassName} - {MethodName} -- request");

            _response = LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(EventIds.Response, nameof(Response)),
                "{ClassName} - {MethodName} -- response: {Message}");

            _responseWithTracing = LoggerMessage.Define<string, string, long, string, string>(
                LogLevel.Information,
                new EventId(EventIds.Response, nameof(Response)),
                "{ClassName} - {MethodName} - [Latency:{Latency} ms -> LatencyRating:{LatencyRating}] -- response: {Message}");

            _responseWithoutParams = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(EventIds.Response, "ResponseWithoutParams"),
                "{ClassName} - {MethodName} -- response");

            _info = LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(EventIds.Info, nameof(Info)),
                "{ClassName} - {MethodName} -- info: {Message}");

            _infoWithFilterDefinition = LoggerMessage.Define<string, string, string>(
                 LogLevel.Information,
                 new EventId(EventIds.Info, nameof(Info)),
                 "{ClassName} - {MethodName} -- FilterDefinition: {Parameters}");

            _warning = LoggerMessage.Define<string, string, string>(
                LogLevel.Warning,
                new EventId(EventIds.Warning, nameof(Warning)),
                "{ClassName} - {MethodName} -- warning: {Message}");

            _debug = LoggerMessage.Define<string, string, string>(
                LogLevel.Debug,
                new EventId(EventIds.Debug, nameof(Debug)),
                "{ClassName} - {MethodName} -- debug: {Message}");

            _errorResult = LoggerMessage.Define<string, string, string>(
                LogLevel.Error,
                new EventId(EventIds.ErrorResult, nameof(ErrorResult)),
                "{ClassName} - {MethodName} -- response error result: {Message}");

            _mediaRResultWithTracing = LoggerMessage.Define<string, string, long, string, string>(
                LogLevel.Information,
                new EventId(EventIds.MediaRResult, nameof(MediaRResult)),
                "------------MEDIAR------------ {ClassName} - {MethodName} - [Latency:{Latency} ms -> LatencyRating:{LatencyRating}] -- response MEDIAR Key: {Message}");

            _kafkaErrorResult = LoggerMessage.Define<string, string, string>(
               LogLevel.Error,
               new EventId(EventIds.KafkaErrorResult, nameof(KafkaErrorResult)),
               "------------KAFKA------------ {ClassName} - {MethodName} -- response KAFKA error result: {Message}");

            _httpErrorResult = LoggerMessage.Define<string, string, string>(
                LogLevel.Error,
                new EventId(EventIds.HttpErrorResult, nameof(HttpErrorResult)),
                "------------HTTP------------ {ClassName} - {MethodName} -- response HTTP error result: {Message}");

            _httpResult = LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(EventIds.HttpResult, nameof(HttpResult)),
                "------------HTTP------------ {ClassName} - {MethodName} -- response HTTP result: {Message}");

            _httpResultWithTracing = LoggerMessage.Define<string, string, string, long, string, string>(
                LogLevel.Information,
                new EventId(EventIds.HttpResult, nameof(HttpResult)),
                "------------HTTP------------ {MethodName} - [{Uri} :: StatusCode:{StatusCode} :: Latency:{Latency}ms -> LatencyRating:{LatencyRating}] -- response HTTP result: {Message}");

            _error = LoggerMessage.Define<string, string, string>(
                LogLevel.Error,
                new EventId(EventIds.Error, nameof(Error)),
                "{ClassName} - {MethodName} -- error: {Message}");

            _failLogic = LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(EventIds.FailLogic, nameof(FailLogic)),
                "{ClassName} - {MethodName} -- fail logic: {Message}");

            _errorException = LoggerMessage.Define<string, string, string>(
                LogLevel.Error,
                new EventId(EventIds.ErrorException, nameof(ErrorException)),
                "{ClassName} - {MethodName} -- error: {Message}");

            _connection = LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(EventIds.Connection, nameof(Connection)),
                "------------CONNECTION------------ {ClassName} - {MethodName} -- message: {Message}.");

            _connectionError = LoggerMessage.Define<string, string, string>(
                LogLevel.Error,
                new EventId(EventIds.ConnectionError, nameof(ConnectionError)),
                "------------CONNECTION------------ {ClassName} - {MethodName} -- error message: {Message}.");
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

        public static void Request(this ILogger logger, string className, string methodName, Exception e = null)
        {
            _requestWithoutParams(logger, className, methodName, e);
        }

        public static void Request(this ILogger logger, string className, string methodName, string requestName, object parameters, Exception e = null)
        {
            _request(logger, className, methodName, requestName, parameters.ToJSon(), e);
        }

        public static void Request(this ILogger logger, string className, string methodName, string requestName, string parameters, Exception e = null)
        {
            _request(logger, className, methodName, requestName, parameters, e);
        }

        public static void Response(this ILogger logger, string className, string methodName, Exception e = null)
        {
            _responseWithoutParams(logger, className, methodName, e);
        }

        public static void Response(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _response(logger, className, methodName, message, e);
        }

        public static void Response(this ILogger logger, string className, string methodName, long latency, string message, Exception e = null)
        {
            _responseWithTracing(logger, className, methodName, latency, LatencyRatingData(latency: latency), message, e);
        }

        public static void Info(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _info(logger, className, methodName, message, e);
        }

        public static void Info<T>(this ILogger logger, string className, string methodName, FilterDefinition<T> parameters, Exception e = null) where T : class
        {
            try
            {
                MongoDB.Bson.BsonDocument bsonFilterElements =
                  parameters.Render(new RenderArgs<T>(
                      BsonSerializer.SerializerRegistry.GetSerializer<T>(),
                      BsonSerializer.SerializerRegistry
                  ));

                _infoWithFilterDefinition(logger, className, methodName, bsonFilterElements?.ToJSon(), e);
            }
            catch (Exception exception)
            {
                _errorException(logger, className, methodName, $"Error while rendering FilterDefinition to BsonDocument of {typeof(T)?.Name}", exception);
            }
        }

        public static void Info(this ILogger logger, string className, string methodName, List<BsonDocument> parameters, Exception e = null)
        {
            try
            {
                string planJson = parameters.ToJson(new MongoDB.Bson.IO.JsonWriterSettings
                {
                    Indent = false,
                    OutputMode = JsonOutputMode.RelaxedExtendedJson
                });

                _infoWithFilterDefinition(logger, className, methodName, planJson, e);
            }
            catch (Exception exception)
            {
                _errorException(logger, className, methodName, $"Error while rendering BsonDocument[]", exception);
            }
        }

        public static void Warning(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _warning(logger, className, methodName, message, e);
        }

        public static void Debug(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _debug(logger, className, methodName, message, e);
        }

        public static void ErrorResult(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _errorResult(logger, className, methodName, message.ToJSon(), e);
        }

        public static void ErrorResult(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _errorResult(logger, className, methodName, message, e);
        }

        public static void MediaRResult(this ILogger logger, string className, string methodName, long latency, string message, Exception e = null)
        {
            _mediaRResultWithTracing(logger, className, methodName, latency, LatencyRatingData(latency: latency), message, e);
        }

        public static void KafkaErrorResult(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _kafkaErrorResult(logger, className, methodName, message.ToJSon(), e);
        }

        public static void KafkaErrorResult(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _kafkaErrorResult(logger, className, methodName, message.ToJSon(), e);
        }

        public static void HttpErrorResult(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _httpErrorResult(logger, className, methodName, message.ToJSon(), e);
        }

        public static void HttpErrorResult(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _httpErrorResult(logger, className, methodName, message.ToJSon(), e);
        }

        public static void HttpResult(this ILogger logger, string className, string methodName, object message, Exception e = null)
        {
            _httpResult(logger, className, methodName, message.ToJSon(), e);
        }

        public static void HttpResult(this ILogger logger, string className, string methodName, string statusCode, long latency, string uri, string message, Exception e = null)
        {
            _httpResultWithTracing(logger, $"{className}_{methodName}", uri, statusCode, latency, LatencyRatingData(latency: latency), message, e);
        }

        public static void Error(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _error(logger, className, methodName, message, e);
        }

        public static void FailLogic(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _failLogic(logger, className, methodName, message, e);
        }

        public static void ErrorException(this ILogger logger, string className, string methodName, string message = "", Exception e = null)
        {
            _errorException(logger, className, methodName, $"{Environment.NewLine} -- Message: {message} {Environment.NewLine} -- Exception: {e?.Message?.Trim()} {Environment.NewLine} -- StackTrace: {e?.StackTrace?.Trim()} {Environment.NewLine} ", e);
        }

        public static void Connection(this ILogger logger, string className, string methodName, string message, Exception e = null)
        {
            _connection(logger, className, methodName, message, e);
        }

        public static void ConnectionError(this ILogger logger, string className, string methodName, string message = "", Exception e = null)
        {
            _connectionError(logger, className, methodName, $"{Environment.NewLine} -- Message: {message} {Environment.NewLine} -- Exception: {e?.Message?.Trim()} {Environment.NewLine} -- StackTrace: {e?.StackTrace?.Trim()} {Environment.NewLine} ", e);
        }
    }
}