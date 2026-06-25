namespace FTELSRCore.Extensions
{
    public static class EnvironmentExtensions
    {
        public const string ELocal = "Local";

        public const string EDev = "Development";

        public const string EStag = "Staging";

        public const string EProd = "Production";

        /// <summary>
        /// Lấy thông tin môi trường của hệ thống.
        /// </summary>
        /// <returns></returns>
        ///
        public static string GetEnvironment() =>
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;

        /// <summary>
        /// Lấy thông tin Prefix cho từng môi trường
        /// </summary>
        /// <returns></returns>
        ///
        public static string GetPrefixEnvironment()
        {
            return GetEnvironment() switch
            {
                EStag => "stag-",
                EProd => string.Empty,
                _ => "dev-"
            };
        }
    }
}