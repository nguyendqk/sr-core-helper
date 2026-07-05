namespace FTELSRCore.Extensions.Loggers.Helpers
{
    public static class LoggerErrorCategoriesHelper
    {
        #region :::::::::::::::::  5.3  Business / Logic :::::::::::::::::

        /// <summary>
        /// Các lỗi liên quan đến nghiệp vụ và logic xử lý.
        /// </summary>
        public static class BusinessCategory
        {
            /// <summary>Lỗi logic nghiệp vụ không xử lý được.</summary>
            public const string BIZ_LOGIC = "BIZ_LOGIC";

            /// <summary>Dữ liệu đầu vào không hợp lệ (schema, format, required field).</summary>
            public const string BIZ_VALIDATION = "BIZ_VALIDATION";

            /// <summary>Không tìm thấy dữ liệu nghiệp vụ theo ID/key.</summary>
            public const string BIZ_NOT_FOUND = "BIZ_NOT_FOUND";

            /// <summary>Trùng lặp dữ liệu khi insert hoặc tạo mới.</summary>
            public const string BIZ_DUPLICATE = "BIZ_DUPLICATE";
        }

        #endregion :::::::::::::::::  5.3  Business / Logic :::::::::::::::::

        #region :::::::::::::::::  5.5  Security :::::::::::::::::

        /// <summary>
        /// Các lỗi liên quan đến bảo mật và xác thực.
        /// </summary>
        public static class SecurityCategory
        {
            /// <summary>Token không hợp lệ, hết hạn, hoặc thiếu.</summary>
            public const string SEC_UNAUTHORIZED = "SEC_UNAUTHORIZED";

            /// <summary>Token hợp lệ nhưng không có quyền truy cập resource.</summary>
            public const string SEC_FORBIDDEN = "SEC_FORBIDDEN";

            /// <summary>Phát hiện SQL injection, NoSQL injection, XSS attempt.</summary>
            public const string SEC_INJECTION = "SEC_INJECTION";

            /// <summary>Đăng nhập sai quá nhiều lần từ cùng IP/user.</summary>
            public const string SEC_BRUTE_FORCE = "SEC_BRUTE_FORCE";
        }

        #endregion :::::::::::::::::  5.5  Security :::::::::::::::::

        // ─────────────────────────────────────────────────────────────
        //  5.4  System / Resource
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Các lỗi liên quan đến tài nguyên hệ thống.
        /// </summary>
        public static class SystemCategory
        {
            /// <summary>OOM, memory leak, heap overflow, GC pressure.</summary>
            public const string SYS_MEMORY = "SYS_MEMORY";

            /// <summary>CPU spike, thread deadlock, thread starvation.</summary>
            public const string SYS_CPU = "SYS_CPU";

            /// <summary>Disk full, I/O error, write permission denied.</summary>
            public const string SYS_DISK = "SYS_DISK";

            /// <summary>Mất kết nối mạng nội bộ, DNS resolution fail.</summary>
            public const string SYS_NETWORK = "SYS_NETWORK";
        }

        #region :::::::::::::::::  5.2  API / HTTP :::::::::::::::::

        /// <summary>
        /// Các lỗi liên quan đến giao tiếp HTTP / API.
        /// </summary>
        public static class ApiCategory
        {
            /// <summary>Request vượt quá thời gian chờ (connect hoặc read timeout).</summary>
            public const string API_TIMEOUT = "API_TIMEOUT";

            /// <summary>Lỗi phía client — 400, 401, 403, 404, 422...</summary>
            public const string API_4XX = "API_4XX";

            /// <summary>Lỗi phía server trả về — 500, 502, 503, 504.</summary>
            public const string API_5XX = "API_5XX";

            /// <summary>Circuit breaker mở, ngưng gọi service downstream.</summary>
            public const string API_CIRCUIT_BREAKER = "API_CIRCUIT_BREAKER";

            /// <summary>Vượt giới hạn rate limit của API gateway hoặc service.</summary>
            public const string API_RATE_LIMIT = "API_RATE_LIMIT";

            // ── Resolve từ HTTP status code ──────────────────────────

            /// <summary>
            /// Xác định <c>errorCategory</c> từ HTTP status code.
            /// <para>
            /// Mapping:<br/>
            /// • 408, 504 (gateway timeout)         → <see cref="API_TIMEOUT"/><br/>
            /// • 429 (too many requests)             → <see cref="API_RATE_LIMIT"/><br/>
            /// • 503 (service unavailable)           → <see cref="API_CIRCUIT_BREAKER"/><br/>
            /// • 4xx (bao gồm 401, 403)              → <see cref="API_4XX"/><br/>
            /// • 5xx                                 → <see cref="API_5XX"/><br/>
            /// • Ngoài range 4xx–5xx                 → <see langword="null"/>
            /// </para>
            /// </summary>
            /// <param name="statusCode">HTTP status code (ví dụ: 200, 404, 500).</param>
            /// <returns>
            /// Chuỗi category chuẩn, hoặc <see langword="null"/> nếu status code
            /// không thuộc nhóm lỗi (1xx, 2xx, 3xx).
            /// </returns>
            ///
            public static string ResolveCategory(int statusCode) => statusCode switch
            {
                // Timeout: 408 Request Timeout, 504 Gateway Timeout
                408 or 504 => API_TIMEOUT,

                // Rate limit: 429 Too Many Requests
                429 => API_RATE_LIMIT,

                // Circuit breaker: 503 Service Unavailable
                503 => API_CIRCUIT_BREAKER,

                // Client errors 4xx (kể cả 401, 403)
                >= 400 and <= 499 => API_4XX,

                // Server errors 5xx (trừ 503, 504 đã xử lý trên)
                >= 500 and <= 599 => API_5XX,

                // 1xx, 2xx, 3xx — không phải lỗi
                _ => string.Empty
            };
        }

        #endregion :::::::::::::::::  5.2  API / HTTP :::::::::::::::::

        #region :::::::::::::::::   5.1  Infrastructure / Connection :::::::::::::::::

        /// <summary>
        /// Các lỗi liên quan đến hạ tầng kết nối: database, message queue.
        /// </summary>
        public static class InfrastructureCategory
        {
            /// <summary>Lỗi kết nối, query, timeout SQL Server.</summary>
            public const string DB_SQLSERVER = "DB_SQLSERVER";

            /// <summary>Lỗi kết nối, query, timeout MongoDB.</summary>
            public const string DB_MONGODB = "DB_MONGODB";

            /// <summary>Lỗi kết nối, cache miss bất thường, Redis timeout.</summary>
            public const string DB_REDIS = "DB_REDIS";

            /// <summary>Lỗi kết nối, query Elasticsearch.</summary>
            public const string DB_ELASTICSEARCH = "DB_ELASTICSEARCH";

            /// <summary>Lỗi produce, consume, consumer lag Kafka.</summary>
            public const string MQ_KAFKA = "MQ_KAFKA";

            /// <summary>Lỗi kết nối, publish, subscribe RabbitMQ.</summary>
            public const string MQ_RABBITMQ = "MQ_RABBITMQ";
        }

        #endregion :::::::::::::::::   5.1  Infrastructure / Connection :::::::::::::::::
    }
}