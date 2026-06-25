// Ignore Spelling: Username sasl ssl

using Confluent.Kafka;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Sinks.PeriodicBatching;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.LoggerConfigurationExtensions
{
    public sealed record SRKafkaSinkExtensions : IBatchedLogEventSink
    {
        private const double FlushTimeoutSecs = 10.0;

        private readonly ITextFormatter _formatter;
        private IProducer<string, byte[]> _producer;
        private readonly TopicPartition _globalTopicPartition;
        private readonly Func<LogEvent, string> _topicDecider;

        public SRKafkaSinkExtensions(string bootstrapServers,
                                     SecurityProtocol securityProtocol,
                                     SaslMechanism saslMechanism,
                                     string saslUsername,
                                     string saslPassword,
                                     string sslCaLocation,
                                     string topic = null,
                                     Func<LogEvent, string> topicDecider = null,
                                     ITextFormatter formatter = null)
        {
            ConfigureKafkaConnection(bootstrapServers, securityProtocol, saslMechanism, saslUsername, saslPassword, sslCaLocation);

            _formatter = formatter ?? new JsonFormatter(null, renderMessage: true);

            if (topic is not null)
            {
                _globalTopicPartition = new TopicPartition(topic, Partition.Any);
            }

            if (topicDecider is not null)
            {
                _topicDecider = topicDecider;
            }
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            try
            {
                foreach (LogEvent item in batch)
                {
                    TopicPartition topicPartition =
                        _topicDecider is null ? _globalTopicPartition
                        : new TopicPartition(_topicDecider(item), Partition.Any);

                    Message<string, byte[]> message;

                    using (StringWriter stringWriter = new(CultureInfo.InvariantCulture))
                    {
                        _formatter.Format(item, stringWriter);

                        message = new Message<string, byte[]>
                        {
                            Key = ((int)item.Level).ToString(),
                            Value = Encoding.UTF8.GetBytes(stringWriter.ToString())
                        };
                    }

                    _producer!.Produce(topicPartition, message);
                }
            }
            catch (Exception exception)
            {
                CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(SRKafkaSinkExtensions), nameof(EmitBatchAsync), exception);
            }

            _producer!.Flush(TimeSpan.FromSeconds(FlushTimeoutSecs));

            return Task.CompletedTask;
        }

        private void ConfigureKafkaConnection(string bootstrapServers,
                                              SecurityProtocol securityProtocol,
                                              SaslMechanism saslMechanism,
                                              string saslUsername,
                                              string saslPassword,
                                              string sslCaLocation)
        {
            ProducerConfig config = new ProducerConfig()
                .SetValue("ApiVersionFallbackMs", 0)
                .SetValue("EnableDeliveryReports", false)
                .LoadFromEnvironmentVariables()
                .SetValue("BootstrapServers", bootstrapServers)
                .SetValue("SecurityProtocol", securityProtocol)
                .SetValue("SaslMechanism", saslMechanism)
                .SetValue("SslCaLocation", string.IsNullOrEmpty(sslCaLocation) ? null : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), sslCaLocation))
                .SetValue("SaslUsername", saslUsername)
                .SetValue("SaslPassword", saslPassword);

            _producer = new ProducerBuilder<string, byte[]>(config).Build();
        }
    }
}