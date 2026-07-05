using System.Text.Json.Serialization;

namespace FTELSRCore.Wrappers
{
    public interface IResult<out T> : IResult
    {
        [JsonPropertyName("data")]
        T Data { get; }
    }

    public partial interface IResult
    {
        /// <summary>
        /// HttpStatusCode dạng số.
        /// </summary>
        ///
        [JsonPropertyName("code")]
        int Code { get; set; }

        /// <summary>
        /// HttpStatusCode dạng tên.
        /// </summary>
        ///
        [JsonPropertyName("status")]
        string Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("success")]
        bool Success { get; set; }

        /// <summary>
        /// Tình trạng xử lý tại hệ thống
        /// [QUY ĐỊNH]:
        /// true: Hệ thống chập nhận các Rule hệ thống cho phép đến hàm xử lý yêu cầu.
        /// false: Hệ thống từ chối yêu cầu ngay tại Rule hệ thống, không đến hàm xử lý yêu cầu.
        /// </summary>
        ///
        [JsonPropertyName("dispatched")]
        bool Dispatched { get; set; }

        //[JsonPropertyName("succeeded")]
        //bool Succeeded { get; set; }

        /// <summary>
        /// Hệ thống xử lý yêu cầu
        /// </summary>
        ///
        [JsonPropertyName("system")]
        string System { get; set; }

        /// <summary>
        /// Danh sách thông báo
        /// </summary>
        ///
        [JsonPropertyName("messages")]
        List<string> Messages { get; set; }
    }

    /// <summary>
    /// Các thông tin chuẩn cấu trúc FTELCore
    /// </summary>
    ///
    public partial interface IResult
    {
        [JsonPropertyName("meta")]
        public ResultFTELCoreMetadataModel Meta { get; set; }

        [JsonPropertyName("error")]
        public ResultFTELCoreErrorModel Error { get; set; }
    }

    public sealed record ResultFTELCoreErrorModel
    {
        /// <summary>
        /// Mã lỗi nội bộ dạng string (ví dụ: SR_500, INVALID_OTP).
        /// </summary>
        ///
        [JsonPropertyName("code")]
        public string Code { get; set; }

        /// <summary>
        /// Cho biết client có thể retry không (true với 408/429/5xx).
        /// </summary>
        ///
        [JsonPropertyName("retryable")]
        public bool Retryable { get; set; } = false;
    }

    public sealed record ResultFTELCoreMetadataModel
    {
        /// <summary>
        /// ID định danh request (per-request, sinh bởi gateway / ASP.NET TraceIdentifier).
        /// </summary>
        ///
        [JsonPropertyName("request_id")]
        public string Request_Id { get; set; }

        /// <summary>
        /// ID trace cross-system (W3C Activity.TraceId hoặc fallback x-correlation-id).
        /// </summary>
        ///
        [JsonPropertyName("trace_id")]
        public string Trace_Id { get; set; }

        /// <summary>
        /// ISO 8601 UTC timestamp lúc tạo response (ví dụ: 2026-05-09T10:15:30.123Z).
        /// </summary>
        ///
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
    }
}