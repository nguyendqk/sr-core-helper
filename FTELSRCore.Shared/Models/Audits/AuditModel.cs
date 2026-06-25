namespace FTELSRCore.Models.Audits
{
    public record AuditModel
    {
        public string Ip { get; set; }

        public string Device { get; set; }

        public string Method { get; set; }

        public string Address { get; set; }

        public CreatorInfo CreatorInfo { get; set; }
    }

    public record CreatorInfo
    {
        /// <summary>
        /// Mã định danh của người tạo.
        /// </summary>
        ///
        public string Code { get; set; }

        /// <summary>
        /// Tên người tạo.
        /// </summary>
        ///
        public string Name { get; set; }

        /// <summary>
        /// Email người tạo.
        /// </summary>
        ///
        public string Email { get; set; }

        /// <summary>
        /// Tên đơn vị/cơ quan của người tạo.
        /// </summary>
        ///
        public string Organization { get; set; }

        /// <summary>
        /// Vai trò chính của người tạo trong hệ thống SR.
        /// </summary>
        ///
        public RoleSR? Role { get; set; } = RoleSR.ONLY_CREATE;

        /// <summary>
        /// ID chi nhánh nơi người tạo làm việc.
        /// </summary>
        ///
        public int? RegionId { get; set; }

        /// <summary>
        /// ID chi nhánh nơi người tạo làm việc.
        /// </summary>
        ///
        public int? BranchId { get; set; }

        /// <summary>
        /// ID địa điểm cụ thể của người tạo.
        /// </summary>
        ///
        public int? LocationId { get; set; }

        /// <summary>
        /// Thông tin vai trò của nhân viên.
        /// </summary>
        ///
        public string TitleCode { get; set; }

        /// <summary>
        /// Danh sách vai trò trong hệ thống SR mà người tạo có.
        /// </summary>
        ///
        public List<string> RolesSR { get; set; } = [];

        /// <summary>
        /// Danh sách vai trò trong hệ thống FTel mà người tạo có.
        /// </summary>
        ///
        public List<string> RolesFTel { get; set; } = [];

        /// <summary>
        /// Danh sách khu vực làm việc đồng thời của người tạo.
        /// </summary>
        ///
        public List<GetAllConcurrentAreaEmployeeModel> ConcurrentAreas { get; set; } = [];
    }
}