using Dapper;
using FTELSRCore.Data.SQL.Dapper;
using FTELSRCore.Data.SQL.DbContexts.Read;
using FTELSRCore.Data.SQL.DbContexts.Write;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using static Dapper.SqlMapper;

namespace FTELSRCore.Data.SQL.Core
{
    #region :::::::::::::::::::::::::::::::::: BaseSQLRepository Tồn tại <TEntity> ::::::::::::::::::::::::::::::::::

    public abstract partial class CoreSQL<TEntity, DBContextRead, DBContextWrite> : ICoreSQL<TEntity, DBContextRead, DBContextWrite>
        where TEntity : class
        where DBContextRead : ReadDbContext<DBContextRead>
        where DBContextWrite : WriteDbContext<DBContextWrite>
    {
        private readonly ILogger<CoreSQL<TEntity, DBContextRead, DBContextWrite>> _logger;

        private readonly ResiliencePipeline _pipelineRead;

        private readonly ResiliencePipeline _pipelineWrite;

        private const string IsDeleted = "IsDeleted";

        private readonly Lazy<IDapperSQLDBContext> _dapperDbContext;

        private readonly Lazy<IDbContextFactory<DBContextRead>> _dbContextRead;

        private readonly Lazy<IDbContextFactory<DBContextWrite>> _dbContextWrite;

        protected CoreSQL(
            ILogger<CoreSQL<TEntity, DBContextRead, DBContextWrite>> logger,
            Lazy<IDapperSQLDBContext> dapperDbContext,
            Lazy<IDbContextFactory<DBContextRead>> contextRead,
            Lazy<IDbContextFactory<DBContextWrite>> contextWrite,
            ResiliencePipeline pipelineRead, ResiliencePipeline pipelineWrite)
        {
            _logger = logger;

            _dbContextRead = contextRead;

            _dbContextWrite = contextWrite;

            _dapperDbContext = dapperDbContext;

            _pipelineRead = pipelineRead;

            _pipelineWrite = pipelineWrite;
        }

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
        public virtual async Task<TDto> FindOneWithScriptAsync<TDto>(
            string scriptSQLQuery,
            DynamicParameters parameters,
            int commandTimeout = 30,
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(scriptSQLQuery))
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(FindOneWithScriptAsync), "scriptSQLQuery is empty");

