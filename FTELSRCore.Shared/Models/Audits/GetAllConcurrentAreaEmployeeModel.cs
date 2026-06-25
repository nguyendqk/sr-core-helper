namespace FTELSRCore.Models.Audits
{
    public record GetAllConcurrentAreaEmployeeModel
    {
        public int? BranchId { get; set; }
        public int? LocationId { get; set; }
    }
}