namespace SRWebCoreAPI.Shared.Enum
{
    /// <summary>
    /// Enum định nghĩa trạng thái lịch sử tin nhắn.
    /// </summary>
    ///
    public enum StatusMessageHistories : byte
    {
        /// <summary>
        /// Tin nhắn đang chờ xử lý.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Gửi tin nhắn thành công.
        /// </summary>
        SendSuccess = 1,

        /// <summary>
        /// Gửi tin nhắn thất bại.
        /// </summary>
        SendFail = 2,

        /// <summary>
        /// Lỗi khi gửi tin nhắn.
        /// </summary>
        SendError = 3,

        /// <summary>
        /// Nhận tin nhắn thành công.
        /// </summary>
        ReceiveSuccess = 4,

        /// <summary>
        /// Nhận tin nhắn thất bại.
        /// </summary>
        ReceiveFail = 5,

        /// <summary>
        /// Lỗi khi nhận tin nhắn.
        /// </summary>
        ReceiveError = 6,

        /// <summary>
        /// Thử lại nhưng vẫn thất bại.
        /// </summary>
        RetryFaild = 99
    }
}