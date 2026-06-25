namespace FTELSRCore.Constants
{
    public static class CommonBaseConstant
    {
        /// <summary>
        ///  Định danh hệ thống SR
        /// </summary>
        ///
        public const string Prefix = "SR";

        /// <summary>
        /// Định danh nhóm quyền của hệ hống SR
        /// </summary>
        ///
        public const string TypePermissions = "FTEL.SR";

        /// <summary>
        /// Giá trị mặc định
        /// </summary>
        ///
        public const string None = "N/A";

        /// <summary>
        /// Giá trị mặc định của APPCode
        /// </summary>
        ///
        public const string APPCode = $"FTEL.{Prefix}";

        public const string AnonymousCode = "0";

        public const string OrganizationForISC = "FTEL";

        public const string Anonymous = "Anonymous";

        public const string System = "FTEL-SERVICEREQUEST-API";

        public const string UserAgentCore = "FTELSRCore";

        public const string CORSDefault = "CorsPolicy";

        /// <summary>
        /// Ngày mặc định hệ thống.
        /// </summary>
        /// <param name="addHour">7</param>
        /// <returns></returns>
        ///
        public static DateTime DateTimeUtc(int addHour = 7)
        {
            return TimeProvider.System.GetUtcNow().DateTime.AddHours(addHour);
        }

        /// <summary>
        /// Logger cho Console.
        /// </summary>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="exception"></param>
        /// <param name="description"></param>
        ///
        public static void ConfigLoggerExceptionByConsole(
            string className, string methodName, Exception exception, string description = "")
        {
            Console.WriteLine($"{Environment.NewLine}[ERR] [LOGCONSOLE] [{DateTimeUtc():dd-MM-yyyy HH:mm:ss.fff}] [{className}] [{methodName}] -- note : {description} {Environment.NewLine}-- Message: {exception.Message?.Trim()} {Environment.NewLine}-- StackTrace: {exception.StackTrace?.Trim()} {Environment.NewLine} ");
        }
    }
}