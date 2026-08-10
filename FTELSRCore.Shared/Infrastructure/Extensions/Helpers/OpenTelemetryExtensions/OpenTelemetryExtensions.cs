using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.OpenTelemetryExtensions
{
    public static partial class OpenTelemetryExtensions
    {
        public static TracerProviderBuilder AddFTELSRTracing(
            this TracerProviderBuilder builder, TracingFTELSRModel model)
        {
            return builder
                .AddFusionCacheInstrumentation()
                .AddSource(names: model.ServiceName)
                .AddSource(names: OpenTelemetryConstant.CoreCacheActivitySource)
                .AddSource(names: OpenTelemetryConstant.LoggingBehaviorActivitySource)
                .ConfigureResource(resource =>
                {
                    resource.AddService(model.ServiceName);
                })
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.FilterHttpRequestMessage = _ => true;

                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        activity.DisplayName =
                            $"{request.Method} {request.RequestUri?.AbsolutePath}";
                    };
                });
        }

        public class TracingFTELSRModel
        {
            public string ServiceName { get; set; }
        }
    }

    public static partial class OpenTelemetryExtensions
    {
        public static MeterProviderBuilder AddFTELSRMetrics(
            this MeterProviderBuilder builder, MetricFTELSRModel model)
        {
            return builder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(names: model.ServiceName)
                .AddMeter(names: OpenTelemetryConstant.CoreCacheActivitySource)
                .AddMeter(names: OpenTelemetryConstant.LoggingBehaviorActivitySource)
                .ConfigureResource(resource =>
                {
                    resource.AddService(model.ServiceName);
                });
        }

        public class MetricFTELSRModel
        {
            public string ServiceName { get; set; }
        }
    }
}