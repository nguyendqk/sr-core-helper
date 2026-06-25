using Confluent.Kafka;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace FTELSRCore.Extensions
{
    public class KafkaExtensions
    {
        public class JSonSerializer<T> : ISerializer<T>
        {
            public byte[] Serialize(T data, SerializationContext context)
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            }
        }

        public class JSonDeserializer<T> : IDeserializer<T>
        {
            public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
            {
                if (isNull)
                {
                    return default;
                }

                if (!Encoding.UTF8.GetString(data).JSonTryParse(out T _))
                {
                    return default;
                }

                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
            }
        }
    }

    public static class CollectionExtensions
    {
        public static string CollectionName(this Type type)
        {
            return type.GetCustomAttribute<CollectionNameAttribute>()?.CollectionName ?? type.Name;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class CollectionNameAttribute(string name) : Attribute
    {
        public string CollectionName { get; } = name;
    }
}