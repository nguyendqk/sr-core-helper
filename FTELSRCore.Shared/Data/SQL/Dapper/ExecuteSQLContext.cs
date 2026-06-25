using Dapper;
using FTELSRCore.Data.SQL.Dapper.Helpers;
using System.Data;

namespace FTELSRCore.Data.SQL.Dapper
{
    public abstract class ExecuteSQLContext<TClass>(string connectionString) where TClass : class
    {
        protected abstract string StoreName { get; }

        protected abstract byte TypeConnection { get; }

        protected abstract DynamicParameters GetDynamicParameters(TClass entry);

        public Task<IEnumerable<TResult>> Execute<TResult>(TClass pParams, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using IDbConnection connection =
                ConfigurationHelpers.CreateConnection(connectionString);

            DynamicParameters parameters = GetDynamicParameters(pParams);

            IEnumerable<TResult> result =
                connection.Query<TResult>(
                    sql: StoreName,
                    param: parameters,
                    commandType: CommandType.StoredProcedure);

            return Task.FromResult(result);
        }

        public Task<TResult> ExecuteScalar<TResult>(TClass pParams, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using IDbConnection connection =
                ConfigurationHelpers.CreateConnection(connectionString);

            DynamicParameters parameters = GetDynamicParameters(pParams);

            IEnumerable<TResult> data =
                connection.Query<TResult>(
                    sql: StoreName,
                    param: parameters,
                    commandType: CommandType.StoredProcedure);

            TResult result = parameters.Get<TResult>("P_RESULT");

            return Task.FromResult(result is null ? data.FirstOrDefault() : result);
        }
    }
}