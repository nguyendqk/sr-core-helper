using Confluent.Kafka;
using System.Collections;
using System.Reflection;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.LoggerConfigurationExtensions
{
    public static class SRProducerConfigExtensions
    {
        private const string SerilogEnvVar = "SERILOG__KAFKA__";

        public static ProducerConfig LoadFromEnvironmentVariables(this ProducerConfig config)
        {
            IDictionary environmentVariables = Environment.GetEnvironmentVariables();

            foreach (DictionaryEntry item in environmentVariables)
            {
                string text = item.Key?.ToString() ?? string.Empty;
                string value = item.Value?.ToString() ?? string.Empty;

                if (text.StartsWith(SerilogEnvVar))
                {
                    string key = text.Replace(SerilogEnvVar, string.Empty).Replace("_", "").ToLower();

                    try
                    {
                        config.SetValue(key, value);
                    }
                    catch (Exception exception)
                    {
                        CommonBaseConstant.ConfigLoggerExceptionByConsole(
                            nameof(SRProducerConfigExtensions), nameof(LoadFromEnvironmentVariables),
                            exception: exception, description: $"Bỏ qua biến môi trường không hợp lệ: {text}");
                    }
                }
            }

            return config;
        }

        public static ProducerConfig SetValue(this ProducerConfig config, string key, object value)
        {
            SetValues(obj: config,
                propertyName: key,
                stringValue: value?.ToString() ?? string.Empty);

            return config;
        }

        private static void SetValues(object obj, string propertyName, string stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return;
            }

            PropertyInfo propertyInfo =
                obj.GetType()
                    .GetProperties()
                    .SingleOrDefault((PropertyInfo x) => x.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase));

            object objValue = null;

            if (propertyInfo is null)
            {
                throw new CustomException(message: "A property (" + propertyName + ") could not be found in Confluent.Kafka)");
            }

            Dictionary<Type, Action> dictionary = new()
            {
                {
                    typeof(string),
                    delegate { objValue = stringValue; }
                },
                {
                    typeof(int?),
                    delegate { objValue = int.Parse(stringValue); }
                },
                {
                    typeof(bool?),
                    delegate { objValue = bool.Parse(stringValue); }
                },
                {
                    typeof(Partitioner?),
                    delegate { objValue = System.Enum.Parse(typeof(Partitioner), stringValue); }
                },
                {
                    typeof(CompressionType?),
                    delegate { objValue = System.Enum.Parse(typeof(CompressionType), stringValue); }
                },
                {
                    typeof(SecurityProtocol?),
                    delegate { objValue = System.Enum.Parse(typeof(SecurityProtocol), stringValue); }
                },
                {
                    typeof(SaslMechanism?),
                    delegate { objValue = System.Enum.Parse(typeof(SaslMechanism), stringValue); }
                },
                {
                    typeof(BrokerAddressFamily?),
                    delegate { objValue = System.Enum.Parse(typeof(BrokerAddressFamily), stringValue); }
                },
                {
                    typeof(Acks?),
                    delegate { objValue = System.Enum.Parse(typeof(Acks), stringValue); }
                },
                {
                    typeof(SslEndpointIdentificationAlgorithm?),
                    delegate { objValue = System.Enum.Parse(typeof(SslEndpointIdentificationAlgorithm), stringValue); }
                }
            };

            Dictionary<Type, Action> dictionary2 = dictionary;

            if (!dictionary2.TryGetValue(propertyInfo.PropertyType, out Action setter))
            {
                throw new CustomException(
                    message: $"Kiểu ({propertyInfo.PropertyType.Name}) của property ({propertyName}) chưa được hỗ trợ trong SRProducerConfigExtensions.");
            }

            setter();

            if (objValue is null)
            {
                throw new CustomException(message: stringValue + " could not be assigned to " + propertyName + " (" + propertyInfo.PropertyType.Name + ")");
            }

            propertyInfo.SetValue(obj, objValue);
        }
    }
}