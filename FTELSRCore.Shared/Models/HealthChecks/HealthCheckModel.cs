using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTELSRCore.Models.HealthChecks
{
    public record HealthCheckModel
    {
        public string Status { get; set; }

        public TimeSpan TotalDuration { get; set; }

        public IEnumerable<IndividualHealthCheckResponse> Checks { get; set; } = [];
    }

    public record IndividualHealthCheckResponse
    {
        public string Name { get; set; }

        public HealthStatus Status { get; set; }

        public TimeSpan Duration { get; set; }

        public Exception Exception { get; set; }
    }
}