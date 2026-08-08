namespace FTELSRCore.Constants
{

    public static class OpenTelemetryConstant
    {
        public const string CoreCacheActivitySource = "FTELSRCore.Caches.CoreCacheExtension";

        public const string LoggingBehaviorActivitySource = "FTELSRCore.CQRS.Behaviors.LoggingBehavior";

        public const string SqlResilienceActivitySource = "FTELSRCore.Data.SQL.Helpers.Policies.SqlResiliencePolicyFactory";

        public const string MongoResilienceActivitySource = "FTELSRCore.Data.MongoDB.Helpers.Policies.MongoResiliencePolicyFactory";

        public const string HttpResilienceActivitySource = "FTELSRCore.Utilizes.Policies.HttpResiliencePolicyFactory";
    }
}
