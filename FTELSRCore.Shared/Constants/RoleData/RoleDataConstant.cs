namespace FTELSRCore.Constants.RoleData
{
    /// <summary>
    /// Độ ưu tiên của từng loại role.
    /// </summary>
    ///
    public static class RoleDataConstant
    {
        private static readonly List<(int Order, RoleSR RoleData, List<string> RolesSR)> ComplexityForRolesSR =
        [
            // Có thể xem được hết và xử lý hết thông tin tại SR.
            new( (int)RoleSR.ALL,
                 RoleSR.ALL,
                 [RoleSRConstant.EMPLOYEE_ADMIN, RoleSRConstant.HANDLER_MASTER]),

            // NV Giám sát được xem tất cả các ticket thuộc địa bàn được phân quyền
            new( (int)RoleSR.MY_DIVISION,
                 RoleSR.MY_DIVISION,
                 [RoleSRConstant.SUPERVISOR]),

            // Có quyền nhận và duyệt các ticket Duyệt miễn giảm, CHS <6T, Duyệt CLG trên toàn quốc, được xử lý cho các SR, Ticket thuộc nhân viên mình quản lý
            new( (int)RoleSR.MASTER_TICKET,
                 RoleSR.MASTER_TICKET,
                 [RoleSRConstant.TICKET_MASTER]),

            // CBQL NV Xử lý được xem, xử lý cho các request, ticket thuộc nhân viên mình quản lý
            new( (int)RoleSR.EMPLOYEE_MANAGER,
                 RoleSR.EMPLOYEE_MANAGER,
                 [RoleSRConstant.HANDLER_MANAGER]),

            // NV Xử lý chỉ xem các request và các ticket của SR phân công mình xử lý
            new( (int)RoleSR.ASSIGNMENT,
                 RoleSR.ASSIGNMENT,
                 [RoleSRConstant.HANDLER, RoleSRConstant.REQUESTOR]),

            // Nhân sự quản lý quy trình và quản lý SR.
            new( (int)RoleSR.ADMIN,
                 RoleSR.ADMIN,
                 [RoleSRConstant.ADMIN])
        ];

        /// <summary>
        /// Lấy ra được role data của người dùng.
        /// </summary>
        /// <param name="roles"></param>
        /// <returns></returns>
        ///
        public static RoleSR GetRoleData(List<string> roles)
        {
            if (roles is null || roles.Count is 0)
            {
                return RoleSR.ONLY_CREATE;
            }

            RoleSR result =
                ComplexityForRolesSR.Where(x => x.RolesSR.Exists(y => roles.Contains(y))).OrderBy(x => x.Order).FirstOrDefault().RoleData;

            return result is 0 ? RoleSR.ONLY_CREATE : result;
        }
    }
}