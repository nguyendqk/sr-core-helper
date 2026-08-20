using Dapper;
using FTELSRCore.Data.SQL.Dapper.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FTELSRCore.Data.SQL.Dapper
{
    /// <summary>
    /// Triển khai <see cref="IDapperSQLDBContext"/> để thực thi SQL thô qua Dapper trên
    /// <see cref="SqlConnection"/>. Mỗi lệnh gọi tự mở một connection mới (qua <see cref="ConfigurationHelpers.CreateConnection(string)"/>)
    /// và giải phóng ngay sau khi thực thi xong (await using), không dùng chung connection giữa các lệnh gọi.
    /// </summary>
    /// <param name="connectionString">Chuỗi kết nối SQL Server dùng để mở connection cho mỗi lệnh gọi.</param>
    public sealed class DapperSQLDBContext(string connectionString) : IDapperSQLDBContext
    {
        private readonly string _dbConnection = connectionString;

        /// <summary>
        /// Thực thi câu lệnh SQL không trả về tập kết quả (Insert/Update/Delete) thông qua Dapper.
        /// </summary>
        /// <param name="pSqlQuery">Câu lệnh SQL cần thực thi; nếu rỗng/null sẽ trả về false ngay mà không mở connection.</param>
        /// <param name="pParams">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định CommandType.Text).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>true nếu số dòng bị ảnh hưởng lớn hơn 0; ngược lại false.</returns>
        ///
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

        /// <summary>
        /// Truy vấn và lấy về bản ghi đầu tiên (hoặc giá trị mặc định nếu không có) thông qua Dapper QueryFirstOrDefaultAsync.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của bản ghi cần lấy.</typeparam>
        /// <param name="pSqlQuery">Câu lệnh SQL cần thực thi; nếu rỗng/null sẽ trả về giá trị mặc định ngay mà không mở connection.</param>
        /// <param name="pParams">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định CommandType.Text).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bản ghi đầu tiên khớp điều kiện, hoặc giá trị mặc định của <typeparamref name="T"/> nếu không có.</returns>
        ///
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

        /// <summary>
        /// Truy vấn và lấy về danh sách bản ghi thông qua Dapper QueryAsync.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của từng bản ghi trong danh sách kết quả.</typeparam>
        /// <param name="pSqlQuery">Câu lệnh SQL cần thực thi; nếu rỗng/null sẽ trả về giá trị mặc định ngay mà không mở connection.</param>
        /// <param name="pParams">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định CommandType.Text).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách bản ghi kiểu <typeparamref name="T"/>.</returns>
        ///
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

        /// <summary>
        /// Thực thi câu lệnh SQL và lấy về một giá trị đơn (scalar) thông qua Dapper ExecuteScalarAsync,
        /// thường dùng cho các truy vấn trả về COUNT/SUM hoặc một cột duy nhất.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của giá trị cần lấy.</typeparam>
        /// <param name="pSqlQuery">Câu lệnh SQL cần thực thi; nếu rỗng/null sẽ trả về giá trị mặc định ngay mà không mở connection.</param>
        /// <param name="pParams">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định CommandType.Text).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Giá trị scalar kiểu <typeparamref name="T"/>.</returns>
        ///
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

        /// <summary>
        /// Thực thi câu lệnh SQL hoặc stored procedure và lấy về danh sách bản ghi thông qua Dapper QueryAsync.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của từng bản ghi trong danh sách kết quả.</typeparam>
        /// <param name="pSqlQuery">Câu lệnh SQL hoặc tên stored procedure cần thực thi; nếu rỗng/null sẽ trả về null ngay mà không mở connection.</param>
        /// <param name="pParams">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định CommandType.Text).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách bản ghi kiểu <typeparamref name="T"/>, hoặc null nếu câu lệnh rỗng.</returns>
        ///
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