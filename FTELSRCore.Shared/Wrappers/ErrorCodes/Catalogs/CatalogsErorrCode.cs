using FTELSRCore.Wrappers.ErrorCodes.Catalogs.Systems;

namespace FTELSRCore.Wrappers.ErrorCodes.Catalogs
{
    public static class CatalogsErorrCode
    {
        public static readonly IReadOnlyDictionary<
            (int StatusCode, ErrorSourceType Source),
            CatalogsErorrCodeModel> StatusMap =
                new Dictionary<
                    (int, ErrorSourceType),
                    CatalogsErorrCodeModel>
                {
                    [(400, ErrorSourceType.General)] =
                        CatalogsErorrCodes.BadRequest,

                    [(401, ErrorSourceType.Authentication)] =
                        CatalogsErorrCodes.Unauthorized,

                    [(403, ErrorSourceType.Authentication)] =
                        CatalogsErorrCodes.Forbidden,

                    [(408, ErrorSourceType.General)] =
                        CatalogsErorrCodes.RequestTimeout,

                    [(426, ErrorSourceType.Authentication)] =
                        CatalogsErorrCodes.UpgradeRequired,

                    [(429, ErrorSourceType.General)] =
                        CatalogsErorrCodes.RateLimit,

                    [(500, ErrorSourceType.General)] =
                        CatalogsErorrCodes.SystemError,

                    [(500, ErrorSourceType.Database)] =
                        CatalogsErorrCodes.DatabaseError,

                    [(502, ErrorSourceType.Network)] =
                        CatalogsErorrCodes.NetworkError,

                    [(503, ErrorSourceType.Database)] =
                        CatalogsErorrCodes.DatabaseUnavailable,

                    [(504, ErrorSourceType.ExternalService)] =
                        CatalogsErorrCodes.ExternalTimeout
                };
    }

    public sealed record CatalogsErorrCodeModel(
        string Code,
        string Message,
        string Description = null,
        bool Retryable = false
    );

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
