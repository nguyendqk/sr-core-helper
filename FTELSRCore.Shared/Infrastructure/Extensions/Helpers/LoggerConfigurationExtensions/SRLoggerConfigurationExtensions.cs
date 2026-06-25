// Ignore Spelling: sasl Username ssl

using Confluent.Kafka;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.LoggerConfigurationExtensions
{
    public static class SRLoggerConfigurationExtensions
    {
        public static LoggerConfiguration SRKafkaSink(this LoggerSinkConfiguration loggerConfiguration,
                                                      int period = 5,
                                                      string topic = "logs",
                                                      int batchSizeLimit = 50,
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
                                                        Period = TimeSpan.FromSeconds(period)
                                                    });

            return loggerConfiguration.Sink(logEventSink);
        }
    }
}