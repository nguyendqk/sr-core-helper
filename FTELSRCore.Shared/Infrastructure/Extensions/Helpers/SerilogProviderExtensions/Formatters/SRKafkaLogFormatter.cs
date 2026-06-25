using Serilog.Events;
using Serilog.Formatting;
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
                Level = logEvent.Level.ToString(),

                Message = logEvent.RenderMessage(),

                TimeStampFormat = logEvent.Timestamp.ToString("dd/MM/yyyy HH:mm:ss.fff"),

                ActionId = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ActionIdPropertyName),

                ActionName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ActionNamePropertyName),

                ClassName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ClassNamePropertyName),

                ClientIp = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ClientIpPropertyName),

                CorrelationId = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.CorrelationIdPropertyName),

                EnvironmentName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.EnvironmentNamePropertyName),

                EventId = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.EventIdNamePropertyName),

                MachineName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.MachineNamePropertyName),

                MethodName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.MethodNamePropertyName),

                Parameters = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ParametersPropertyName),

                RequestId = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.RequestIdPropertyName),

                RequestName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.RequestNamePropertyName),

                RequestPath = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.RequestPathPropertyName),

                ServiceName = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.ServiceNamePropertyName),

                SourceContext = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.SourceContextPropertyName),

                User = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.UserPropertyName),

                UserInfo = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.UserInfoPropertyName),

                UserAgent = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.UserAgentPropertyName),

                DynamicRule = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.DynamicRule),

                XCorrelationId = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.x_correlation_idPropertyName),

                #region ::::::::::::: Dùng cho kiểm tra kết nối API :::::::::::::

                Uri = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.Uri),

                Latency = long.TryParse(GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.Latency), out var latency) ? latency : 0,

                StatusCode = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.StatusCode),

                LatencyRating = GetLogEventPropertyValue(logEvent.Properties, SerilogConstant.LatencyRating)

                #endregion ::::::::::::: Dùng cho kiểm tra kết nối API :::::::::::::
            };

            if ((string.IsNullOrWhiteSpace(logTemplate.XCorrelationId)
                || logTemplate.XCorrelationId.Equals(CommonBaseConstant.Anonymous, StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(logTemplate.CorrelationId))
            {
                logTemplate.XCorrelationId = logTemplate.CorrelationId;
            }

            output.Write(JsonSerializer.Serialize(value: logTemplate, options: _jsonSerializerOptions));
        }

        private sealed class LogTemplateModel
        {
            public string Level { get; set; }

            public string Message { get; set; }

            public string TimeStampFormat { get; set; }

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
            public string EnvironmentName { get; set; }

            /// <summary>
            /// Thông tin về EventId.
            /// </summary>
            ///
            public string EventId { get; set; }

            /// <summary>
            /// Tên của máy chủ thực hiện request.
            /// </summary>
            ///
            public string MachineName { get; set; }

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
            public string XCorrelationId { get; set; }

            /// <summary>
            /// Nhân sự thao tác
            /// </summary>
            ///
            public string User { get; set; }

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

            /// <summary>
            /// Thông tin đường dẫn API.
            /// </summary>
            ///
            public string Uri { get; set; }

            /// <summary>
            /// Thời gian xử lý của API
            /// </summary>
            /// 
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public long Latency { get; set; }

            /// <summary>
            /// Tình trạng xử lý của API
            /// </summary>
            ///
            public string StatusCode { get; set; }

            /// <summary>
            /// Đánh giá xem thời gian xử lý là như nào.
            /// </summary>
            ///
            public string LatencyRating { get; set; }
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