using System.Text.Json.Serialization;

namespace FTELSRCore.Models.Https.ResponseModel
{
    public class CoreReinventResponseModel<TData> where TData : notnull
    {
        [JsonPropertyName("errorData")]
        public string ErrorData { get; set; }

        [JsonPropertyName("exceptionMessage")]
        public string ExceptionMessage { get; set; }

        [JsonPropertyName("clientRequestId")]
        public string ClientRequestId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public TData Data { get; set; }
    }
}