                return default;
            }

            string sqlQuery = commandType switch
            {
                CommandType.Text => @$"
                    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    {scriptSQLQuery}
                ",
                _ => scriptSQLQuery
            };

            return await _pipelineRead.ExecuteAsync(
                async cancellationToken =>
                {
                    return
                        await _dapperDbContext.Value.GetOne<TDto>(
                            pParams: parameters,
                            pSqlQuery: sqlQuery,
                            commandType: commandType,
                            commandTimeout: commandTimeout,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

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
        public virtual async Task<IEnumerable<TDto>> FindAllWithScriptAsync<TDto>(
            string scriptSQLQuery,
            DynamicParameters parameters,
            int commandTimeout = 30,
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(scriptSQLQuery))
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(FindAllWithScriptAsync), "scriptSQLQuery is empty");

                return null;
            }

            string sqlQuery = commandType switch
            {
                CommandType.Text => @$"
                    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    {scriptSQLQuery}
                ",
                _ => scriptSQLQuery
            };

            return await _pipelineRead.ExecuteAsync(
                async cancellationToken =>
                {
                    return
                        await _dapperDbContext.Value.GetAll<TDto>(
                            pParams: parameters,
                            pSqlQuery: sqlQuery,
                            commandType: commandType,
                            commandTimeout: commandTimeout,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

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
        public virtual async Task<TDto> FindOneWithScalarScriptAsync<TDto>(
            string scriptSQLQuery,
            DynamicParameters parameters,
            int commandTimeout = 30,
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(scriptSQLQuery))
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(FindOneWithScalarScriptAsync), "scriptSQLQuery is empty");

                return default;
            }

            string sqlQuery = commandType switch
            {
                CommandType.Text => @$"
                    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    {scriptSQLQuery}
                ",
                _ => scriptSQLQuery
            };

            return await _pipelineRead.ExecuteAsync(
                async cancellationToken =>
                {
                    return
                        await _dapperDbContext.Value.GetOneExecute<TDto>(
                             pParams: parameters,
                             commandType: commandType,
                             pSqlQuery: sqlQuery,
                             commandTimeout: commandTimeout,
                             cancellationToken: cancellationToken).ConfigureAwait(false);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

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
        public virtual async Task<bool> IsExecuteNonQueryAsync(
            string scriptSQLQuery,
            DbConnection context,
            DbTransaction transaction,
            DynamicParameters parameters,
            int commandTimeout = 30,
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(scriptSQLQuery))
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(IsExecuteNonQueryAsync), "scriptSQLQuery is empty");

                return false;
            }

            return await context.ExecuteAsync(
                param: parameters,
                sql: scriptSQLQuery,
                commandType: commandType,
                transaction: transaction,
                commandTimeout: commandTimeout).ConfigureAwait(false) > 0;
        }

        /// <summary>
        /// Tìm một thực thể theo ID.
        /// </summary>
        /// <param name="id">ID của thực thể cần tìm.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Thực thể tìm thấy, hoặc giá trị null nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TEntity> FindByIdAsync(
            object id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (id == null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(FindByIdAsync), "id is null");

                return null;
            }

            return await _pipelineRead.ExecuteAsync(
                callback: async ct =>
                {
                    await using DBContextRead createDbContext =
                        await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                    TEntity entity = await createDbContext.Set<TEntity>()
                        .FindAsync(keyValues: [id], cancellationToken: ct).ConfigureAwait(false);

                    if (entity is not null)
                    {
                        createDbContext.Entry(entity).State = EntityState.Detached;
                    }

                    return entity;
                }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc và trạng thái "isDeleted" đã chỉ định, sau đó chuyển đổi nó thành đối tượng DTO.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="isDeleted">Trạng thái "isDeleted" của thực thể (true hoặc false).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Đối tượng DTO của thực thể đầu tiên thỏa mãn bộ lọc và trạng thái "isDeleted", hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TDto> FindOneSortDeletedAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters,
            bool isDeleted = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TEntity result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntity> query = createDbContext.Set<TEntity>();

                        query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

                        if (filters is not null && filters.Length > 0)
                        {
                            query = filters.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().FirstOrDefaultAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result is null ? default : result.ProjectTo<TEntity, TDto>();
        }

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc và trạng thái "isDeleted" đã chỉ định, có hỗ trợ sắp xếp và chiếu (projection) trực tiếp trên server.
        /// Đây là overload bổ sung, KHÔNG thuộc <see cref="ICoreSQL{TEntity, DBContextRead, DBContextWrite}"/> — chỉ tồn tại trên <see cref="CoreSQL{TEntity, DBContextRead, DBContextWrite}"/>
        /// vì <paramref name="sorting"/> là một delegate đã biên dịch (không phải expression tree) nên không thể "dịch" tham số kiểu như <c>ReplaceParameters&lt;TFrom,TTo&gt;</c>
        /// để tái sử dụng cho lớp CoreSQL&lt;TEntityFrom, TEntityTo,...&gt; (ánh xạ TEntityFrom -&gt; TEntityTo).
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="sorting">Hàm sắp xếp áp dụng lên query trước khi lấy bản ghi đầu tiên, ví dụ: q =&gt; q.OrderByDescending(x =&gt; x.CreatedDate).</param>
        /// <param name="isDeleted">Trạng thái "isDeleted" của thực thể (true hoặc false).</param>
        /// <param name="selector">Biểu thức chiếu (projection) trực tiếp sang <typeparamref name="TDto"/> trên server (tuỳ chọn). Nếu không truyền, kết quả sẽ được ánh xạ bằng <see cref="ProjectToExtensions.ProjectTo{TEntity, TDto}(TEntity)"/> sau khi lấy về.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Đối tượng DTO của thực thể đầu tiên thỏa mãn bộ lọc và trạng thái "isDeleted" sau khi sắp xếp, hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TDto> FindOneSortDeletedAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sorting,
            bool isDeleted = false,
            Expression<Func<TEntity, TDto>> selector = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _pipelineRead.ExecuteAsync(
                callback: async ct =>
                {
                    await using DBContextRead createDbContext =
                        await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                    IQueryable<TEntity> query = createDbContext.Set<TEntity>();

                    query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

                    if (filters is not null && filters.Length > 0)
                    {
                        query = filters.Aggregate(query, (current, filter) => current.Where(filter));
                    }

                    if (sorting is not null)
                    {
                        query = sorting(query);
                    }

                    IQueryable<TEntity> readOnlyQuery = query.AsNoTracking();

                    if (selector is not null)
                    {
                        return await readOnlyQuery.Select(selector).FirstOrDefaultAsync(ct);
                    }

                    TEntity result = await readOnlyQuery.FirstOrDefaultAsync(ct);

                    return result is null ? default : result.ProjectTo<TEntity, TDto>();
                },
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc và trạng thái "isDeleted" đã chỉ định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="isDeleted">Trạng thái "isDeleted" của thực thể (true hoặc false).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Thực thể đầu tiên thỏa mãn bộ lọc và trạng thái "isDeleted", hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TEntity> FindOneSortDeletedAsync(
            Expression<Func<TEntity, bool>>[] filters,
            bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _pipelineRead.ExecuteAsync(
                callback: async ct =>
                {
                    await using DBContextRead createDbContext =
                        await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                    IQueryable<TEntity> query = createDbContext.Set<TEntity>();

                    query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

                    if (filters is not null && filters.Length > 0)
                    {
                        query = filters.Aggregate(query, (current, filter) => current.Where(filter));
                    }

                    return await query.AsNoTracking().FirstOrDefaultAsync(ct);
                },
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc đã chỉ định và chuyển đổi nó sang kiểu dữ liệu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu DTO mà thực thể sẽ được chuyển đổi thành.</typeparam>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>DTO của thực thể đầu tiên thỏa mãn bộ lọc, hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TDto> FindOneAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TEntity result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntity> query = createDbContext.Set<TEntity>();

                        if (filters is not null && filters.Length > 0)
                        {
                            query = filters.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().FirstOrDefaultAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result is null ? default : result.ProjectTo<TEntity, TDto>();
        }

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc đã chỉ định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Thực thể đầu tiên thỏa mãn bộ lọc, hoặc null nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TEntity> FindOneAsync(
            Expression<Func<TEntity, bool>>[] filters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _pipelineRead.ExecuteAsync(
                callback: async ct =>
                {
                    await using DBContextRead createDbContext =
                        await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                    IQueryable<TEntity> query = createDbContext.Set<TEntity>();

                    if (filters is not null && filters.Length > 0)
                    {
                        query = filters.Aggregate(query, (current, filter) => current.Where(filter));
                    }

                    return await query.AsNoTracking().FirstOrDefaultAsync(ct);
                },
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Tìm tất cả các thực thể trong cơ sở dữ liệu thỏa mãn các bộ lọc đã chỉ định và có trạng thái xóa xác định, và chuyển đổi chúng thành kiểu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO mà kết quả sẽ được chuyển đổi sang.</typeparam>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm các thực thể.</param>
        /// <param name="isDeleted">Trạng thái xóa của các thực thể cần tìm (true nếu xóa, false nếu chưa xóa).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Danh sách các đối tượng DTO thỏa mãn bộ lọc và trạng thái xóa.</returns>
        ///
        public virtual async Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters,
            bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TEntity> result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntity> query = createDbContext.Set<TEntity>();

                        query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

                        if (filters is not null && filters.Length > 0)
                        {
                            query = filters.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().ToListAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntity, TDto>();
        }

        /// <summary>
        /// Tìm tất cả các thực thể trong cơ sở dữ liệu thỏa mãn các bộ lọc đã chỉ định và có trạng thái xóa xác định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm các thực thể.</param>
        /// <param name="isDeleted">Trạng thái xóa của các thực thể cần tìm (true nếu xóa, false nếu chưa xóa).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Danh sách các thực thể thỏa mãn bộ lọc và trạng thái xóa.</returns>
        ///
        public virtual async Task<List<TEntity>> FindAllSortDeletedAsync(
            Expression<Func<TEntity, bool>>[] filters,
            bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TEntity> result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntity> query = createDbContext.Set<TEntity>();

                        query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

                        if (filters is not null && filters.Length > 0)
                        {
                            query = filters.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().ToListAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result.IsNullOrEmpty() ? [] : result;
        }

        /// <summary>
        /// Tìm tất cả các thực thể trong cơ sở dữ liệu thỏa mãn các bộ lọc đã chỉ định và chuyển đổi chúng thành kiểu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu DTO để chuyển đổi các thực thể.</typeparam>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm các thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Danh sách các đối tượng DTO thỏa mãn bộ lọc.</returns>
        ///
        public virtual async Task<List<TDto>> FindAllAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TEntity> result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntity> query = createDbContext.Set<TEntity>();

                        if (filters is not null && filters.Length > 0)
                        {
                            query = filters.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().ToListAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntity, TDto>();
        }

        /// <summary>
        /// Tìm tất cả các thực thể trong cơ sở dữ liệu thỏa mãn các bộ lọc đã chỉ định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm các thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Danh sách các thực thể thỏa mãn bộ lọc.</returns>
        ///
        public virtual async Task<List<TEntity>> FindAllAsync(
            Expression<Func<TEntity, bool>>[] filters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TEntity> result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntity> query = createDbContext.Set<TEntity>();

                        if (filters is not null && filters.Length > 0)
                        {
                            query = filters.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().ToListAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result.IsNullOrEmpty() ? [] : result;
        }

        /// <summary>
        /// Thêm một thực thể vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entity">Thực thể cần thêm.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tuỳ chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Số lượng bản ghi đã được thêm vào cơ sở dữ liệu.</returns>
        ///
        public virtual async Task<int> CreateAsync(
            TEntity entity,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entity is null");

                return 0;
            }

            int result = 0;

            await using DBContextWrite createDbContext =
                await _dbContextWrite.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            await createDbContext.Set<TEntity>().AddAsync(
                entity: entity, cancellationToken: cancellationToken);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await createDbContext.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Thêm một thực thể vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entity">Thực thể cần thêm.</param>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tuỳ chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Tuple chứa số lượng thực thể đã thêm và thực thể đã thêm.</returns>
        ///
        public virtual async Task<(int Result, TEntity Data)> CreateAsync(
            TEntity entity,
            DBContextWrite context,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entity is null");

                return (Result: 0, Data: null);
            }

            int result = 0;

            await context.Set<TEntity>().AddAsync(entity: entity, cancellationToken: cancellationToken);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await context.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return result is 0
                ? (Result: 0, Data: entity)
                : (Result: result, Data: entity);
        }

        /// <summary>
        /// Thêm một tập hợp thực thể vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entities">Danh sách các thực thể cần thêm.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tuỳ chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Số lượng thực thể đã thêm vào.</returns>
        ///
        public virtual async Task<int> CreateAsync(
            IEnumerable<TEntity> entities,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entities is empty");

                return 0;
            }

            int result = 0;

            await using DBContextWrite createDbContext =
                 await _dbContextWrite.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            await createDbContext.Set<TEntity>().AddRangeAsync(
                entities: entities, cancellationToken: cancellationToken);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await createDbContext.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Thêm một tập hợp thực thể mới vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="entities">Danh sách thực thể cần tạo.</param>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu để thực thi thao tác.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns> Trả về kết quả bao gồm số lượng bản ghi bị ảnh hưởng và danh sách thực thể đã tạo.</returns>
        ///
        public virtual async Task<(int Result, IEnumerable<TEntity> Data)> CreateAsync(
            IEnumerable<TEntity> entities,
            DBContextWrite context,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entities is empty");

                return (Result: 0, Data: null);
            }

            int result = 0;

            await context.Set<TEntity>().AddRangeAsync(
                entities: entities, cancellationToken: cancellationToken);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await context.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            switch (result is 0)
            {
                case true:
                    {
                        return (Result: 0, Data: entities);
                    }
                case false:
                    {
                        return (Result: result, Data: entities);
                    }
            }
        }

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
        public virtual async Task<int> UpdateAsync(
            TEntity entity,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entity is null");

                return 0;
            }

            int result = 0;

            await using DBContextWrite createDbContext =
                await _dbContextWrite.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            createDbContext.Set<TEntity>().Update(entity: entity);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await createDbContext.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Cập nhật một thực thể trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="entity">Thực thể cần cập nhật.</param>
        /// <param name="context">Đối tượng ngữ cảnh ghi dữ liệu.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns>Một tuple chứa kết quả cập nhật và thực thể đã được cập nhật.</returns>
        ///
        public virtual async Task<(int Result, TEntity Data)> UpdateAsync(
            TEntity entity,
            DBContextWrite context,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entity is null");

                return (Result: 0, Data: null);
            }

            int result = 0;

            context.Set<TEntity>().Update(entity: entity);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await context.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            switch (result is 0)
            {
                case true:
                    {
                        return (Result: 0, Data: entity);
                    }
                case false:
                    {
                        return (Result: result, Data: entity);
                    }
            }
        }

        /// <summary>
        /// Cập nhật nhiều thực thể trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="entities">Danh sách thực thể cần cập nhật.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns>Số lượng bản ghi được cập nhật.</returns>
        ///
        public virtual async Task<int> UpdateAsync(
            IEnumerable<TEntity> entities,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entities is empty");

                return 0;
            }

            int result = 0;

            await using DBContextWrite createDbContext =
                 await _dbContextWrite.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            createDbContext.Set<TEntity>().UpdateRange(entities: entities);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await createDbContext.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Cập nhật một tập hợp thực thể trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="entities">Danh sách thực thể cần cập nhật.</param>
        /// <param name="context">Đối tượng ngữ cảnh ghi dữ liệu.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns> Một tuple chứa kết quả cập nhật (số bản ghi bị ảnh hưởng) và danh sách thực thể đã được cập nhật.</returns>
        ///
        public virtual async Task<(int Result, IEnumerable<TEntity> Data)> UpdateAsync(
            IEnumerable<TEntity> entities,
            DBContextWrite context,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entities is empty");

                return (Result: 0, Data: null);
            }

            int result = 0;

            context.Set<TEntity>().UpdateRange(entities: entities);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await context.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            switch (result is 0)
            {
                case true:
                    {
                        return (Result: 0, Data: entities);
                    }
                case false:
                    {
                        return (Result: result, Data: entities);
                    }
            }
        }
    }

    #endregion :::::::::::::::::::::::::::::::::: BaseSQLRepository Tồn tại <TEntity> ::::::::::::::::::::::::::::::::::
}