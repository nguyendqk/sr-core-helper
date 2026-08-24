using FTELSRCore.Wrappers.ErrorCodes.Catalogs;
using System.Net;

namespace FTELSRCore.Wrappers.ErrorCodes
{
    public class ResponseWrapperByCodeMapper
    {
        public static ResultFTelCoreErrorModel FromStatusCode(
             HttpStatusCode statusCode, ErrorSourceType sourceType = ErrorSourceType.General)
        {
            return CatalogsErrorCode.StatusMap.TryGetValue(
                ((int)statusCode, sourceType), out ResultFTelCoreErrorModel errorCode)
                ? errorCode : FromStatusCodeDefault(statusCode: statusCode);
        }

        private static ResultFTelCoreErrorModel FromStatusCodeDefault(HttpStatusCode statusCode)
        {
            HttpStatusCode? statusCodeConvertEnum =
                ConvertHelpers.ConvertEnum<HttpStatusCode>(statusCode.ToString());

            return new ResultFTelCoreErrorModel
            {
                Retryable = false,
                Code = $"SYS_{statusCodeConvertEnum}",
                Message = statusCodeConvertEnum.ToString(),
                Details = [],
            };
        }
    }
}