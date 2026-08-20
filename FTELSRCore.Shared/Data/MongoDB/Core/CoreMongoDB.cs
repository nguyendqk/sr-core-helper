using MongoDB.Bson;
using MongoDB.Driver;
using Polly;
using System.Data;
using System.Linq.Expressions;
using static FTELSRCore.Extensions.ProjectToExtensions;

namespace FTELSRCore.Data.MongoDB.Core
{
    #region :::::::::::::::::::::::::::::::::: BaseMongoDBRepository Tồn tại <TTable> ::::::::::::::::::::::::::::::::::

    public abstract class CoreMongoDB<TTable> : ICoreMongoDB<TTable> where TTable : class
    {
        #region :::::::: Ctor ::::::::

        private readonly ILogger<CoreMongoDB<TTable>> _logger;

        private readonly ResiliencePipeline _pipelineRead;

        private readonly ResiliencePipeline _pipelineWrite;

        private readonly Lazy<IMongoCollection<TTable>> _dbReadContext;

        private readonly Lazy<IMongoCollection<TTable>> _dbWriteContext;

        private static readonly AggregateOptions _aggregateOptions = new()
        {
            BatchSize = 500,
            MaxTime = TimeSpan.FromSeconds(30)
        };

        protected CoreMongoDB(
            string collectionName,
            IMongoDatabase dbContextRead,
            IMongoDatabase dbContextWrite,
            ILogger<CoreMongoDB<TTable>> logger,
            ResiliencePipeline pipelineRead, 
            ResiliencePipeline pipelineWrite)
        {
            _logger = logger;

            _dbWriteContext =
                new Lazy<IMongoCollection<TTable>>(
                    () => dbContextWrite.GetCollection<TTable>(name: collectionName));

            _dbReadContext =
                new Lazy<IMongoCollection<TTable>>(
                    () => dbContextRead.GetCollection<TTable>(name: collectionName));

            _pipelineRead = pipelineRead;

            _pipelineWrite = pipelineWrite;
        }

        #endregion :::::::: Ctor ::::::::

