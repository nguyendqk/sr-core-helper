namespace FTELSRCore.Enum
{
    public enum RoleSR
    {
        /// <summary>
        /// Có quyền xem và xử lý tất cả các thông tin.
        /// </summary>
        ALL = 1,

        /// <summary>
        /// Được xem và xử lý các thông tin của bản thân, nhân sự được nhận quản lý và các thông tin thuộc địa bàn kiêm nhiệm.
        /// </summary>
        MY_DIVISION,

        /// <summary>
        /// Được xem và xử lý các thông tin của bản thân, nhân sự được nhận quản lý.
        /// </summary>
        EMPLOYEE_MANAGER,

        /// <summary>
        /// Được xem và xử lý các thông tin của bản thân, nhân sự được nhận quản lý .
        /// [ BỔ SUNG: Có quyền duyệt các yêu cầu Miễn Giảm, CHS6T, Duyệt CLG trên toàn quốc ]
        /// </summary>
        MASTER_TICKET,

        /// <summary>
        /// Được xem và xử lý các thông tin của bản thân tạo và được phân công.
        /// </summary>
        ASSIGNMENT,

        /// <summary>
        /// Được xem và xử lý các thông tin của bản thân tạo.
        /// </summary>
        ONLY_CREATE,

        /// <summary>
        /// Chỉ được xử lý lịch trực và nhân sự và các thông tin do bản thân tạo.
        /// </summary>
        ADMIN
    }
}