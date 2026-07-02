using Confluent.Kafka;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Sinks.PeriodicBatching;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.LoggerConfigurationExtensions.SRKafkaSinks
{
    public sealed record TenantSRKafkaSinkExtensions : IBatchedLogEventSink
    {
        #region :::::::: Ctor ::::::::

        private const double FlushTimeoutSecs = 10.0;

        private readonly ITextFormatter _formatter;

        private readonly IDictionary<LogEventLevel, (IProducer<string, byte[]> Producer, TopicPartition TopicPartition)> _keyValuePairs
            = new Dictionary<LogEventLevel, (IProducer<string, byte[]>, TopicPartition)>();

        public TenantSRKafkaSinkExtensions(
            List<TenantSRKafkaSinkModel> tenantSRKafkaSinkModels,
            ITextFormatter formatter = null)
        {
            SetupConfiguration(tenantSRKafkaSinkModels);

            _formatter = formatter ?? new JsonFormatter(null, renderMessage: true);
        }

        #endregion :::::::: Ctor ::::::::

        private void SetupConfiguration(List<TenantSRKafkaSinkModel> tenantSRKafkaSinkModels)
        {
            if (tenantSRKafkaSinkModels.IsNullOrEmpty())
            {
                throw new CustomException(message: "Tenant Kafka Sink Models cannot be null or empty.");
            }

            foreach (IGrouping<List<LogEventLevel>, TenantSRKafkaSinkModel> item in tenantSRKafkaSinkModels.GroupBy(x => x.LogEventLevels))
            {
                TenantSRKafkaSinkModel value = item?.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(value?.Topic)
                    || item.Key.IsNullOrEmpty())
                {
                    continue;
                }

                foreach (LogEventLevel logEventLevel in item.Key)
                {
                    if (_keyValuePairs is null 
                        || _keyValuePairs.ContainsKey(logEventLevel))
                    {
                        CommonBaseConstant.ConfigLoggerInformationByConsole(nameof(TenantSRKafkaSinkExtensions), nameof(SetupConfiguration),
                            description: $"Duplicate configuration for log event level {logEventLevel} is found. Skipping this configuration.");

                        continue;
                    }

                    _keyValuePairs.Add(
                        key: logEventLevel,
                        value: (Producer: ProducerConfig(value.BootstrapServers,
                                                         value.SecurityProtocol,
                                                         value.SaslMechanism,
                                                         value.SaslUsername,
                                                         value.SaslPassword,
                                                         value.SslCaLocation),
                                TopicPartition: new TopicPartition(value.Topic, Partition.Any)));
                }
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
                    LogEventLevel logEventLevel = item.Level;

                    Message<string, byte[]> message;

                    using (StringWriter stringWriter = new(CultureInfo.InvariantCulture))
                    {
                        _formatter.Format(item, stringWriter);

                        message = new Message<string, byte[]>
                        {
                            Key = ((int)logEventLevel).ToString(),
                            Value = Encoding.UTF8.GetBytes(stringWriter.ToString())
                        };
                    }

                    (IProducer<string, byte[]> Producer, TopicPartition TopicPartition) configuration = _keyValuePairs.TryGetValue(logEventLevel, out var value) ? value : default;

                    if (configuration == default)
                    {
                        CommonBaseConstant.ConfigLoggerInformationByConsole(nameof(TenantSRKafkaSinkExtensions), nameof(EmitBatchAsync),
                            description: $"configuration for log event level {logEventLevel} is empty.");

                        continue;
                    }

                    configuration.Producer.Produce(topicPartition: configuration.TopicPartition, message: message);
                }
            }
            catch (Exception exception)
            {
                CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(TenantSRKafkaSinkExtensions), nameof(EmitBatchAsync), exception: exception);
            }

            foreach ((IProducer<string, byte[]> Producer, TopicPartition _) in _keyValuePairs.Values)
            {
                if (Producer == default)
                {
                    CommonBaseConstant.ConfigLoggerInformationByConsole(nameof(TenantSRKafkaSinkExtensions), nameof(EmitBatchAsync), description: "Producer is empty.");

                    continue;
                }

                Producer.Flush(TimeSpan.FromSeconds(FlushTimeoutSecs));
            }

            return Task.CompletedTask;
        }

        private static IProducer<string, byte[]> ProducerConfig(
            string bootstrapServers,
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
                .SetValue("SslCaLocation", !string.IsNullOrEmpty(sslCaLocation)
                                           ? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), sslCaLocation)
                                           : null)
                .SetValue("SaslUsername", saslUsername)
                .SetValue("SaslPassword", saslPassword);

            return new ProducerBuilder<string, byte[]>(config).Build();
        }
    }

    public record TenantSRKafkaSinkModel(
        List<LogEventLevel> LogEventLevels,
        string Topic,
        string BootstrapServers,
        SaslMechanism SaslMechanism,
        SecurityProtocol SecurityProtocol,
        string SaslUsername,
        string SaslPassword,
        string SslCaLocation = null);
}