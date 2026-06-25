// Ignore Spelling: SQL

using Dapper;
using FTELSRCore.Data.SQL.DbContexts.Read;
using FTELSRCore.Data.SQL.DbContexts.Write;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace FTELSRCore.Data.SQL.Core
{
    public interface ICoreSQL<TEntity, DBContextRead, DBContextWrite>
        where TEntity : class
        where DBContextRead : ReadDbContext<DBContextRead>
        where DBContextWrite : WriteDbContext<DBContextWrite>
    {
        /// <summary>
        /// Thực thi truy vấn SQL và trả về một kết quả duy nhất ánh xạ vào DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO của kết quả.</typeparam>
        /// <param name="scriptSQLQuery">Truy vấn SQL cần thực thi.</param>
        /// <param name="parameters">Các tham số của truy vấn SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ, mặc định 30 giây.</param>
        /// <param name="commandType">Loại câu lệnh, mặc định là <see cref="CommandType.Text"/>.</param>
        /// <param name="cancellationToken">Token hủy tác vụ.</param>
        /// <returns>Task chứa kết quả duy nhất ánh xạ vào DTO.</returns>
        ///
        Task<TDto> FindOneWithScriptAsync<TDto>(string scriptSQLQuery,
                                                DynamicParameters parameters,
                                                int commandTimeout = 30,
                                                CommandType commandType = CommandType.Text,
                                                CancellationToken cancellationToken = default);

        /// <summary>
        /// Thực thi truy vấn SQL và trả về một danh sách kết quả ánh xạ vào DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO của các kết quả.</typeparam>
        /// <param name="scriptSQLQuery">Truy vấn SQL cần thực thi.</param>
        /// <param name="parameters">Các tham số của truy vấn SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ, mặc định 30 giây.</param>
        /// <param name="commandType">Loại câu lệnh, mặc định là <see cref="CommandType.Text"/>.</param>
        /// <param name="cancellationToken">Token hủy tác vụ.</param>
        /// <returns>Task chứa danh sách các kết quả ánh xạ vào DTO.</returns>
        ///
        Task<IEnumerable<TDto>> FindAllWithScriptAsync<TDto>(string scriptSQLQuery,
                                                             DynamicParameters parameters,
                                                             int commandTimeout = 30,
                                                             CommandType commandType = CommandType.Text,
                                                             CancellationToken cancellationToken = default);

        /// <summary>
        /// Thực thi truy vấn SQL và trả về kết quả ánh xạ vào DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO của kết quả.</typeparam>
        /// <param name="scriptSQLQuery">Truy vấn SQL cần thực thi.</param>
        /// <param name="parameters">Các tham số của truy vấn SQL.</param>
        /// <param name="commandTimeout">Thời gian chờ, mặc định 30 giây.</param>
        /// <param name="commandType">Loại câu lệnh, mặc định là <see cref="CommandType.Text"/>.</param>
        /// <param name="cancellationToken">Token hủy tác vụ.</param>
        /// <returns>Task chứa kết quả ánh xạ vào DTO.</returns>
        ///
        Task<TDto> FindOneWithScalarScriptAsync<TDto>(string scriptSQLQuery,
                                                      DynamicParameters parameters,
                                                      int commandTimeout = 30,
                                                      CommandType commandType = CommandType.Text,
                                                      CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra và thực thi một câu lệnh SQL với kiểu câu lệnh xác định, sử dụng tham số động.
        /// </summary>
        /// <param name="scriptSQLQuery">Câu lệnh SQL cần thực thi.</param>
        /// <param name="parameters">Tham số động cho câu lệnh SQL.</param>
        /// <param name="context">Đối tượng DbContext để truy cập cơ sở dữ liệu.</param>
        /// <param name="transaction">Phiên giao dịch cơ sở dữ liệu.</param>
        /// <param name="commandTimeout">Thời gian chờ câu lệnh SQL (mặc định 30 giây).</param>
        /// <param name="commandType">Loại câu lệnh SQL (mặc định là CommandType.Text).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>True nếu câu lệnh SQL thực thi thành công (với kết quả > 0), ngược lại là false.</returns>
        ///
        Task<bool> IsSAPWithScalarScriptAsync(string scriptSQLQuery,
                                              DbConnection context,
                                              DbTransaction transaction,
                                              DynamicParameters parameters,
                                              int commandTimeout = 30,
                                              CommandType commandType = CommandType.Text,
                                              CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm một thực thể theo ID.
        /// </summary>
        /// <param name="id">ID của thực thể cần tìm.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Thực thể tìm thấy, hoặc giá trị null nếu không tìm thấy.</returns>
        ///
        Task<TEntity> FindByIdAsync(
            object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc và trạng thái "isDeleted" đã chỉ định, sau đó chuyển đổi nó thành đối tượng DTO.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="isDeleted">Trạng thái "isDeleted" của thực thể (true hoặc false).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Đối tượng DTO của thực thể đầu tiên thỏa mãn bộ lọc và trạng thái "isDeleted", hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        Task<TDto> FindOneSortDeletedAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters, bool isDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc và trạng thái "isDeleted" đã chỉ định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="isDeleted">Trạng thái "isDeleted" của thực thể (true hoặc false).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Thực thể đầu tiên thỏa mãn bộ lọc và trạng thái "isDeleted", hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        Task<TEntity> FindOneSortDeletedAsync(
            Expression<Func<TEntity, bool>>[] filters, bool isDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc đã chỉ định và chuyển đổi nó sang kiểu dữ liệu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu DTO mà thực thể sẽ được chuyển đổi thành.</typeparam>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>DTO của thực thể đầu tiên thỏa mãn bộ lọc, hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        Task<TDto> FindOneAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc đã chỉ định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Thực thể đầu tiên thỏa mãn bộ lọc, hoặc null nếu không tìm thấy.</returns>
        ///
        Task<TEntity> FindOneAsync(
            Expression<Func<TEntity, bool>>[] filters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm tất cả các thực thể trong cơ sở dữ liệu thỏa mãn các bộ lọc đã chỉ định và có trạng thái xóa xác định, và chuyển đổi chúng thành kiểu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO mà kết quả sẽ được chuyển đổi sang.</typeparam>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm các thực thể.</param>
        /// <param name="isDeleted">Trạng thái xóa của các thực thể cần tìm (true nếu xóa, false nếu chưa xóa).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Danh sách các đối tượng DTO thỏa mãn bộ lọc và trạng thái xóa.</returns>
        ///
        Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters, bool isDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm tất cả các thực thể trong cơ sở dữ liệu thỏa mãn các bộ lọc đã chỉ định và có trạng thái xóa xác định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm các thực thể.</param>
        /// <param name="isDeleted">Trạng thái xóa của các thực thể cần tìm (true nếu xóa, false nếu chưa xóa).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Danh sách các thực thể thỏa mãn bộ lọc và trạng thái xóa.</returns>
        ///
        Task<List<TEntity>> FindAllSortDeletedAsync(
            Expression<Func<TEntity, bool>>[] filters, bool isDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm tất cả các thực thể trong cơ sở dữ liệu thỏa mãn các bộ lọc đã chỉ định và chuyển đổi chúng thành kiểu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO để chuyển đổi các thực thể.</typeparam>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm các thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Danh sách các đối tượng DTO thỏa mãn bộ lọc.</returns>
        ///
        Task<List<TDto>> FindAllAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm tất cả các thực thể trong cơ sở dữ liệu thỏa mãn các bộ lọc đã chỉ định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm các thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Danh sách các thực thể thỏa mãn bộ lọc.</returns>
        ///
        Task<List<TEntity>> FindAllAsync(
            Expression<Func<TEntity, bool>>[] filters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm một thực thể vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entity">Thực thể cần thêm.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tuỳ chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Số lượng bản ghi đã được thêm vào cơ sở dữ liệu.</returns>
        ///
        Task<int> CreateAsync(TEntity entity,
                              AuditModel auditLog = null,
                              CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm một thực thể vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entity">Thực thể cần thêm.</param>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tuỳ chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Tuple chứa số lượng thực thể đã thêm và thực thể đã thêm.</returns>
        ///
        Task<(int Result, TEntity Data)> CreateAsync(TEntity entity,
                                                     DBContextWrite context,
                                                     AuditModel auditLog = null,
                                                     CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm một tập hợp thực thể vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entities">Danh sách các thực thể cần thêm.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tuỳ chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Số lượng thực thể đã thêm vào.</returns>
        ///
        Task<int> CreateAsync(IEnumerable<TEntity> entities,
                              AuditModel auditLog = null,
                              CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm một tập hợp thực thể mới vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entities">Danh sách thực thể cần tạo.</param>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu để thực thi thao tác.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns> Trả về kết quả bao gồm số lượng bản ghi bị ảnh hưởng và danh sách thực thể đã tạo.</returns>
        ///
        Task<(int Result, IEnumerable<TEntity> Data)> CreateAsync(IEnumerable<TEntity> entities,
                                                                  DBContextWrite context,
                                                                  AuditModel auditLog = null,
                                                                  CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật một thực thể trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="entity">Thực thể cần cập nhật.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns>
        /// Trả về số lượng bản ghi bị ảnh hưởng sau khi cập nhật.
        /// </returns>
        ///
        Task<int> UpdateAsync(TEntity entity,
                              AuditModel auditLog = null,
                              CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật một thực thể trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="entity">Thực thể cần cập nhật.</param>
        /// <param name="context">Đối tượng ngữ cảnh ghi dữ liệu.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns>Một tuple chứa kết quả cập nhật và thực thể đã được cập nhật.</returns>
        ///
        Task<(int Result, TEntity Data)> UpdateAsync(TEntity entity,
                                                     DBContextWrite context,
                                                     AuditModel auditLog = null,
                                                     CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật nhiều thực thể trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="entities">Danh sách thực thể cần cập nhật.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns>Số lượng bản ghi được cập nhật.</returns>
        ///
        Task<int> UpdateAsync(IEnumerable<TEntity> entities,
                              AuditModel auditLog = null,
                              CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật một tập hợp thực thể trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="entities">Danh sách thực thể cần cập nhật.</param>
        /// <param name="context">Đối tượng ngữ cảnh ghi dữ liệu.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns> Một tuple chứa kết quả cập nhật (số bản ghi bị ảnh hưởng) và danh sách thực thể đã được cập nhật.</returns>
        ///
        Task<(int Result, IEnumerable<TEntity> Data)> UpdateAsync(IEnumerable<TEntity> entities,
                                                                  DBContextWrite context,
                                                                  AuditModel auditLog = null,
                                                                  CancellationToken cancellationToken = default);
    }
}