        /// <summary>
        /// Đếm số lượng bản ghi thỏa mãn điều kiện lọc.
        /// </summary>
        /// <param name="filter">Điều kiện lọc (nếu không truyền sẽ lấy tất cả).</param>
        /// <param name="cancellationToken">Token hủy bỏ.</param>
        /// <returns>Số lượng bản ghi thỏa điều kiện.</returns>
        ///
        public virtual async Task<long> CountAllAsync(
            FilterDefinition<TTable> filter = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // filter = null nghĩa là "lấy tất cả" theo tài liệu của interface — driver
            // ném ArgumentNullException nếu nhận filter null nên phải quy về Filter.Empty.
            filter ??= Builders<TTable>.Filter.Empty;

            return await _pipelineRead.ExecuteAsync(
                callback: async ct =>
                {
                    return
                        await _dbReadContext.Value.CountDocumentsAsync(
                            filter: filter, cancellationToken: ct).ConfigureAwait(false);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

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
        ///
        public virtual async Task<List<TDto>> FindAllPagingAsync<TDto>(
            FilterDefinition<TTable> filter,
            SortDefinition<TTable> sortDefinition,
            int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default) where TDto : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TTable> result =
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.Find(filter)
                                                      .Sort(sortDefinition)
                                                      .Skip((pageNumber - 1) * pageSize)
                                                      .Limit(pageSize)
                                                      .ToListAsync(cancellationToken)
                                                      .ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return !result.IsNullOrEmpty() ? result.ProjectTo<TTable, TDto>() : [];
        }

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
        public virtual async Task<List<TDto>> FindAllPagingAsync<TDto>(
            QueryContext<TTable, TDto> queryContext,
            int pageSize = 10, int? pageNumber = null,
            CancellationToken cancellationToken = default) where TDto : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            IFindFluent<TTable, TDto> findFluent =
                _dbReadContext.Value.Find(filter: queryContext.Predicate)
                .Project(projection: queryContext.Selector);

            if (queryContext is { Sorting: not null })
            {
                findFluent = findFluent.Sort(sort: queryContext.Sorting);
            }

            if (pageSize > 0)
            {
                findFluent = findFluent.Limit(limit: pageSize);
            }

            if (pageNumber.HasValue is true)
            {
                findFluent = findFluent.Skip(skip: pageNumber);
            }

            List<TDto> result =
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await findFluent.ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return !result.IsNullOrEmpty() ? result : [];
        }

        /// <summary>
        /// Tìm tất cả các bản ghi có phân trang và chuyển đổi chúng thành kiểu DTO.
        /// </summary>
        /// <param name="queryContext">Các hàm xử lý truy vấn, sắp xếp và chuyển đổi dữ liệu.</param>
        /// <param name="pageNumber">Số trang (mặc định là null).</param>
        /// <param name="pageSize">Kích thước mỗi trang (mặc định là 10).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu DTO được phân trang và lọc theo điều kiện.</returns>
        ///
        public virtual async Task<List<TTable>> FindAllPagingAsync(
            QueryContext<TTable, TTable> queryContext,
            int pageSize = 10, int? pageNumber = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var findFluent =
                _dbReadContext.Value.Find(filter: queryContext.Predicate);

            if (queryContext is { Sorting: not null })
            {
                findFluent = findFluent.Sort(sort: queryContext.Sorting);
            }

            if (pageSize > 0)
            {
                findFluent = findFluent.Limit(limit: pageSize);
            }

            if (pageNumber.HasValue is true)
            {
                findFluent = findFluent.Skip(skip: pageNumber);
            }

            List<TTable> result =
                  await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await findFluent.ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return !result.IsNullOrEmpty() ? result : [];
        }

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
        public virtual async Task<List<TDto>> FindAllPagingAsync<TDto>(
            Expression<Func<TTable, bool>> filter, int pageNumber = 1, int pageSize = 10,
            CancellationToken cancellationToken = default) where TDto : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TTable> result =
                  await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                             await _dbReadContext.Value.Find(filter)
                                                      .Skip((pageNumber - 1) * pageSize)
                                                      .Limit(pageSize)
                                                      .ToListAsync(cancellationToken)
                                                      .ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return !result.IsNullOrEmpty() ? result.ProjectTo<TTable, TDto>() : [];
        }

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
        public virtual async Task<List<TDto>> FindAllPagingAsync<TDto>(
            Expression<Func<TTable, bool>> filter, SortDefinition<TTable> sort,
            int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default) where TDto : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TTable> result =
                  await _pipelineRead.ExecuteAsync(
                  async cancellationToken =>
                  {
                      return
                           await _dbReadContext.Value.Find(filter)
                                                      .Skip((pageNumber - 1) * pageSize)
                                                      .Limit(pageSize)
                                                      .Sort(sort)
                                                      .ToListAsync(cancellationToken)
                                                      .ConfigureAwait(false);
                  }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return !result.IsNullOrEmpty() ? result.ProjectTo<TTable, TDto>() : [];
        }

