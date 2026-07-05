using Serilog.Events;
using Serilog.Formatting;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.SerilogProviderExtensions.Formatters
{
    public class SRKafkaLogFormatter : ITextFormatter
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        public SRKafkaLogFormatter()
        {
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            LogTemplateModel logTemplate = new()
            {
                SpanId = logEvent.SpanId,

                TraceId = logEvent.TraceId,

                Level = logEvent.Level.ToString(),

                Message = logEvent.RenderMessage(),

                LocalTimeStamp = logEvent.Timestamp.ToString("dd-MM-yyyy HH:mm:ss.fff"),

                ActionId = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ActionIdPropertyName),

                ActionName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ActionNamePropertyName),

                ClassName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ClassNamePropertyName),

                ClientIp = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ClientIpPropertyName),

                CorrelationId = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.CorrelationIdPropertyName),

                Environment = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.EnvironmentNamePropertyName),

                EventId = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.EventIdNamePropertyName),

                Pod = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.MachineNamePropertyName),

                MethodName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.MethodNamePropertyName),

                Parameters = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ParametersPropertyName),

                RequestName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.RequestNamePropertyName),

                RequestPath = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.RequestPathPropertyName),

                ServiceName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ServiceNamePropertyName),

                SourceContext = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.SourceContextPropertyName),

                User = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.UserPropertyName),

                IPAddress = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ForwardedPropertyName),

                UserInfo = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.UserInfoPropertyName),

                UserAgent = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.UserAgentPropertyName),

                DynamicRule = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.DynamicRule),

                RequestId = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.RequestIdPropertyName),

                Topic = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.TopicPropertyName),

                StackTrace = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.StackTracePropertyName),

                ErrorMessage = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ErrorMessagePropertyName),

                ErrorCategory = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ErrorCategoryPropertyName),

                #region ::::::::::::: Dùng cho kiểm tra kết nối API :::::::::::::

                Endpoint = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.EndpointPropertyName),

                Direction = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.DirectionPropertyName),

                HttpMethod = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.HttpMethodPropertyName),

                SystemOwner = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.SystemOwnerPropertyName),

                HttpStatusCode = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.HttpStatusCodePropertyName),

                LatencyRating = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.LatencyRatingPropertyName),

                ResponseTimeMs = long.TryParse(GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ResponseTimeMsPropertyName), out var latency) ? latency : 0,

                #endregion ::::::::::::: Dùng cho kiểm tra kết nối API :::::::::::::
            };

            output.Write(JsonSerializer.Serialize(value: logTemplate, options: _jsonSerializerOptions));
        }

        private sealed class LogTemplateModel
        {
            public string Level { get; set; }

            public string Message { get; set; }

            public string LocalTimeStamp { get; set; }

            /// <summary>
            /// Mã định danh của Action.
            /// </summary>
            ///
            public string ActionId { get; set; }

            /// <summary>
            /// Tên của Action được gọi.
            /// </summary>
            ///
            public string ActionName { get; set; }

            /// <summary>
            /// Tên class xử lý request.
            /// </summary>
            ///
            public string ClassName { get; set; }

            /// <summary>
            /// Địa chỉ IP của client thực hiện request.
            /// </summary>
            ///
            public string ClientIp { get; set; }

            /// <summary>
            /// Mã định danh giúp theo dõi request qua nhiều hệ thống.
            /// </summary>
            ///
            public string CorrelationId { get; set; }

            /// <summary>
            /// Tên môi trường đang chạy (Local, Staging, Production).
            /// </summary>
            ///
            public string Environment { get; set; }

            /// <summary>
            /// Thông tin về EventId.
            /// </summary>
            ///
            public string EventId { get; set; }

            /// <summary>
            /// Tên của máy chủ thực hiện request.
            /// </summary>
            ///
            public string Pod { get; set; }

            /// <summary>
            /// Tên của phương thức đang thực thi.
            /// </summary>
            ///
            public string MethodName { get; set; }

            /// <summary>
            /// Tham số đầu vào của request.
            /// </summary>
            ///
            public string Parameters { get; set; }

            /// <summary>
            /// Mã định danh của request.
            /// </summary>
            ///
            public string RequestId { get; set; }

            /// <summary>
            /// Tên của request được gọi.
            /// </summary>
            ///
            public string RequestName { get; set; }

            /// <summary>
            /// Đường dẫn của request.
            /// </summary>
            ///
            public string RequestPath { get; set; }

            /// <summary>
            /// Tên service đang xử lý request.
            /// </summary>
            ///
            public string ServiceName { get; set; }

            /// <summary>
            /// Bối cảnh nguồn gốc của request.
            /// </summary>
            public string SourceContext { get; set; }

            /// <summary>
            /// Thông tin về người dùng thực hiện request.
            /// </summary>
            ///
            public string UserInfo { get; set; }

            /// <summary>
            /// Mã correlation ID được truyền theo request.
            /// </summary>
            ///
            public ActivityTraceId? TraceId { get; set; }

            public ActivitySpanId? SpanId { get; set; }

            /// <summary>
            /// Nhân sự thao tác
            /// </summary>
            ///
            public string User { get; set; }

            public string IPAddress { get; set; }

            /// <summary>
            /// User agent của client thực hiện request.
            /// </summary>
            ///
            public string UserAgent { get; set; }

            /// <summary>
            /// Rule tự gán.
            /// </summary>
            ///
            public string DynamicRule { get; set; }

            public string ErrorCategory { get; set; }

            #region ::::::::::::: Dùng cho kiểm tra kết nối API :::::::::::::

            /// <summary>
            /// Thông tin đường dẫn API.
            /// </summary>
            ///
            public string Endpoint { get; set; }

            /// <summary>
            /// Thông tin HTTP Method.
            /// </summary>
            ///
            public string HttpMethod { get; set; }

            /// <summary>
            /// Thời gian xử lý của API
            /// </summary>
            ///
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public long ResponseTimeMs { get; set; }

            /// <summary>
            /// Kích thước request body (bytes)
            /// </summary>
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public long RequestSize { get; set; }

            /// <summary>
            /// Kích thước response body (bytes)
            /// </summary>
            ///
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public long ResponseSize { get; set; }

            /// <summary>
            /// Tình trạng xử lý của API
            /// </summary>
            ///
            public string HttpStatusCode { get; set; }

            /// <summary>
            /// Hệ thống xử quản lý
            /// </summary>
            ///
            public string SystemOwner { get; set; }

            /// <summary>
            /// Gọi nội bộ hay gọi sang 1 hệ thống khác
            /// </summary>
            ///
            public string Direction { get; set; } = nameof(DirectionType.Inbound);

            #endregion ::::::::::::: Dùng cho kiểm tra kết nối API :::::::::::::

            /// <summary>
            /// Đánh giá xem thời gian xử lý là như nào.
            /// </summary>
            ///
            public string LatencyRating { get; set; }

            public string Topic { get; set; }

            #region :::::::::::::::::::::::::::::::: Error ::::::::::::::::::::::::::::::::

            /// <summary>
            /// Chi tiết lỗi (không chứa PII)
            /// </summary>
            ///
            public string ErrorMessage { get; set; }

            /// <summary>
            /// Stack trace
            /// </summary>
            ///
            public string StackTrace { get; set; }

            #endregion :::::::::::::::::::::::::::::::: Error ::::::::::::::::::::::::::::::::
        }

        public enum DirectionType
        {
            Outbound = 0,
            Inbound = 1,
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        ///
        internal static string GetLogEventPropertyValue(IReadOnlyDictionary<string, LogEventPropertyValue> value, string type)
        {
            if (value is null || string.IsNullOrWhiteSpace(type)) return null;

            if (value.TryGetValue(type, out LogEventPropertyValue sourceContextValue) is false
                || sourceContextValue?.ToString() == "null")
            {
                return null;
            }

            if (sourceContextValue is ScalarValue { Value: not null } scalarValue
                && scalarValue.Value.ToString() != "null")
            {
                return scalarValue.Value?.ToString();
            }

            return sourceContextValue?.ToString();
        }
    }
}