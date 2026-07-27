namespace FTELSRCore.Wrappers.ErrorCodes.Catalogs.Systems
{
    public static class CatalogsErrorCodes
    {
        // =========================
        // GENERAL
        // =========================

        public static readonly CatalogsErrorCodeModel BadRequest =
            new(
                "BUSINESS_RULE_400",
                "Yêu cầu không hợp lệ",
                "Invalid request payload or missing fields"
            );

        public static readonly CatalogsErrorCodeModel RequestTimeout =
            new(
                "REQ_TIMEOUT",
                "Yêu cầu xử lý quá lâu",
                "Request timeout exceeded",
                true
            );

        public static readonly CatalogsErrorCodeModel RateLimit =
            new(
                "RATE_429",
                "Gửi quá nhiều yêu cầu được gủi đến",
                "Rate limit exceeded",
                true
            );

        public static readonly CatalogsErrorCodeModel SystemError =
            new(
                "SYS_500",
                "Lỗi hệ thống",
                "Unhandled internal server exception"
            );

        // =========================
        // AUTHENTICATION
        // =========================

        public static readonly CatalogsErrorCodeModel Unauthorized =
            new(
                "AUTH_401",
                "Chưa xác thực hoặc thiếu token",
                "Missing or invalid authentication token"
            );

        public static readonly CatalogsErrorCodeModel Forbidden =
            new(
                "AUTH_403",
                "Không có quyền truy cập",
                "Permission denied"
            );

        public static readonly CatalogsErrorCodeModel UpgradeRequired =
            new(
                "UPGRADE_REQUIRED",
                "Cần nâng cấp phiên bản client",
                "Unsupported client version"
            );

        // =========================
        // DATABASE
        // =========================

        public static readonly CatalogsErrorCodeModel DatabaseError =
            new(
                "DB_500",
                "Lỗi kết nối hoặc xử lý database",
                "Database execution failure",
                true
            );

        public static readonly CatalogsErrorCodeModel DatabaseUnavailable =
            new(
                "DB_503",
                "Database quá tải hoặc không sẵn sàng",
                "Database unavailable or overloaded",
                true
            );

        // =========================
        // NETWORK
        // =========================

        public static readonly CatalogsErrorCodeModel NetworkError =
            new(
                "NET_502",
                "Lỗi kết nối mạng nội bộ",
                "Internal network communication failure",
                true
            );

        // =========================
        // EXTERNAL SERVICE
        // =========================

        public static readonly CatalogsErrorCodeModel ExternalTimeout =
            new(
                "EXT_504",
                "Timeout khi gọi hệ thống bên ngoài",
                "External dependency timeout",
                true
            );
    }
}