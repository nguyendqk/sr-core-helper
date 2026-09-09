using System.Text.Json.Serialization;

namespace FTELSRCore.Models.Https.ResponseModel
{
    public class TicketSupportResponseModel<TData> where TData : notnull
    {
        [JsonPropertyName("ErrorCode")]
        public int? ErrorCode { get; set; }

        [JsonPropertyName("ErrorDescription")]
        public string ErrorDescription { get; set; }

        [JsonPropertyName("data")]
        public TData Data { get; set; }
    }
}