        /// <summary>
        /// Tìm tất cả các bản ghi có phân trang từ cơ sở dữ liệu và chuyển đổi chúng thành kiểu TTable.
        /// </summary>
        /// <param name="filter">Biểu thức lọc các bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="pageNumber">Số trang (mặc định là 1).</param>
        /// <param name="pageSize">Kích thước mỗi trang (mặc định là 10).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu TTable được phân trang và lọc theo điều kiện.</returns>
        ///
        public virtual async Task<List<TTable>> FindAllPagingAsync(
            Expression<Func<TTable, bool>> filter,
            int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return
                   await _pipelineRead.ExecuteAsync(
                   async cancellationToken =>
                   {
                       return
                           await _dbReadContext.Value.Find(filter)
                                                   .Skip((pageNumber - 1) * pageSize)
                                                   .Limit(pageSize)
                                                   .ToListAsync(cancellationToken).ConfigureAwait(false);
                   }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

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
        public virtual async Task<List<TTable>> FindAllPagingAsync(
            Expression<Func<TTable, bool>> filter, SortDefinition<TTable> sort,
            int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return
               await _pipelineRead.ExecuteAsync(
               async cancellationToken =>
               {
                   return
                      await _dbReadContext.Value.Find(filter)
                                                          .Skip((pageNumber - 1) * pageSize)
                                                          .Limit(pageSize)
                                                          .Sort(sort)
                                                          .ToListAsync(cancellationToken).ConfigureAwait(false);
               }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

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
        public virtual async Task<long> CountAllSortDeletedAsync(
            Expression<Func<TTable, bool>> filter,
            bool isDeleted = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Expression<Func<TTable, bool>> addIsDeleted =
                PrecateBuilderExtensions.AddIsDeleted<TTable>(isDeleted);

            filter = filter.And(addIsDeleted);

            return
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                         await _dbReadContext.Value.CountDocumentsAsync(
                                                filter: filter, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

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
        public virtual async Task<long> CountAllAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.CountDocumentsAsync(
                                filter: filter, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Tìm tất cả các bản ghi từ cơ sở dữ liệu theo bộ lọc và chuyển đổi chúng thành kiểu TDto.
        /// </summary>
        /// <param name="filter">Biểu thức lọc các bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu TDto được lọc theo điều kiện.</returns>
        ///
        public virtual async Task<List<TDto>> FindAllAsync<TDto>(
            Expression<Func<TTable, bool>> filter,
            CancellationToken cancellationToken = default) where TDto : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TTable> result =
              await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.Find(
                            filter: filter).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return !result.IsNullOrEmpty() ? result.ProjectTo<TTable, TDto>() : [];
        }

        /// <summary>
        /// Tìm tất cả bản ghi theo bộ lọc, kết hợp thêm điều kiện trạng thái xóa mềm, và chuyển đổi sang kiểu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO mà dữ liệu sẽ được chuyển đổi sang.</typeparam>
        /// <param name="filter">Biểu thức lọc bản ghi.</param>
        /// <param name="isDeleted">true: chỉ lấy bản ghi đã xóa mềm; false: chỉ lấy bản ghi chưa xóa mềm.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách bản ghi kiểu TDto thỏa điều kiện lọc và trạng thái xóa mềm.</returns>
        public virtual async Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
            Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default) where TDto : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            Expression<Func<TTable, bool>> addIsDeleted =
                PrecateBuilderExtensions.AddIsDeleted<TTable>(isDeleted);

            filter = filter.And(addIsDeleted);

            List<TTable> result =
              await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.Find(
                                        filter: filter).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return !result.IsNullOrEmpty() ? result.ProjectTo<TTable, TDto>() : [];
        }

        /// <summary>
        /// Tìm tất cả các bản ghi từ cơ sở dữ liệu theo bộ lọc và chuyển đổi chúng thành kiểu TTable.
        /// </summary>
        /// <param name="filter">Biểu thức lọc các bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách các bản ghi kiểu TTable được lọc theo điều kiện.</returns>
        ///
        public virtual async Task<List<TTable>> FindAllAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return
                  await _pipelineRead.ExecuteAsync(
                        async cancellationToken =>
                        {
                            return
                                await _dbReadContext.Value.Find(
                                            filter: filter).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Tìm tất cả bản ghi theo bộ lọc, kết hợp thêm điều kiện trạng thái xóa mềm (AND với <c>isDeleted</c>).
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi.</param>
        /// <param name="isDeleted">true: chỉ lấy bản ghi đã xóa mềm; false: chỉ lấy bản ghi chưa xóa mềm.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Danh sách bản ghi kiểu TTable thỏa điều kiện lọc và trạng thái xóa mềm.</returns>
        public virtual async Task<List<TTable>> FindAllSortDeletedAsync(
           Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Expression<Func<TTable, bool>> addIsDeleted =
                PrecateBuilderExtensions.AddIsDeleted<TTable>(isDeleted);

            filter = filter.And(addIsDeleted);

            return
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.Find(
                                    filter: filter).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Tìm một bản ghi duy nhất từ cơ sở dữ liệu theo bộ lọc và chuyển đổi nó thành kiểu TDto.
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bản ghi duy nhất kiểu TDto hoặc null nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TDto> FindOneAsync<TDto>(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default) where TDto : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            TTable result =
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.Find(
                                filter: filter).FirstOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return result?.ProjectTo<TTable, TDto>();
        }

        /// <summary>
        /// Tìm một bản ghi duy nhất theo bộ lọc, kết hợp thêm điều kiện trạng thái xóa mềm, chuyển đổi sang kiểu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO mà dữ liệu sẽ được chuyển đổi sang.</typeparam>
        /// <param name="filter">Biểu thức lọc bản ghi.</param>
        /// <param name="isDeleted">true: chỉ lấy bản ghi đã xóa mềm; false: chỉ lấy bản ghi chưa xóa mềm.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bản ghi duy nhất kiểu TDto hoặc null nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TDto> FindOneSortDeletedAsync<TDto>(
           Expression<Func<TTable, bool>> filter,
           bool isDeleted = false, CancellationToken cancellationToken = default) where TDto : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            Expression<Func<TTable, bool>> addIsDeleted =
                PrecateBuilderExtensions.AddIsDeleted<TTable>(isDeleted);

            filter = filter.And(addIsDeleted);

            TTable result =
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.Find(
                                filter: filter).FirstOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return result?.ProjectTo<TTable, TDto>();
        }

        /// <summary>
        /// Tìm một bản ghi duy nhất từ cơ sở dữ liệu theo bộ lọc và chuyển đổi nó thành kiểu TTable.
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi trong cơ sở dữ liệu.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bản ghi duy nhất kiểu TTable hoặc null nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TTable> FindOneAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.Find(
                                filter: filter).FirstOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Tìm một bản ghi duy nhất theo bộ lọc, kết hợp thêm điều kiện trạng thái xóa mềm.
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi.</param>
        /// <param name="isDeleted">true: chỉ lấy bản ghi đã xóa mềm; false: chỉ lấy bản ghi chưa xóa mềm.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bản ghi duy nhất kiểu TTable hoặc null nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TTable> FindOneSortDeletedAsync(
           Expression<Func<TTable, bool>> filter, bool isDeleted = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Expression<Func<TTable, bool>> addIsDeleted =
                PrecateBuilderExtensions.AddIsDeleted<TTable>(isDeleted);

            filter = filter.And(addIsDeleted);

            return
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.Find(
                                filter: filter).FirstOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cập nhật một bản ghi trong cơ sở dữ liệu theo bộ lọc và cập nhật dữ liệu mới.
        /// </summary>
        /// <param name="filter">Biểu thức lọc bản ghi cần cập nhật trong cơ sở dữ liệu.</param>
        /// <param name="entity">Đối tượng chứa thông tin cần cập nhật.</param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về true nếu cập nhật hoặc tạo mới (upsert) thành công, false nếu có lỗi hoặc tham số không hợp lệ.</returns>
        /// <remarks>Bật upsert: nếu filter không khớp bản ghi nào, MongoDB sẽ tự tạo mới document.</remarks>
        ///
        public virtual async Task<bool> IsUpdateOneAsync(
            Expression<Func<TTable, bool>> filter,
            TTable entity, AuditModel audit = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null || filter is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateOneAsync),
                    $"Validate {typeof(TTable).Name} with {entity?.ToJSon()} is failed");

                return false;
            }

            entity = ProjectToExtensions.SetDataUpdatedDefault(entity: entity, audit: audit);

            UpdateDefinition<TTable> mapUpdateDefinition =
                ProjectToExtensions.MapUpdateDefinition(entity);

            if (mapUpdateDefinition is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateOneAsync),
                    $"MapUpdateDefinition {typeof(TTable).Name} with {entity?.ToJSon()} is failed");

                return false;
            }

