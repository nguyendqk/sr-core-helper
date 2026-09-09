using System.Text.Json.Serialization;

namespace FTELSRCore.Models.Https.ResponseModel
{
    public class ProductInternetResponseModel<TData> where TData : notnull
    {
        [JsonPropertyName("error")]
        public int? Error { get; set; }

        [JsonPropertyName("error_data")]
        public string ErrorData { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public TData Data { get; set; }
    }
}
