using Dapper;
using FTELSRCore.Data.SQL.Dapper.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FTELSRCore.Data.SQL.Dapper
{
    /// <summary>
    /// Lớp cơ sở trừu tượng để gọi stored procedure qua Dapper: mỗi lớp con định nghĩa tên stored procedure
    /// (<see cref="StoreName"/>) và cách ánh xạ đối tượng đầu vào <typeparamref name="TClass"/> sang
    /// <see cref="DynamicParameters"/>. Mỗi lệnh gọi tự mở và giải phóng một connection riêng.
    /// </summary>
    /// <typeparam name="TClass">Kiểu đối tượng chứa tham số đầu vào cho stored procedure.</typeparam>
    /// <param name="connectionString">Chuỗi kết nối SQL Server dùng để mở connection cho mỗi lệnh gọi.</param>
    public abstract class ExecuteSQLContext<TClass>(string connectionString) where TClass : class
    {
        protected abstract string StoreName { get; }

        protected abstract byte TypeConnection { get; }

        protected abstract DynamicParameters GetDynamicParameters(TClass entry);

        /// <summary>
        /// Gọi stored procedure <see cref="StoreName"/> với tham số ánh xạ từ <paramref name="pParams"/>
        /// và trả về toàn bộ tập kết quả qua Dapper QueryAsync.
        /// </summary>
        /// <typeparam name="TResult">Kiểu dữ liệu của từng bản ghi trong tập kết quả.</typeparam>
        /// <param name="pParams">Đối tượng chứa tham số đầu vào, được chuyển đổi qua <see cref="GetDynamicParameters(TClass)"/>.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định 30 giây).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách kết quả kiểu <typeparamref name="TResult"/>.</returns>
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

        /// <summary>
        /// Gọi stored procedure <see cref="StoreName"/> và lấy về một giá trị kết quả duy nhất: ưu tiên giá trị
        /// tham số output tên <c>P_RESULT</c> nếu stored procedure có khai báo, nếu không sẽ lấy dòng đầu tiên
        /// của tập kết quả trả về.
        /// </summary>
        /// <typeparam name="TResult">Kiểu dữ liệu của giá trị kết quả.</typeparam>
        /// <param name="pParams">Đối tượng chứa tham số đầu vào, được chuyển đổi qua <see cref="GetDynamicParameters(TClass)"/>.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định 30 giây).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Giá trị tham số output <c>P_RESULT</c> nếu có, ngược lại là bản ghi đầu tiên của tập kết quả (hoặc mặc định nếu rỗng).</returns>
        ///
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