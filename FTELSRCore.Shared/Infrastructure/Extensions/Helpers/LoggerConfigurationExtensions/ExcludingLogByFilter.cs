using FTELSRCore.Infrastructure.Extensions.Helpers.SerilogProviderExtensions.Formatters;
using Serilog.Events;
using Serilog.Filters;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.LoggerConfigurationExtensions
{
    public static class ExcludingLoggerExtensions
    {
        public static bool ExcludeRemoveLogHealthCheck(this LogEvent e, string subDirectory)
        {
            if (Matching.FromSource(CommonBaseConstant.UserAgentCore)(e) is false)
            {
                return false;
            }

            if (e.Properties.IsNullOrEmpty())
            {
                return false;
            }

            string value =
                SRKafkaLogFormatter.GetLogEventPropertyValue(
                    value: e.Properties, type: SerilogConstant.RequestPathPropertyName);

            // Bỏ qua tất cả các log liên quan đến healthcheck hệ thống dành cho system.
            if (!string.IsNullOrWhiteSpace(value) && value.Equals(subDirectory) is true)
            {
                return true;
            }

            return false;
        }
    }
}