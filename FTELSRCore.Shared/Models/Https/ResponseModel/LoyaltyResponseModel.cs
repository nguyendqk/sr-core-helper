using System.Text.Json.Serialization;

namespace FTELSRCore.Models.Https.ResponseModel
{
    public class LoyaltyResponseModel<TData> where TData : notnull
    {
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public TData Data { get; set; }
    }
}
