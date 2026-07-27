using Confluent.Kafka;
using FTELSRCore.Infrastructure.Extensions.Helpers.LoggerConfigurationExtensions.SRKafkaSinks;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.LoggerConfigurationExtensions
{
    #region :::::::::::::::::::: SRKafkaSink ::::::::::::::::::::

    public static partial class SRLoggerConfigurationExtensions
    {
        public static LoggerConfiguration SRKafkaSink(this LoggerSinkConfiguration loggerConfiguration,
                                                      int period = 5,
                                                      string topic = "logs",
                                                      int batchSizeLimit = 50,
                                                      int queueLimit = 10_000,
                                                      string saslUsername = null,
                                                      string saslPassword = null,
                                                      string sslCaLocation = null,
                                                      ITextFormatter formatter = null,
                                                      string bootstrapServers = "localhost:9092",
                                                      SaslMechanism saslMechanism = SaslMechanism.Plain,
                                                      SecurityProtocol securityProtocol = SecurityProtocol.Plaintext)
        {
            return loggerConfiguration.SRKafka(topic: topic,
                                               period: period,
                                               topicDecider: null,
                                               formatter: formatter,
                                               queueLimit: queueLimit,
                                               saslUsername: saslUsername,
                                               saslPassword: saslPassword,
                                               saslMechanism: saslMechanism,
                                               sslCaLocation: sslCaLocation,
                                               batchSizeLimit: batchSizeLimit,
                                               securityProtocol: securityProtocol,
                                               bootstrapServers: bootstrapServers);
        }

        private static LoggerConfiguration SRKafka(this LoggerSinkConfiguration loggerConfiguration,
                                                   int period,
                                                   string topic,
                                                   int batchSizeLimit,
                                                   int queueLimit,
                                                   string saslUsername,
                                                   string saslPassword,
                                                   string sslCaLocation,
                                                   string bootstrapServers,
                                                   ITextFormatter formatter,
                                                   SaslMechanism saslMechanism,
                                                   SecurityProtocol securityProtocol,
                                                   Func<LogEvent, string> topicDecider)
        {
            SRKafkaSinkExtensions batchedSink = new(topic: topic,
                                                    formatter: formatter,
                                                    saslUsername: saslUsername,
                                                    saslPassword: saslPassword,
                                                    saslMechanism: saslMechanism,
                                                    sslCaLocation: sslCaLocation,
                                                    topicDecider: topicDecider,
                                                    bootstrapServers: bootstrapServers,
                                                    securityProtocol: securityProtocol);

            PeriodicBatchingSink logEventSink = new(batchedSink: batchedSink,
                                                    options: new()
                                                    {
                                                        BatchSizeLimit = batchSizeLimit,
                                                        Period = TimeSpan.FromSeconds(period),
                                                        // Chặn tăng trưởng bộ nhớ không giới hạn khi Kafka broker
                                                        // gián đoạn kéo dài — mặc định của thư viện là unbounded.
                                                        QueueLimit = queueLimit
                                                    });

            return loggerConfiguration.Sink(logEventSink);
        }
    }

    #endregion :::::::::::::::::::: SRKafkaSink ::::::::::::::::::::

    #region :::::::::::::::::::: SRKafkaSink ::::::::::::::::::::

    public static partial class SRLoggerConfigurationExtensions
    {
        public static LoggerConfiguration TenantSRKafkaSinks(
            this LoggerSinkConfiguration loggerConfiguration,
            List<TenantSRKafkaSinkModel> kafkaSinkModels,
            ITextFormatter formatter = null,
            int period = 5, int batchSizeLimit = 50, int queueLimit = 10_000)
        {
            return loggerConfiguration.TenantSRKafkas(period: period,
                                                      formatter: formatter,
                                                      batchSizeLimit: batchSizeLimit,
                                                      queueLimit: queueLimit,
                                                      kafkaSinkModels: kafkaSinkModels);
        }

        private static LoggerConfiguration TenantSRKafkas(
            this LoggerSinkConfiguration loggerConfiguration,
            List<TenantSRKafkaSinkModel> kafkaSinkModels,
            ITextFormatter formatter = null,
            int period = 5, int batchSizeLimit = 50, int queueLimit = 10_000)
        {
            TenantSRKafkaSinkExtensions batchedSink = new(kafkaSinkModels, formatter);

            PeriodicBatchingSink logEventSink = new(
                batchedSink: batchedSink,
                options: new PeriodicBatchingSinkOptions
                {
                    BatchSizeLimit = batchSizeLimit,
                    Period = TimeSpan.FromSeconds(period),
                    // Chặn tăng trưởng bộ nhớ không giới hạn khi Kafka broker
                    // gián đoạn kéo dài — mặc định của thư viện là unbounded.
                    QueueLimit = queueLimit
                });

            return loggerConfiguration.Sink(logEventSink);
        }
    }

    #endregion :::::::::::::::::::: SRKafkaSink ::::::::::::::::::::
}