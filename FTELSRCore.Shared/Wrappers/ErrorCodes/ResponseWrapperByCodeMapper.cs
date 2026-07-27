using FTELSRCore.Wrappers.ErrorCodes.Catalogs;
using System.Net;

namespace FTELSRCore.Wrappers.ErrorCodes
{
    public class ResponseWrapperByCodeMapper
    {
        public static CatalogsErrorCodeModel FromStatusCode(
             HttpStatusCode statusCode, ErrorSourceType sourceType = ErrorSourceType.General)
        {
            return CatalogsErrorCode.StatusMap.TryGetValue(
                ((int)statusCode, sourceType), out CatalogsErrorCodeModel errorCode)
                ? errorCode : FromStatusCodeDefault(statusCode: statusCode);
        }

        private static CatalogsErrorCodeModel FromStatusCodeDefault(HttpStatusCode statusCode)
        {
            HttpStatusCode? statusCodeConvertEnum =
                ConvertHelpers.ConvertEnum<HttpStatusCode>(statusCode.ToString());

            return new CatalogsErrorCodeModel
            (
                Code: $"SYS_{statusCodeConvertEnum}",
                Description: statusCodeConvertEnum.ToString(),
                Message: statusCodeConvertEnum.ToString(),
                Retryable: false
            );
        }
    }
}