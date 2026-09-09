using System.Text.Json.Serialization;

namespace FTELSRCore.Models.Https.ResponseModel
{
    public class ACSResponseModel<TData> where TData : notnull
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("Data")]
        public TData Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
