using Dapper;
using FTELSRCore.Data.SQL.Dapper.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FTELSRCore.Data.SQL.Dapper
{
    public sealed class DapperSQLDBContext(string connectionString) : IDapperSQLDBContext
    {
        private readonly string _dbConnection = connectionString;

        public async Task<bool> ExecuteNonQueryAsync(string pSqlQuery,
                                           DynamicParameters pParams,
                                           int commandTimeout = 30,
                                           CommandType commandType = CommandType.Text,
                                           CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery))
            {
                return false;
            }

            await using SqlConnection connection =
                ConfigurationHelpers.CreateConnection(_dbConnection);

            return await connection.ExecuteAsync(
                new CommandDefinition(
                    commandText: pSqlQuery,
                    parameters: pParams,
                    commandType: commandType,
                    commandTimeout: commandTimeout,
                    cancellationToken: cancellationToken)).ConfigureAwait(false) > 0;
        }

        public async Task<T> GetOne<T>(string pSqlQuery,
                                 DynamicParameters pParams,
                                 int commandTimeout = 30,
                                 CommandType commandType = CommandType.Text,
                                 CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery))
            {
                return default;
            }

            await using SqlConnection connection =
                ConfigurationHelpers.CreateConnection(_dbConnection);

            return await connection.QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(
                    commandText: pSqlQuery,
                    parameters: pParams,
                    commandType: commandType,
                    commandTimeout: commandTimeout,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        public async Task<IEnumerable<T>> GetAll<T>(string pSqlQuery,
                                                    DynamicParameters pParams,
                                                    int commandTimeout = 30,
                                                    CommandType commandType = CommandType.Text,
                                                    CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery))
            {
                return default;
            }

            await using SqlConnection connection =
                ConfigurationHelpers.CreateConnection(_dbConnection);

            return await connection.QueryAsync<T>(
                new CommandDefinition(
                    commandText: pSqlQuery,
                    parameters: pParams,
                    commandType: commandType,
                    commandTimeout: commandTimeout,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        public async Task<T> GetOneExecute<T>(string pSqlQuery,
                                              DynamicParameters pParams,
                                              int commandTimeout = 30,
                                              CommandType commandType = CommandType.Text,
                                              CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery))
            {
                return default;
            }

            await using SqlConnection connection =
                ConfigurationHelpers.CreateConnection(_dbConnection);

            T result = await connection.ExecuteScalarAsync<T>(
                new CommandDefinition(
                    commandText: pSqlQuery,
                    parameters: pParams,
                    commandType: commandType,
                    commandTimeout: commandTimeout,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return result;
        }

        public async Task<IEnumerable<T>> GetAllExecuteAsync<T>(string pSqlQuery,
                                                                DynamicParameters pParams,
                                                                int commandTimeout = 30,
                                                                CommandType commandType = CommandType.Text,
                                                                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery))
            {
                return null;
            }

            await using SqlConnection connection =
                ConfigurationHelpers.CreateConnection(_dbConnection);

            IEnumerable<T> result = await connection.QueryAsync<T>(
                new CommandDefinition(
                    commandText: pSqlQuery,
                    parameters: pParams,
                    commandType: commandType,
                    commandTimeout: commandTimeout,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return result;
        }
    }
}