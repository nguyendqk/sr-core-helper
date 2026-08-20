using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace FTELSRCore.Data.SQL.Helpers
{
    /// <summary>
    /// EF Core connection interceptor bắt buộc mọi kết nối (thường gắn vào
    /// <see cref="FTELSRCore.Data.SQL.DbContexts.Read.ReadDbContext{TContext}"/>) chạy ở mức cách ly
    /// READ UNCOMMITTED ngay sau khi mở, nhằm cho phép đọc dữ liệu chưa commit (dirty read) để tăng
    /// thông lượng truy vấn đọc, đánh đổi lấy tính nhất quán dữ liệu.
    /// </summary>
    public sealed class ReadUncommittedConnectionInterceptor : DbConnectionInterceptor
    {
        /// <summary>
        /// Được EF Core gọi ngay sau khi connection vừa mở; thực thi lệnh
        /// <c>SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED</c> trên connection đó.
        /// </summary>
        /// <param name="connection">Connection vừa được mở.</param>
        /// <param name="eventData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public override async Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            await using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = " SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED ";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
