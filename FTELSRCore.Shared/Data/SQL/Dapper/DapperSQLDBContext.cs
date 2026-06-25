using Dapper;
using FTELSRCore.Data.SQL.Dapper.Helpers;
using System.Data;

namespace FTELSRCore.Data.SQL.Dapper
{
    public sealed class DapperSQLDBContext(string connectionString) : IDapperSQLDBContext
    {
        private readonly string _dbConnection = connectionString;

        /// <summary>
        /// Thực thi một câu lệnh SQL để thực hiện các thao tác SAP (Save, Alter, Purge) trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="pSqlQuery">Câu lệnh SQL cần thực thi.</param>
        /// <param name="pParams">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định là 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định là CommandType.Text).</param>
        /// <param name="cancellationToken"></param>
        /// <returns> Trả về `true` nếu câu lệnh SQL thực thi thành công (có thay đổi dữ liệu), ngược lại trả về `false`. </returns>
        ///
        public async Task<bool> IsSAPAsync(string pSqlQuery,
                                           DynamicParameters pParams,
                                           int commandTimeout = 30,
                                           CommandType commandType = CommandType.Text,
                                           CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery)) return false;

            using IDbConnection connection = ConfigurationHelpers.CreateConnection(_dbConnection);

            int result = await connection.ExecuteAsync(sql: pSqlQuery,
                                                       param: pParams,
                                                       commandType: commandType,
                                                       commandTimeout: commandTimeout).ConfigureAwait(false);

            return result > 0;
        }

        /// <summary>
        /// Thực hiện truy vấn để lấy một bản ghi từ cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của bản ghi cần lấy.</typeparam>
        /// <param name="pSqlQuery">Câu lệnh SQL cần thực thi.</param>
        /// <param name="pParams">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định là 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định là CommandType.Text).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// Trả về một đối tượng kiểu <typeparamref name="T"/> nếu truy vấn thành công;
        /// nếu không tìm thấy dữ liệu hoặc xảy ra lỗi, trả về giá trị mặc định của <typeparamref name="T"/>.
        /// </returns>
        ///
        public Task<T> GetOne<T>(string pSqlQuery,
                                 DynamicParameters pParams,
                                 int commandTimeout = 30,
                                 CommandType commandType = CommandType.Text,
                                 CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery)) return Task.FromResult<T>(default);

            using IDbConnection connection = ConfigurationHelpers.CreateConnection(_dbConnection);

            T result = connection.QueryFirstOrDefault<T>(sql: pSqlQuery,
                                                         param: pParams,
                                                         commandType: commandType,
                                                         commandTimeout: commandTimeout);

            return Task.FromResult(result);
        }

        /// <summary>
        /// Thực hiện truy vấn để lấy danh sách các bản ghi từ cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của danh sách bản ghi cần lấy.</typeparam>
        /// <param name="pSqlQuery">Câu lệnh SQL cần thực thi.</param>
        /// <param name="pParams">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định là 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định là CommandType.Text).</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns> Trả về một danh sách các đối tượng kiểu <typeparamref name="T"/> nếu truy vấn thành công; nếu xảy ra lỗi, trả về `null`.
        /// </returns>
        ///
        public async Task<IEnumerable<T>> GetAll<T>(string pSqlQuery,
                                                    DynamicParameters pParams,
                                                    int commandTimeout = 30,
                                                    CommandType commandType = CommandType.Text,
                                                    CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery)) return null;

            using IDbConnection connection = ConfigurationHelpers.CreateConnection(_dbConnection);

            IEnumerable<T> result = await connection.QueryAsync<T>(sql: pSqlQuery,
                                                                   param: pParams,
                                                                   commandType: commandType,
                                                                   commandTimeout: commandTimeout).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Thực hiện truy vấn để lấy một giá trị đơn từ cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của giá trị cần lấy.</typeparam>
        /// <param name="pSqlQuery">Câu lệnh SQL cần thực thi.</param>
        /// <param name="pParams">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định là 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định là CommandType.Text).</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns> Trả về một giá trị kiểu <typeparamref name="T"/> nếu truy vấn thành công; nếu xảy ra lỗi hoặc không có giá trị trả về, trả về giá trị mặc định của <typeparamref name="T"/>.
        /// </returns>
        ///
        public async Task<T> GetOneExecute<T>(string pSqlQuery,
                                              DynamicParameters pParams,
                                              int commandTimeout = 30,
                                              CommandType commandType = CommandType.Text,
                                              CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery)) return default;

            using IDbConnection connection = ConfigurationHelpers.CreateConnection(_dbConnection);

            T result = await connection.ExecuteScalarAsync<T>(sql: pSqlQuery,
                                                              param: pParams,
                                                              commandType: commandType,
                                                              commandTimeout: commandTimeout).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Thực thi câu truy vấn SQL hoặc stored procedure và trả về danh sách các bản ghi kiểu <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của từng dòng kết quả trả về.</typeparam>
        /// <param name="pSqlQuery">Chuỗi truy vấn SQL hoặc tên stored procedure cần thực thi.</param>
        /// <param name="pParams">Các tham số truyền vào truy vấn, sử dụng <see cref="DynamicParameters"/> để tránh SQL injection.</param>
        /// <param name="commandTimeout">Thời gian tối đa (giây) cho phép truy vấn thực thi trước khi bị timeout. Mặc định là 30 giây.</param>
        /// <param name="commandType">Loại truy vấn: <see cref="CommandType.Text"/> cho câu lệnh SQL, hoặc <see cref="CommandType.StoredProcedure"/> cho thủ tục.</param>
        /// <param name="cancellationToken">Token huỷ bỏ thao tác bất đồng bộ nếu cần dừng sớm.</param>
        /// <returns>
        /// Danh sách kết quả truy vấn có kiểu <typeparamref name="T"/>.
        /// Nếu câu truy vấn rỗng hoặc null, phương thức sẽ trả về <see cref="Enumerable.Empty{T}"/>.
        /// </returns>
        ///
        public async Task<IEnumerable<T>> GetAllExecuteAsync<T>(string pSqlQuery,
                                                                DynamicParameters pParams,
                                                                int commandTimeout = 30,
                                                                CommandType commandType = CommandType.Text,
                                                                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pSqlQuery)) return null;

            using IDbConnection connection = ConfigurationHelpers.CreateConnection(_dbConnection);

            IEnumerable<T> result = await connection.QueryAsync<T>(sql: pSqlQuery,
                                                                   param: pParams,
                                                                   commandType: commandType,
                                                                   commandTimeout: commandTimeout).ConfigureAwait(false);

            return result;
        }
    }
}