namespace FTELSRCore.Models.HealthChecks
{
    public record HealthCheckModel
    {
        public string Status { get; set; }
        public TimeSpan HealthCheckDuration { get; set; }

        public IEnumerable<IndividualHealthCheckResponse> HealthChecks { get; set; } = [];
    }

    public record IndividualHealthCheckResponse
    {
        public string Status { get; set; }

        public string Components { get; set; }

        public string Description { get; set; }

        public string Data { get; set; }
    }
}