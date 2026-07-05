using Microsoft.Data.SqlClient;

namespace FTELSRCore.Data.SQL.Dapper.Helpers
{
    public static class ConfigurationHelpers
    {
        public static SqlConnection CreateConnection(string connection)
        {
            return new SqlConnection(connection);
        }
    }
}