            return await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    UpdateResult result =
                        await _dbWriteContext.Value.UpdateOneAsync(
                            filter: filter, update: mapUpdateDefinition,
                            options: new UpdateOptions { IsUpsert = true },
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (result is null)
                    {
                        _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateOneAsync),
                            $"Fail to update {typeof(TTable).Name} with {entity?.ToJSon()}");

                        return false;
                    }

                    switch (result.MatchedCount > 0)
                    {
                        // Tìm thấy document — thành công dù data không thay đổi (no-op idempotent)
                        case true:
                            return true;

                        case false:
                            {
                                if (result is { MatchedCount: 0, ModifiedCount: 0, UpsertedId: not null })
                                {
                                    return true;
                                }

                                break;
                            }
                    }

                    _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateOneAsync),
                        $"Modified {typeof(TTable).Name} with {entity?.ToJSon()} is failed");

                    return false;
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cập nhật một bản ghi duy nhất dựa trên điều kiện lọc.
        /// </summary>
        /// <param name="filter">Biểu thức điều kiện lọc.</param>
        /// <param name="updateDefinition">Định nghĩa cập nhật.</param>
        /// <param name="audit">Thông tin audit, nếu có.</param>
        /// <param name="cancellationToken">Token hủy bỏ.</param>
        /// <returns>Trả về true nếu có bản ghi được cập nhật hoặc tạo mới (upsert); ngược lại là false.</returns>
        /// <remarks>Bật upsert: nếu filter không khớp bản ghi nào, MongoDB sẽ tự tạo mới document.</remarks>
        ///
        public virtual async Task<bool> IsUpdateOneAsync(
            Expression<Func<TTable, bool>> filter, UpdateDefinition<TTable> updateDefinition,
            AuditModel audit = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (updateDefinition is null || filter is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateOneAsync),
                    $"Validate {typeof(TTable).Name} with {updateDefinition?.ToJSon()} is failed");

                return false;
            }

            updateDefinition =
                ProjectToExtensions.SetDataUpdatedDefault(updateDefinition, audit: audit);

            if (updateDefinition is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateOneAsync),
                    $"MapUpdateDefinition {typeof(TTable).Name} is null");

                return false;
            }

            return await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    UpdateResult result =
                        await _dbWriteContext.Value.UpdateOneAsync(
                            filter: filter,
                            update: updateDefinition,
                            options: new UpdateOptions { IsUpsert = true },
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (result is null)
                    {
                        _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateOneAsync),
                            $"Fail to update {typeof(TTable).Name} with {updateDefinition?.ToJSon()}");

                        return false;
                    }

                    switch (result.MatchedCount > 0)
                    {
                        // Tìm thấy document — thành công dù data không thay đổi (no-op idempotent)
                        case true:
                            return true;

                        case false:
                            {
                                if (result is { MatchedCount: 0, ModifiedCount: 0, UpsertedId: not null })
                                {
                                    return true;
                                }

                                break;
                            }
                    }

                    _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateOneAsync),
                        $"Modified {typeof(TTable).Name} with {updateDefinition?.ToJSon()} is failed");

                    return false;
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cập nhật nhiều bản ghi dựa trên điều kiện lọc.
        /// </summary>
        /// <param name="filter">Biểu thức điều kiện lọc.</param>
        /// <param name="updateDefinition">Định nghĩa cập nhật.</param>
        /// <param name="audit">Thông tin audit, nếu có.</param>
        /// <param name="cancellationToken">Token hủy bỏ.</param>
        /// <returns>Trả về true nếu có ít nhất một bản ghi được cập nhật hoặc tạo mới (upsert); ngược lại là false.</returns>
        /// <remarks>Bật upsert: nếu filter không khớp bản ghi nào, MongoDB sẽ tự tạo mới document.</remarks>
        ///
        public virtual async Task<bool> IsUpdateManyAsync(
            Expression<Func<TTable, bool>> filter,
            UpdateDefinition<TTable> updateDefinition, AuditModel audit = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (updateDefinition is null || filter is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                    $"Validate {typeof(TTable).Name} with {updateDefinition?.ToJSon()} is failed");

                return false;
            }

            updateDefinition =
                ProjectToExtensions.SetDataUpdatedDefault(updateDefinition, audit: audit);

            if (updateDefinition is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                    $"MapUpdateDefinition {typeof(TTable).Name} is null");

                return false;
            }

            return await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    UpdateResult result =
                        await _dbWriteContext.Value.UpdateManyAsync(
                            filter: filter,
                            update: updateDefinition,
                            options: new UpdateOptions { IsUpsert = true },
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (result is null)
                    {
                        _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                            $"Fail to update {typeof(TTable).Name} with {updateDefinition?.ToJSon()}");

                        return false;
                    }

                    switch (result.MatchedCount > 0)
                    {
                        // Tìm thấy document — thành công dù data không thay đổi (no-op idempotent)
                        case true:
                            return true;

                        case false:
                            {
                                if (result is { MatchedCount: 0, ModifiedCount: 0, UpsertedId: not null })
                                {
                                    return true;
                                }

                                break;
                            }
                    }

                    _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                        $"Modified {typeof(TTable).Name} with {updateDefinition?.ToJSon()} is failed");

                    return false;
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cập nhật nhiều bản ghi trong cơ sở dữ liệu theo bộ lọc và dữ liệu cập nhật.
        /// </summary>
        /// <typeparam name="TTable">Kiểu dữ liệu của đối tượng yêu cầu cập nhật.</typeparam>
        /// <param name="filter">Biểu thức bộ lọc để xác định các bản ghi cần cập nhật.</param>
        /// <param name="entity">Dữ liệu cập nhật cho các bản ghi thỏa mãn bộ lọc.</param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về <c>true</c> nếu việc cập nhật hoặc tạo mới (upsert) thành công, <c>false</c> nếu thất bại hoặc có lỗi xảy ra.</returns>
        /// <remarks>Bật upsert: nếu filter không khớp bản ghi nào, MongoDB sẽ tự tạo mới document.</remarks>
        ///
        public virtual async Task<bool> IsUpdateManyAsync(
            Expression<Func<TTable, bool>> filter,
            TTable entity, AuditModel audit = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null || filter is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                    $"Validate {typeof(TTable).Name} with {entity?.ToJSon()} is failed");

                return false;
            }

            entity = ProjectToExtensions.SetDataUpdatedDefault(entity: entity, audit: audit);

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                    $"MapUpdateDefinition {typeof(TTable).Name} is null");

                return false;
            }

            UpdateDefinition<TTable> mapUpdateDefinition =
                ProjectToExtensions.MapUpdateDefinition(request: entity);

            if (mapUpdateDefinition is null) return false;

            return await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    UpdateResult result =
                        await _dbWriteContext.Value.UpdateManyAsync(
                            filter: filter,
                            update: mapUpdateDefinition,
                            options: new UpdateOptions { IsUpsert = true },
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (result is null)
                    {
                        _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                            $"Fail to update {typeof(TTable).Name} with {entity?.ToJSon()}");

                        return false;
                    }

                    switch (result.MatchedCount > 0)
                    {
                        // Tìm thấy document — thành công dù data không thay đổi (no-op idempotent)
                        case true:
                            return true;

                        case false:
                            {
                                if (result is { MatchedCount: 0, ModifiedCount: 0, UpsertedId: not null })
                                {
                                    return true;
                                }

                                break;
                            }
                    }

                    _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                        $"Modified {typeof(TTable).Name} with {entity?.ToJSon()} is failed");

                    return false;
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cập nhật hàng loạt, mỗi phần tử có filter và entity riêng, gộp thành một lệnh BulkWrite (UpdateOneModel).
        /// </summary>
        /// <param name="entities">Danh sách cặp (filter, entity) — mỗi cặp là một bản ghi cần cập nhật.</param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về true nếu có ít nhất một bản ghi được cập nhật hoặc tạo mới (upsert); ngược lại false.</returns>
        /// <remarks>Bật upsert: nếu filter của một phần tử không khớp bản ghi nào, MongoDB sẽ tự tạo mới document.</remarks>
        ///
        public virtual async Task<bool> IsUpdateManyAsync(
            List<(Expression<Func<TTable, bool>> filter, TTable entity)> entities,
            AuditModel audit = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities is null || entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                    $"Validate {typeof(TTable).Name} with {entities?.ToJSon()} is failed");

                return false;
            }

            List<WriteModel<TTable>> writeModels = [];

            foreach ((Expression<Func<TTable, bool>> Filter, TTable Entity) in entities)
            {
                TTable entity =
                    ProjectToExtensions.SetDataUpdatedDefault(entity: Entity, audit: audit);

                UpdateDefinition<TTable> mapUpdateDefinition =
                    ProjectToExtensions.MapUpdateDefinition(request: entity);

                if (mapUpdateDefinition is null)
                {
                    continue;
                }

                writeModels.Add(new UpdateOneModel<TTable>(Filter, mapUpdateDefinition) { IsUpsert = true });
            }

            if (writeModels is null || writeModels.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                    $"MapUpdateDefinition {typeof(TTable).Name} is null");

                return false;
            }

            return await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    BulkWriteResult<TTable> result =
                        await _dbWriteContext.Value.BulkWriteAsync(
                            requests: writeModels, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (result is null)
                    {
                        _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                            $"Fail to update {typeof(TTable).Name}");

                        return false;
                    }

                    switch (result.MatchedCount > 0)
                    {
                        case true:
                            {
                                return true;
                            }
                        case false:
                            {
                                if (result.Upserts != null && result.Upserts.Any())
                                {
                                    return true;
                                }

                                break;
                            }
                    }

                    _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                        $"Modified {typeof(TTable).Name} with {writeModels?.ToJSon()} is failed");

                    return false;
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cập nhật hàng loạt bằng <see cref="UpdateDefinition{TTable}"/> có sẵn cho từng phần tử, gộp thành một
        /// lệnh BulkWrite (UpdateOneModel).
        /// </summary>
        /// <param name="entities">Danh sách cặp (filter, updateDefinition) — mỗi cặp là một bản ghi cần cập nhật.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về true nếu có ít nhất một bản ghi được cập nhật hoặc tạo mới (upsert); ngược lại false.</returns>
        /// <remarks>Bật upsert: nếu filter của một phần tử không khớp bản ghi nào, MongoDB sẽ tự tạo mới document.</remarks>
        ///
        public virtual async Task<bool> IsUpdateManyAsync(
            List<(Expression<Func<TTable, bool>> filter, UpdateDefinition<TTable> entity)> entities,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities is null || entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                    $"Validate {typeof(TTable).Name} with {entities?.ToJSon()} is failed");

                return false;
            }

            List<WriteModel<TTable>> writeModels = [];

            foreach ((Expression<Func<TTable, bool>> Filter, UpdateDefinition<TTable> Entity) in entities)
            {
                writeModels.Add(new UpdateOneModel<TTable>(Filter, Entity) { IsUpsert = true });
            }

            if (writeModels is null || writeModels.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                    $"MapUpdateDefinition {typeof(TTable).Name} is null");

                return false;
            }

            return await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    BulkWriteResult<TTable> result =
                        await _dbWriteContext.Value.BulkWriteAsync(
                            requests: writeModels, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (result is null)
                    {
                        _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                            $"Fail to update {typeof(TTable).Name}");

                        return false;
                    }

                    switch (result.MatchedCount > 0)
                    {
                        case true:
                            {
                                return true;
                            }
                        case false:
                            {
                                if (result.Upserts != null && result.Upserts.Any())
                                {
                                    return true;
                                }

                                break;
                            }
                    }

                    _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsUpdateManyAsync),
                        $"Modified {typeof(TTable).Name} with {writeModels?.ToJSon()} is failed");

                    return false;
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Thêm một bản ghi mới vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entity">Dữ liệu cần thêm vào cơ sở dữ liệu.</param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về true nếu thêm bản ghi thành công, false nếu có lỗi hoặc dữ liệu không hợp lệ.</returns>
        ///
        public virtual async Task<bool> IsCreateOneAsync(
            TTable entity, AuditModel audit = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsCreateOneAsync),
                    $"Validate {typeof(TTable).Name} with is null");

                return false;
            }

            entity = ProjectToExtensions.SetDataCreatedDefault(entity: entity, audit: audit);

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsCreateOneAsync),
                    $"Validate {typeof(TTable).Name} with SetDataCreatedDefault is null");

                return false;
            }

            await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    await _dbWriteContext.Value.InsertOneAsync(
                        document: entity, cancellationToken: cancellationToken).ConfigureAwait(false);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Tạo nhiều bản ghi trong cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="TTable">Kiểu dữ liệu của đối tượng cần chèn.</typeparam>
        /// <param name="entities">Danh sách các đối tượng cần chèn vào cơ sở dữ liệu.</param>
        /// <param name="audit"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về <c>true</c> nếu việc chèn thành công, <c>false</c> nếu thất bại hoặc có lỗi xảy ra.</returns>
        ///
        public virtual async Task<bool> IsCreateManyAsync(
            IEnumerable<TTable> entities,
            AuditModel audit = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsCreateManyAsync),
                    $"Validate {typeof(TTable).Name} with is null");

                return false;
            }

            List<TTable> result = [];

            foreach (TTable entity in entities)
            {
                TTable data =
                    ProjectToExtensions.SetDataCreatedDefault(entity: entity, audit: audit);

                if (data is not null)
                {
                    result.Add(data);
                }
            }

            if (result.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsCreateManyAsync),
                    $"Validate {typeof(TTable).Name} with SetDataCreatedDefault is null");

                return false;
            }

            await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    await _dbWriteContext.Value.InsertManyAsync(
                        documents: result, cancellationToken: cancellationToken).ConfigureAwait(false);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Xóa một bản ghi trong cơ sở dữ liệu dựa trên điều kiện lọc.
        /// </summary>
        /// <param name="filter">Điều kiện lọc để xác định bản ghi cần xóa.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về true nếu xóa thành công, false nếu có lỗi hoặc điều kiện lọc không hợp lệ.</returns>
        ///
        public virtual async Task<bool> IsDeleteOneAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (filter is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsDeleteOneAsync),
                    $"Validate {typeof(TTable).Name} with is null");

                return false;
            }

            return await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    DeleteResult result =
                        await _dbWriteContext.Value.DeleteOneAsync(
                            filter: filter, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (result is null || result.DeletedCount is 0)
                    {
                        _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsDeleteOneAsync),
                            $"Fail to deleted {typeof(TTable).Name} with {filter.ToJSon()}");

                        return false;
                    }

                    return true;
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Xóa nhiều bản ghi trong cơ sở dữ liệu theo bộ lọc.
        /// </summary>
        /// <typeparam name="TTable">Kiểu dữ liệu của đối tượng cần xóa.</typeparam>
        /// <param name="filter">Biểu thức bộ lọc để xác định các bản ghi cần xóa.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về <c>true</c> nếu việc xóa thành công, <c>false</c> nếu thất bại hoặc có lỗi xảy ra.</returns>
        ///
        public virtual async Task<bool> IsDeleteManyAsync(
            Expression<Func<TTable, bool>> filter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (filter is null)
            {
                _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsDeleteManyAsync),
                    $"Validate {typeof(TTable).Name} with is null");

                return false;
            }

            return await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    DeleteResult result =
                        await _dbWriteContext.Value.DeleteManyAsync(
                            filter: filter, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (result is null || result.DeletedCount is 0)
                    {
                        _logger.FailLogic(nameof(CoreMongoDB<TTable>), nameof(IsDeleteManyAsync),
                            $"Fail to deleted {typeof(TTable).Name} with {filter.ToJSon()}");

                        return false;
                    }

                    return true;
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Lấy thông tin tổng hợp từ cơ sở dữ liệu sử dụng pipeline.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="pipeline"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public virtual async Task<List<TResult>> FindAllWithAggregateAsync<TResult>(
            PipelineDefinition<TTable, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pipeline is null)
            {
                return [];
            }

            options ??= _aggregateOptions;

            using IAsyncCursor<TResult> cursor =
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.AggregateAsync(
                                pipeline: pipeline, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            List<TResult> result = [];

            while (await cursor.MoveNextAsync(cancellationToken: cancellationToken))
            {
                IEnumerable<TResult> data =
                    cursor.Current.Where(item => item is not null);

                if (!data.IsNullOrEmpty())
                {
                    result.AddRange(data);
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin tổng hợp từ cơ sở dữ liệu sử dụng pipeline.
        /// </summary>
        /// <param name="pipeline"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<List<TTable>> FindAllWithAggregateAsync(
            BsonDocument[] pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pipeline is null)
            {
                return [];
            }

            options ??= _aggregateOptions;

            using IAsyncCursor<TTable> cursor =
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.AggregateAsync<TTable>(
                                pipeline: pipeline, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            List<TTable> result = [];

            while (await cursor.MoveNextAsync(cancellationToken: cancellationToken))
            {
                IEnumerable<TTable> data =
                    cursor.Current.Where(item => item is not null);

                if (!data.IsNullOrEmpty())
                {
                    result.AddRange(data);
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin tổng hợp từ cơ sở dữ liệu sử dụng pipeline.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="pipeline"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public virtual async Task<List<TResult>> FindAllWithAggregateAsync<TResult>(
            BsonDocument[] pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pipeline is null)
            {
                return [];
            }

            options ??= _aggregateOptions;

            using IAsyncCursor<TResult> cursor =
                await _pipelineRead.ExecuteAsync(
                    async cancellationToken =>
                    {
                        return
                            await _dbReadContext.Value.AggregateAsync<TResult>(
                                pipeline: pipeline, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);

            List<TResult> result = [];

            while (await cursor.MoveNextAsync(cancellationToken: cancellationToken))
            {
                IEnumerable<TResult> data =
                    cursor.Current.Where(item => item is not null);

                if (!data.IsNullOrEmpty())
                {
                    result.AddRange(data);
                }
            }

            return result;
        }

        /// <summary>
        /// Thực thi hàng loạt lệnh ghi tùy ý (Insert/Update/Delete/Replace) trong một lần gọi MongoDB BulkWrite,
        /// caller tự xây dựng danh sách <see cref="WriteModel{TTable}"/> (bao gồm việc tự bật upsert nếu cần).
        /// </summary>
        /// <param name="requests">Danh sách các lệnh ghi (WriteModel) cần thực thi.</param>
        /// <param name="options">Tùy chọn BulkWrite, mặc định null.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về true nếu server xác nhận (IsAcknowledged) và có bản ghi khớp hoặc được upsert; ngược lại false.</returns>
        ///
        public virtual async Task<bool> BulkWriteAsync(
            IEnumerable<WriteModel<TTable>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _pipelineWrite.ExecuteAsync(
                async cancellationToken =>
                {
                    BulkWriteResult<TTable> result =
                        await _dbWriteContext.Value.BulkWriteAsync(
                            requests: requests,
                            options: options,
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                    return result switch
                    {
                        { IsAcknowledged: false } => false,

                        { MatchedCount: > 0 } => true,

                        { Upserts.Count: > 0 } => true,

                        _ => false
                    };
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion :::::::::::::::::::::::::::::::::: BaseMongoDBRepository Tồn tại <TTable> ::::::::::::::::::::::::::::::::::
}