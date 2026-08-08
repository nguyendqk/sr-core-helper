using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.OpenTelemetryExtensions
{
    public static class OpenTelemetryExtensions
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
}