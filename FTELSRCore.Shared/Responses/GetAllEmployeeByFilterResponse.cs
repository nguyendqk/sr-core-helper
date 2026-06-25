namespace FTELSRCore.Responses
{
    public record GetAllEmployeeByFilterResponse
    {
        public int? Id { get; set; }

        public string Code { get; set; }

        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        public string OrganizationCode { get; set; }
        public string OrganizationName { get; set; }

        public string PositionCode { get; set; }
        public string PositionName { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; }

        public int? EmployeeRank { get; set; }

        public DateTime? EmployeeBirthday { get; set; }

        public string EmployeePhone { get; set; }

        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }

        public int? Gender { get; set; }
        public string GenderName { get; set; }

        public string TitleCode { get; set; }
        public string TitleName { get; set; }

        public string Address { get; set; }
        public string Identification { get; set; }

        public string InsideUserName { get; set; }
        public long? InsideUserId { get; set; }

        public string InsideEmail { get; set; }

        public int? InsideBranchId { get; set; }
        public string InsideBranchName { get; set; }

        public int? InsideLocationId { get; set; }
        public string InsideLocationName { get; set; }

        public int? InsideRegionId { get; set; }
        public string InsideRegionName { get; set; }

        public string ManagerEmployee { get; set; }

        public int TotalRows { get; set; }

        public bool? Sex { get; set; }

        public DateTime? BirthDay { get; set; }

        public string Phone { get; set; }
    }
}