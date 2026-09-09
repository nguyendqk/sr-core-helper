using System.Text.Json.Serialization;

namespace FTELSRCore.Models.Https.ResponseModel
{
    public class CPEResponseModel<TData> where TData : notnull
    {
        [JsonPropertyName("status_key")]
        public string StatusKey { get; set; }

        [JsonPropertyName("data")]
        public TData Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
