namespace FTELSRCore.Models.Https
{
    public record AuthModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string PolicyName { get; set; }
        public string EmployeeCode { get; set; }
    }
}