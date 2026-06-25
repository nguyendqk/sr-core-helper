namespace FTELSRCore.Audits
{
    public interface IUserAudit
    {
        /// <summary>
        /// Thông tin chinh nhánh.
        /// </summary>
        ///
        int? BranchId { get; }

        /// <summary>
        /// Thông tin vùng.
        /// </summary>
        ///
        int? LocationId { get; }

        /// <summary>
        /// Thông tin email.
        /// </summary>
        ///
        string EmployeeEmail { get; }

        /// <summary>
        /// Thông tin mã nhân viên.
        /// </summary>
        ///
        string EmployeeCode { get; }

        /// <summary>
        /// Thông tin tên user của nhân viên.
        /// </summary>
        ///
        string EmployeeUserName { get; }

        /// <summary>
        /// Thông tin đơn vị của nhân viên.
        /// </summary>
        ///
        string Organization { get; }

        /// <summary>
        /// Thông tin vai trò của nhân viên.
        /// </summary>
        ///
        string TitleCode { get; }

        /// <summary>
        /// Danh sách các role SR của nhân viên.
        /// </summary>
        ///
        List<string> RolesSR { get; }

        /// <summary>
        /// Danh sách các role FTEL của nhân viên.
        /// </summary>
        ///
        List<string> RolesFTel { get; }

        /// <summary>
        /// Thông tin quyền của nhân viên.
        /// </summary>
        ///
        List<string> Permissions { get; }

        /// <summary>
        /// Thông tin địa bàn kiêm nhiệm của nhân viên.
        /// </summary>
        ///
        List<GetAllConcurrentAreaEmployeeModel> ConcurrentAreas { get; }

        /// <summary>
        /// Lấy thông tin role data của nhân viên.
        /// </summary>
        ///
        RoleSR RoleData { get; }

        /// <summary>
        /// Lấy thông tin Audit theo token.
        /// </summary>
        /// <param name="defaultName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<AuditModel> GetAuditCurrentAsync(
            string defaultName = "", CancellationToken cancellationToken = default);
    }
}