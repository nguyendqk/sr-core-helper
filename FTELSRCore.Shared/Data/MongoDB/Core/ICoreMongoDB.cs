// Ignore Spelling: Mongo

using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using static FTELSRCore.Extensions.ProjectToExtensions;

namespace FTELSRCore.Data.MongoDB.Core
{
    public interface ICoreMongoDB<TTable> where TTable : class
    {
         Task<long> CountAllAsync(
            FilterDefinition<TTable> filter = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đếm bất đồng bộ số lượng tài liệu trong bộ sưu tập MongoDB dựa trên bộ lọc và trạng thái xóa mềm.
        /// </summary>
        /// <param name="filter">Bộ lọc dạng biểu thức để chọn các tài liệu cần đếm.</param>
        /// <param name="isDeleted">Xác định xem có chỉ đếm các tài liệu bị xóa mềm (true) hay không bị xóa mềm (false). Mặc định là false.</param>
        /// <param name="cancellationToken">Token để hủy thao tác bất đồng bộ. Mặc định là default.</param>
        /// <returns>Task trả về số lượng tài liệu (long) thỏa mãn bộ lọc và trạng thái xóa mềm.</returns>
        /// <remarks>
        /// Hàm kết hợp bộ lọc đầu vào với điều kiện xóa mềm, thực thi với chính sách thử lại (_retryPolicy)
        /// và sử dụng MongoDB CountDocumentsAsync để đếm tài liệu.
        /// </remarks>
        ///
        public Task<long> CountAllSortDeletedAsync(
            Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đếm bất đồng bộ số lượng tài liệu trong bộ sưu tập MongoDB dựa trên bộ lọc.
        /// </summary>
        /// <param name="filter">Bộ lọc dạng biểu thức để chọn các tài liệu cần đếm.</param>
        /// <param name="cancellationToken">Token để hủy thao tác bất đồng bộ. Mặc định là default.</param>
        /// <returns>Task trả về số lượng tài liệu (long) thỏa mãn bộ lọc.</returns>
        /// <remarks>
        /// Hàm thực thi với chính sách thử lại (_retryPolicy) và sử dụng MongoDB CountDocumentsAsync để đếm tài liệu.
        /// </remarks>
        ///
        public Task<long> CountAllAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm tất cả các bản ghi có phân trang và chuyển đổi chúng thành kiểu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO mà dữ liệu sẽ được chuyển đổi sang.</typeparam>
        /// <param name="queryContext">Các hàm xử lý truy vấn, sắp xếp và chuyển đổi dữ liệu.</param>
        /// <param name="pageNumber">Số trang (mặc định là null).</param>
        /// <param name="pageSize">Kích thước mỗi trang (mặc định là 10).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu DTO được phân trang và lọc theo điều kiện.</returns>
        ///
        Task<List<TDto>> FindAllPagingAsync<TDto>(
            QueryContext<TTable, TDto> queryContext,
            int pageSize = 10, int? pageNumber = null,
            CancellationToken cancellationToken = default) where TDto : class;

        /// <summary>
        /// Lấy thông tin danh sách theo điều kiện có paging
        /// </summary>
        /// <typeparam name="TDto"></typeparam>
        /// <param name="filter"></param>
        /// <param name="sortDefinition"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<TDto>> FindAllPagingAsync<TDto>(
            FilterDefinition<TTable> filter, SortDefinition<TTable> sortDefinition,
            int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default) where TDto : class;

        /// <summary>
        /// Tìm tất cả các bản ghi có phân trang và chuyển đổi chúng thành kiểu DTO.
        /// </summary>
        /// <param name="queryContext">Các hàm xử lý truy vấn, sắp xếp và chuyển đổi dữ liệu.</param>
        /// <param name="pageNumber">Số trang (mặc định là null).</param>
        /// <param name="pageSize">Kích thước mỗi trang (mặc định là 10).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu DTO được phân trang và lọc theo điều kiện.</returns>
        ///
        Task<List<TTable>> FindAllPagingAsync(
            QueryContext<TTable, TTable> queryContext,
            int pageSize = 10, int? pageNumber = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm tất cả các bản ghi có phân trang và chuyển đổi chúng thành kiểu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO mà dữ liệu sẽ được chuyển đổi sang.</typeparam>
        /// <param name="filter">Biểu thức lọc các bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="pageNumber">Số trang (mặc định là 1).</param>
        /// <param name="pageSize">Kích thước mỗi trang (mặc định là 10).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu DTO được phân trang và lọc theo điều kiện.</returns>
        ///
        Task<List<TDto>> FindAllPagingAsync<TDto>(
            Expression<Func<TTable, bool>> filter, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default) where TDto : class;

        /// <summary>
        /// Truy vấn danh sách bản ghi theo phân trang, sau đó ánh xạ sang DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu đích để ánh xạ.</typeparam>
        /// <param name="filter">Biểu thức điều kiện lọc.</param>
        /// <param name="sort">Định nghĩa sắp xếp.</param>
        /// <param name="pageNumber">Số trang (mặc định là 1).</param>
        /// <param name="pageSize">Số lượng bản ghi mỗi trang (mặc định là 10).</param>
        /// <param name="cancellationToken">Token hủy bỏ.</param>
        /// <returns>Danh sách DTO đã được ánh xạ.</returns>
        ///
        Task<List<TDto>> FindAllPagingAsync<TDto>(
            Expression<Func<TTable, bool>> filter, SortDefinition<TTable> sort, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default) where TDto : class;

        /// <summary>
        /// Tìm tất cả các bản ghi có phân trang từ cơ sở dữ liệu và chuyển đổi chúng thành kiểu TTable.
        /// </summary>
        /// <param name="filter">Biểu thức lọc các bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="pageNumber">Số trang (mặc định là 1).</param>
        /// <param name="pageSize">Kích thước mỗi trang (mặc định là 10).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu TTable được phân trang và lọc theo điều kiện.</returns>
        ///
        Task<List<TTable>> FindAllPagingAsync(
            Expression<Func<TTable, bool>> filter, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Truy vấn danh sách bản ghi theo phân trang dựa trên bộ lọc và sắp xếp.
        /// </summary>
        /// <param name="filter">Biểu thức điều kiện lọc.</param>
        /// <param name="sort">Định nghĩa sắp xếp.</param>
        /// <param name="pageNumber">Số trang (mặc định là 1).</param>
        /// <param name="pageSize">Số lượng bản ghi mỗi trang (mặc định là 10).</param>
        /// <param name="cancellationToken">Token hủy bỏ.</param>
        /// <returns>Danh sách bản ghi theo trang.</returns>
        ///
        Task<List<TTable>> FindAllPagingAsync(
            Expression<Func<TTable, bool>> filter, SortDefinition<TTable> sort, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm tất cả các bản ghi từ cơ sở dữ liệu theo bộ lọc và chuyển đổi chúng thành kiểu TDto.
        /// </summary>
        /// <param name="filter">Biểu thức lọc các bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu TDto được lọc theo điều kiện.</returns>
        ///
        Task<List<TDto>> FindAllAsync<TDto>(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default) where TDto : class;

        /// <summary>
        /// Tìm tất cả các bản ghi từ cơ sở dữ liệu theo bộ lọc và chuyển đổi chúng thành kiểu TTable.
        /// </summary>
        /// <param name="filter">Biểu thức lọc các bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu TTable được lọc theo điều kiện.</returns>
        ///
        Task<List<TTable>> FindAllAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="isDeleted"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<List<TTable>> FindAllSortDeletedAsync(
            Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TDto"></typeparam>
        /// <param name="filter"></param>
        /// <param name="isDeleted"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
            Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default) where TDto : class;

        /// <summary>
        /// Tìm một bản ghi duy nhất từ cơ sở dữ liệu theo bộ lọc và chuyển đổi nó thành kiểu TDto.
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bản ghi duy nhất kiểu TDto hoặc null nếu không tìm thấy.</returns>
        ///
        Task<TDto> FindOneAsync<TDto>(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default) where TDto : class;

        /// <summary>
        /// Tìm một bản ghi duy nhất từ cơ sở dữ liệu theo bộ lọc và chuyển đổi nó thành kiểu TTable.
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bản ghi duy nhất kiểu TTable hoặc null nếu không tìm thấy.</returns>
        ///
        Task<TTable> FindOneAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm một bản ghi duy nhất từ cơ sở dữ liệu theo bộ lọc và chuyển đổi nó thành kiểu TDto.
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="isDeleted"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bản ghi duy nhất kiểu TDto hoặc null nếu không tìm thấy.</returns>
        ///
        Task<TDto> FindOneSortDeletedAsync<TDto>(
            Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default) where TDto : class;

        /// <summary>
        /// Tìm một bản ghi duy nhất từ cơ sở dữ liệu theo bộ lọc và chuyển đổi nó thành kiểu TTable.
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="isDeleted"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bản ghi duy nhất kiểu TTable hoặc null nếu không tìm thấy.</returns>
        ///
        Task<TTable> FindOneSortDeletedAsync(
            Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm một bản ghi mới vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entity">Dữ liệu cần thêm vào cơ sở dữ liệu.</param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về true nếu thêm bản ghi thành công, false nếu có lỗi hoặc dữ liệu không hợp lệ.</returns>
        ///
        Task<bool> IsCreateOneAsync(
            TTable entity, AuditModel audit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo nhiều bản ghi trong cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="TTable">Kiểu dữ liệu của đối tượng cần chèn.</typeparam>
        /// <param name="entites">Danh sách các đối tượng cần chèn vào cơ sở dữ liệu.</param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về <c>true</c> nếu việc chèn thành công, <c>false</c> nếu thất bại hoặc có lỗi xảy ra.</returns>
        ///
        Task<bool> IsCreateManyAsync(IEnumerable<TTable> entites, AuditModel audit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật một bản ghi trong cơ sở dữ liệu theo bộ lọc và cập nhật dữ liệu mới.
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi cần cập nhật trong cơ sở dữ liệu.</param>
        /// <param name="entity">Đối tượng chứa thông tin cần cập nhật.</param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về true nếu cập nhật thành công, false nếu có lỗi hoặc các tham số không hợp lệ.</returns>
        ///
        Task<bool> IsUpdateOneAsync(
            Expression<Func<TTable, bool>> filter, TTable entity, AuditModel audit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật một bản ghi duy nhất dựa trên điều kiện lọc.
        /// </summary>
        /// <param name="filter">Biểu thức điều kiện lọc.</param>
        /// <param name="updateDefinition">Định nghĩa cập nhật.</param>
        /// <param name="audit">Thông tin audit, nếu có.</param>
        /// <param name="cancellationToken">Token hủy bỏ.</param>
        /// <returns>Trả về true nếu có bản ghi được cập nhật; ngược lại là false.</returns>
        ///
        Task<bool> IsUpdateOneAsync(
            Expression<Func<TTable, bool>> filter, UpdateDefinition<TTable> updateDefinition, AuditModel audit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật nhiều bản ghi trong cơ sở dữ liệu theo bộ lọc và dữ liệu cập nhật.
        /// </summary>
        /// <typeparam name="TTable">Kiểu dữ liệu của đối tượng yêu cầu cập nhật.</typeparam>
        /// <param name="filter">Biểu thức bộ lọc để xác định các bản ghi cần cập nhật.</param>
        /// <param name="entity">Dữ liệu cập nhật cho các bản ghi thỏa mãn bộ lọc.</param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về <c>true</c> nếu việc cập nhật thành công, <c>false</c> nếu thất bại hoặc có lỗi xảy ra.</returns>
        ///
        Task<bool> IsUpdateManyAsync(
             Expression<Func<TTable, bool>> filter, TTable entity, AuditModel audit = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<bool> IsUpdateManyAsync(
            List<(Expression<Func<TTable, bool>> filter, TTable entity)> entities, AuditModel audit = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<bool> IsUpdateManyAsync(
            List<(Expression<Func<TTable, bool>> filter, UpdateDefinition<TTable> entity)> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật nhiều bản ghi dựa trên điều kiện lọc.
        /// </summary>
        /// <param name="filter">Biểu thức điều kiện lọc.</param>
        /// <param name="updateDefinition">Định nghĩa cập nhật.</param>
        /// <param name="audit">Thông tin audit, nếu có.</param>
        /// <param name="cancellationToken">Token hủy bỏ.</param>
        /// <returns>Trả về true nếu có ít nhất một bản ghi được cập nhật; ngược lại là false.</returns>
        ///
        Task<bool> IsUpdateManyAsync(
            Expression<Func<TTable, bool>> filter, UpdateDefinition<TTable> updateDefinition, AuditModel audit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một bản ghi trong cơ sở dữ liệu dựa trên điều kiện lọc.
        /// </summary>
        /// <param name="filter">Điều kiện lọc để xác định bản ghi cần xóa.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về true nếu xóa thành công, false nếu có lỗi hoặc điều kiện lọc không hợp lệ.</returns>
        ///
        Task<bool> IsDeleteOneAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa nhiều bản ghi trong cơ sở dữ liệu theo bộ lọc.
        /// </summary>
        /// <param name="filter">Biểu thức bộ lọc để xác định các bản ghi cần xóa.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về <c>true</c> nếu việc xóa thành công, <c>false</c> nếu thất bại hoặc có lỗi xảy ra.</returns>
        ///
        Task<bool> IsDeleteManyAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin tổng hợp từ cơ sở dữ liệu sử dụng pipeline.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="pipeline"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<List<TResult>> FindAllWithAggregateAsync<TResult>(
            PipelineDefinition<TTable, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin tổng hợp từ cơ sở dữ liệu sử dụng pipeline.
        /// </summary>
        /// <param name="pipeline"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<TTable>> FindAllWithAggregateAsync(
            BsonDocument[] pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin tổng hợp từ cơ sở dữ liệu sử dụng pipeline.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="pipeline"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<List<TResult>> FindAllWithAggregateAsync<TResult>(
            BsonDocument[] pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="options"></param>
        /// <param name="requests"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<bool> BulkWriteAsync(
            IEnumerable<WriteModel<TTable>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default);
    }
}