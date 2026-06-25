using Microsoft.Data.SqlClient;
using System.Data;

namespace FTELSRCore.Data.SQL.Dapper.Helpers
{
    public static class ConfigurationHelpers
    {
        public static IDbConnection CreateConnection(string connection)
        {
            return new SqlConnection(connection);
        }
    }
}