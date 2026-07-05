using FTELSRCore.Wrappers.ErrorCodes.Catalogs;
using System.Net;

namespace FTELSRCore.Wrappers.ErrorCodes
{
    public class ResponseWrapperByCodeMapper
    {
        public static CatalogsErorrCodeModel FromStatusCode(
             HttpStatusCode statusCode, ErrorSourceType sourceType = ErrorSourceType.General)
        {
            return CatalogsErorrCode.StatusMap.TryGetValue(
                ((int)statusCode, sourceType), out CatalogsErorrCodeModel errorCode)
                ? errorCode : FromStatusCodeDefault(statusCode: statusCode);
        }

        private static CatalogsErorrCodeModel FromStatusCodeDefault(HttpStatusCode statusCode)
        {
            HttpStatusCode? statusCodeConvertEnum =
                ConvertHelpers.ConvertEnum<HttpStatusCode>(statusCode.ToString());

            return new CatalogsErorrCodeModel
            (
                Code: $"SYS_{statusCodeConvertEnum}",
                Description: nameof(statusCodeConvertEnum),
                Message: nameof(statusCodeConvertEnum),
                Retryable: false
            );
        }
    }
}