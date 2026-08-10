using Confluent.Kafka;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Text;

namespace FTELSRCore.Extensions
{
    public static class KafkaTraceContextExtensions
    {
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public static void InjectProducerTraceContext(Headers headers)
        {
            ArgumentNullException.ThrowIfNull(headers);

            var propagationContext = new PropagationContext(
                Activity.Current?.Context ?? default, Baggage.Current);

            Propagator.Inject(
                propagationContext,
                headers,
                static (carrier, key, value) =>
                {
                    carrier.Add(
                        key,
                        Encoding.UTF8.GetBytes(value));
                });
        }

        public static PropagationContext ExtractConsumerTraceContext(Headers headers)
        {
            if (headers is null)
            {
                return default;
            }

            return Propagator.Extract(
                default,
                headers,
                static (carrier, key) =>
                {
                    var header = carrier.LastOrDefault(x =>
                        x.Key.Equals(
                            key,
                            StringComparison.OrdinalIgnoreCase));

                    return header is null
                        ? []
                        : [Encoding.UTF8.GetString(header.GetValueBytes())];
                });
        }
    }
}