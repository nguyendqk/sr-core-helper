using Dapper;
using FTELSRCore.Data.SQL.Dapper.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FTELSRCore.Data.SQL.Dapper
{
    public abstract class ExecuteSQLContext<TClass>(string connectionString) where TClass : class
    {
        protected abstract string StoreName { get; }

        protected abstract byte TypeConnection { get; }

        protected abstract DynamicParameters GetDynamicParameters(TClass entry);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="pParams"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public async Task<IEnumerable<TResult>> Execute<TResult>(
            TClass pParams, int commandTimeout = 30, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using SqlConnection connection =
                ConfigurationHelpers.CreateConnection(connectionString);

            DynamicParameters parameters = GetDynamicParameters(pParams);

            return await connection.QueryAsync<TResult>(
                    new CommandDefinition(
                        commandText: StoreName,
                        parameters: parameters,
                        commandTimeout: commandTimeout,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        public async Task<TResult> ExecuteScalar<TResult>(
            TClass pParams, int commandTimeout = 30, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using SqlConnection connection =
                ConfigurationHelpers.CreateConnection(connectionString);

            DynamicParameters parameters = GetDynamicParameters(pParams);

            IEnumerable<TResult> data =
                await connection.QueryAsync<TResult>(
                    new CommandDefinition(
                        commandText: StoreName,
                        parameters: parameters,
                        commandTimeout: commandTimeout,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

            TResult result = parameters.Get<TResult>("P_RESULT");

            return result is null ? data.FirstOrDefault() : result;
        }
    }
}