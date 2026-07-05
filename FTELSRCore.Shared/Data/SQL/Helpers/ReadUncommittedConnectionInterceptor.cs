using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace FTELSRCore.Data.SQL.Helpers
{
    public sealed class ReadUncommittedConnectionInterceptor : DbConnectionInterceptor
    {
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
