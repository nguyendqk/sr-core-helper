using System.Text.Json.Serialization;

namespace FTELSRCore.Models.Https.ResponseModel
{
    public class ProductPartnerResponseModel<TData> where TData : notnull
    {
        /// <summary>
        /// true: Thành công; false: Thất bại
        /// </summary>
        ///
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Thông báo lỗi hoặc thành công
        /// </summary>
        ///
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Dữ liệu trả về
        /// </summary>
        ///
        [JsonPropertyName("data")]
        public TData Data { get; set; }

        /// <summary>
        /// Danh sách lỗi dữ liệu
        /// </summary>
        ///
        [JsonPropertyName("dataError")]
        public List<string> DataError { get; set; } = [];

        /// <summary>
        /// Thông báo lỗi ngoại lệ
        /// </summary>
        ///
        [JsonPropertyName("exceptionMessage")]
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// Mã trace request từ client
        /// </summary>
        ///
        [JsonPropertyName("clientRequestId")]
        public string ClientRequestId { get; set; }
    }
}
