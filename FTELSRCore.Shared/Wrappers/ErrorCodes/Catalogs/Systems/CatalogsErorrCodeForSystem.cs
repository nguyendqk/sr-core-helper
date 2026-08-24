namespace FTELSRCore.Wrappers.ErrorCodes.Catalogs.Systems
{
    /// <summary>
    /// Danh mục các mã lỗi mặc định của hệ thống, tổ chức theo nhóm nguyên nhân (General, Authentication, Database, Network, External Service).
    /// </summary>
    ///
    public static class CatalogsErrorCodes
    {
        // =========================
        // GENERAL
        // =========================

        public static readonly ResultFTelCoreErrorModel BadRequest =
            new()
            {
                Code = "BUSINESS_RULE_400",
                Message = "Yêu cầu không hợp lệ",
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Dữ liệu yêu cầu không hợp lệ hoặc thiếu trường bắt buộc"]
                    }
                ]
            };

        public static readonly ResultFTelCoreErrorModel UnprocessableEntity =
            new()
            {
                Code = "VALIDATION_422",
                Message = "Dữ liệu không thể xử lý",
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Yêu cầu đúng định dạng nhưng dữ liệu không thỏa điều kiện nghiệp vụ"]
                    }
                ]
            };

        public static readonly ResultFTelCoreErrorModel RequestTimeout =
            new()
            {
                Code = "REQ_TIMEOUT",
                Message = "Yêu cầu xử lý quá lâu",
                Retryable = true,
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Yêu cầu xử lý vượt quá thời gian cho phép"]
                    }
                ]
            };

        public static readonly ResultFTelCoreErrorModel RateLimit =
            new()
            {
                Code = "RATE_429",
                Message = "Gửi quá nhiều yêu cầu được gủi đến",
                Retryable = true,
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Vượt quá giới hạn số lượng yêu cầu cho phép"]
                    }
                ]
            };

        public static readonly ResultFTelCoreErrorModel SystemError =
            new()
            {
                Code = "SYS_500",
                Message = "Ngoại lệ nội bộ hệ thống",
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Ngoại lệ nội bộ hệ thống vui lòng liên hệ ftel.isc.srsupport@fpt.com để được hỗ trợ"]
                    }
                ]
            };

        public static ResultFTelCoreErrorModel NotFound =>
            new()
            {
                Code = "NOT_FOUND",
                Message = "Không tìm thấy tài nguyên",
                Details =
                [
                    new DetailsFTelCoreErrorModel
            {
                Reason = ["Đường dẫn yêu cầu không tồn tại hoặc không được hỗ trợ"]
            }
                ]
            };

        // =========================
        // AUTHENTICATION
        // =========================

        public static readonly ResultFTelCoreErrorModel Unauthorized =
            new()
            {
                Code = "AUTH_401",
                Message = "Chưa xác thực hoặc thiếu token",
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Thiếu token xác thực hoặc token không hợp lệ"]
                    }
                ]
            };

        public static readonly ResultFTelCoreErrorModel Forbidden =
            new()
            {
                Code = "AUTH_403",
                Message = "Không có quyền truy cập",
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Không có quyền thực hiện thao tác này"]
                    }
                ]
            };

        public static readonly ResultFTelCoreErrorModel UpgradeRequired =
            new()
            {
                Code = "UPGRADE_REQUIRED",
                Message = "Cần nâng cấp phiên bản client",
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Phiên bản client hiện tại không được hỗ trợ"]
                    }
                ]
            };

        // =========================
        // DATABASE
        // =========================

        public static readonly ResultFTelCoreErrorModel DatabaseError =
            new()
            {
                Code = "DB_500",
                Message = "Lỗi kết nối hoặc xử lý database",
                Retryable = true,
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Lỗi thực thi truy vấn database"]
                    }
                ]
            };

        public static readonly ResultFTelCoreErrorModel DatabaseUnavailable =
            new()
            {
                Code = "DB_503",
                Message = "Database quá tải hoặc không sẵn sàng",
                Retryable = true,
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Database không sẵn sàng hoặc đang quá tải"]
                    }
                ]
            };

        // =========================
        // NETWORK
        // =========================

        public static readonly ResultFTelCoreErrorModel NetworkError =
            new()
            {
                Code = "NET_502",
                Message = "Lỗi kết nối mạng nội bộ",
                Retryable = true,
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Lỗi giao tiếp mạng nội bộ giữa các hệ thống"]
                    }
                ]
            };

        // =========================
        // EXTERNAL SERVICE
        // =========================

        public static readonly ResultFTelCoreErrorModel ExternalTimeout =
            new()
            {
                Code = "EXT_504",
                Message = "Timeout khi gọi hệ thống bên ngoài",
                Retryable = true,
                Details =
                [
                    new DetailsFTelCoreErrorModel
                    {
                        Reason = ["Hệ thống bên ngoài phản hồi quá thời gian cho phép"]
                    }
                ]
            };
    }
}
