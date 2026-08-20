using Microsoft.Data.SqlClient;

namespace FTELSRCore.Data.SQL.Dapper.Helpers
{
    /// <summary>
    /// Lớp tiện ích tạo kết nối SQL Server dùng chung cho các thao tác Dapper trong tầng SQL.
    /// </summary>
    public static class ConfigurationHelpers
    {
        /// <summary>
        /// Tạo mới một <see cref="SqlConnection"/> từ chuỗi kết nối truyền vào. Không tự mở connection
        /// (caller chịu trách nhiệm mở và giải phóng, thường thông qua <c>await using</c>).
        /// </summary>
        /// <param name="connection">Chuỗi kết nối SQL Server.</param>
        /// <returns>Đối tượng <see cref="SqlConnection"/> chưa mở, khởi tạo với chuỗi kết nối truyền vào.</returns>
        public static SqlConnection CreateConnection(string connection)
        {
            return new SqlConnection(connection);
        }
    }
}