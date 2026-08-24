using FTELSRCore.Wrappers.ErrorCodes.Catalogs.Systems;
using System.Net;

namespace FTELSRCore.Wrappers.ErrorCodes.Catalogs
{
    public static class CatalogsErrorCode
    {
        public static readonly IReadOnlyDictionary<
            (int StatusCode, ErrorSourceType Source),
            ResultFTelCoreErrorModel> StatusMap =
                new Dictionary<(int, ErrorSourceType), ResultFTelCoreErrorModel>
                {
                    [(400, ErrorSourceType.General)] =
                        CatalogsErrorCodes.BadRequest,

                    [(422, ErrorSourceType.General)] =
                        CatalogsErrorCodes.UnprocessableEntity,

                    [(401, ErrorSourceType.Authentication)] =
                        CatalogsErrorCodes.Unauthorized,

                    [(403, ErrorSourceType.Authentication)] =
                        CatalogsErrorCodes.Forbidden,

                    [(408, ErrorSourceType.General)] =
                        CatalogsErrorCodes.RequestTimeout,

                    [(426, ErrorSourceType.Authentication)] =
                        CatalogsErrorCodes.UpgradeRequired,

                    [(429, ErrorSourceType.General)] =
                        CatalogsErrorCodes.RateLimit,

                    [(500, ErrorSourceType.General)] =
                        CatalogsErrorCodes.SystemError,

                    [(500, ErrorSourceType.Database)] =
                        CatalogsErrorCodes.DatabaseError,

                    [(502, ErrorSourceType.Network)] =
                        CatalogsErrorCodes.NetworkError,

                    [(503, ErrorSourceType.Database)] =
                        CatalogsErrorCodes.DatabaseUnavailable,

                    [(504, ErrorSourceType.ExternalService)] =
                        CatalogsErrorCodes.ExternalTimeout,

                    [((int)HttpStatusCode.NotFound, ErrorSourceType.General)] =
                        CatalogsErrorCodes.NotFound
                };
    }

    public enum ErrorSourceType
    {
        General,
        Authentication,
        Database,
        Cache,
        MessageQueue,
        ExternalService,
        Network,
        Storage
    }
}
