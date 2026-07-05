using Dapper;
using FTELSRCore.Abstractions;
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
    #region :::::::::::::::::::::::::::::::::: BaseSQLRepository Tồn tại <TEntityFrom, TEntityTo> ::::::::::::::::::::::::::::::::::

    public abstract partial class CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>
        : ICoreSQL<TEntityFrom, DBContextRead, DBContextWrite>
        where TEntityFrom : class
        where TEntityTo : class
        where DBContextRead : ReadDbContext<DBContextRead>
        where DBContextWrite : WriteDbContext<DBContextWrite>
    {
        #region :::::::: Ctor ::::::::

        private const string IsDeleted = "IsDeleted";

        private readonly ILogger<CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>> _logger;

        private readonly ResiliencePipeline _pipelineRead;

        private readonly ResiliencePipeline _pipelineWrite;

        private readonly Lazy<IDapperSQLDBContext> _dapperDbContext;

        private readonly Lazy<IDbContextFactory<DBContextRead>> _dbContextRead;

        private readonly Lazy<IDbContextFactory<DBContextWrite>> _dbContextWrite;

        protected CoreSQL(
            ILogger<CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>> logger,
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

        #endregion :::::::: Ctor ::::::::

        /// <summary>
        /// Thực hiện truy vấn SQL và trả về một đối tượng duy nhất thuộc kiểu <typeparamref name="TDto"/>.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu của đối tượng cần trả về.</typeparam>
        /// <param name="scriptSQLQuery">Truy vấn SQL cần thực thi.</param>
        /// <param name="parameters">Danh sách các tham số cho truy vấn SQL.</param>
        /// <param name="commandTimeout">Thời gian tối đa cho phép thực thi lệnh SQL, mặc định là 30 giây.</param>
        /// <param name="commandType">Kiểu lệnh SQL (Text hoặc StoredProcedure), mặc định là Text.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Đối tượng thuộc kiểu <typeparamref name="TDto"/> được ánh xạ từ dữ liệu truy vấn.</returns>
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
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(FindOneWithScriptAsync), "scriptSQLQuery is empty");

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
                            pSqlQuery: sqlQuery,
                            pParams: parameters,
                            commandType: commandType,
                            commandTimeout: commandTimeout,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Thực hiện truy vấn SQL và trả về danh sách các đối tượng thuộc kiểu <typeparamref name="TDto"/>.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu của các đối tượng cần trả về.</typeparam>
        /// <param name="scriptSQLQuery">Truy vấn SQL cần thực thi.</param>
        /// <param name="parameters">Danh sách các tham số cho truy vấn SQL.</param>
        /// <param name="commandTimeout">Thời gian tối đa cho phép thực thi lệnh SQL, mặc định là 30 giây.</param>
        /// <param name="commandType">Kiểu lệnh SQL (Text hoặc StoredProcedure), mặc định là Text.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// Một danh sách các đối tượng thuộc kiểu <typeparamref name="TDto"/> được ánh xạ từ dữ liệu truy vấn.
        /// </returns>
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
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(FindAllWithScriptAsync), "scriptSQLQuery is empty");

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
        /// Thực hiện truy vấn SQL với kiểu dữ liệu scalar và trả về một đối tượng thuộc kiểu <typeparamref name="TDto"/>.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu của đối tượng cần trả về.</typeparam>
        /// <param name="scriptSQLQuery">Truy vấn SQL cần thực thi.</param>
        /// <param name="parameters">Danh sách các tham số cho truy vấn SQL.</param>
        /// <param name="commandTimeout">Thời gian tối đa cho phép thực thi lệnh SQL, mặc định là 30 giây.</param>
        /// <param name="commandType">Kiểu lệnh SQL (Text hoặc StoredProcedure), mặc định là Text.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Đối tượng thuộc kiểu <typeparamref name="TDto"/> được ánh xạ từ dữ liệu truy vấn.</returns>
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
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(FindOneWithScalarScriptAsync), "scriptSQLQuery is empty");

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
        /// Thực thi một câu lệnh SQL scalar để thực hiện các thao tác SAP (Save, Alter, Purge) trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="scriptSQLQuery">Câu lệnh SQL cần thực thi.</param>
        /// <param name="transaction">Giao dịch cơ sở dữ liệu hiện tại</param>
        /// <param name="parameters">Các tham số động cho câu lệnh SQL.</param>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu.</param>
        /// <param name="commandTimeout">Thời gian chờ thực thi lệnh (mặc định là 30 giây).</param>
        /// <param name="commandType">Loại lệnh SQL (mặc định là CommandType.Text).</param>
        /// <param name="cancellationToken"></param>
        /// <returns> Trả về `true` nếu câu lệnh SQL thực thi thành công (có thay đổi dữ liệu), ngược lại trả về `false`. </returns>
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
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(IsExecuteNonQueryAsync), "scriptSQLQuery is empty");

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
        /// Tìm kiếm một đối tượng theo ID và chuyển đổi kết quả sang kiểu <typeparamref name="TEntityFrom"/>.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng trả về.</typeparam>
        /// <typeparam name="TEntityTo">Kiểu dữ liệu của đối tượng trong cơ sở dữ liệu.</typeparam>
        /// <param name="id">ID của đối tượng cần tìm kiếm.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Đối tượng kiểu <typeparamref name="TEntityFrom"/> sau khi được chuyển đổi từ kết quả tìm kiếm.</returns>
        ///
        public virtual async Task<TEntityFrom> FindByIdAsync(
            object id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (id == null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(FindByIdAsync), "id is null");

                return null;
            }

            TEntityTo result = await _pipelineRead.ExecuteAsync(
                async ct =>
                {
                    await using DBContextRead createDbContext =
                        await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                    TEntityTo entity = await createDbContext.Set<TEntityTo>()
                        .FindAsync(keyValues: [id], cancellationToken: ct).ConfigureAwait(false);

                    if (entity is not null)
                    {
                        createDbContext.Entry(entity).State = EntityState.Detached;
                    }

                    return entity;
                },
                cancellationToken);

            return result?.ProjectTo<TEntityTo, TEntityFrom>();
        }

        /// <summary>
        /// Tìm một đối tượng theo các điều kiện lọc và trạng thái "IsDeleted", sau đó chuyển đổi kết quả sang kiểu <typeparamref name="TDto"/>.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu của đối tượng trả về.</typeparam>
        /// <param name="filters">Mảng các biểu thức điều kiện lọc.</param>
        /// <param name="isDeleted">Trạng thái "IsDeleted" của đối tượng cần tìm (mặc định là false).</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Đối tượng kiểu <typeparamref name="TDto"/> sau khi được chuyển đổi từ kết quả tìm kiếm, hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TDto> FindOneSortDeletedAsync<TDto>(
            Expression<Func<TEntityFrom, bool>>[] filters,
            bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TEntityTo result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

                        query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

                        if (filters is not null && filters.Length > 0)
                        {
                            Expression<Func<TEntityTo, bool>>[] replaced =
                                filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                            query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().FirstOrDefaultAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result is null ? default : result.ProjectTo<TEntityTo, TDto>();
        }

        /// <summary>
        /// Tìm một đối tượng theo các điều kiện lọc và trạng thái "IsDeleted", sau đó trả về đối tượng kiểu <typeparamref name="TEntityFrom"/>.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng trả về.</typeparam>
        /// <param name="filters">Mảng các biểu thức điều kiện lọc.</param>
        /// <param name="isDeleted">Trạng thái "IsDeleted" của đối tượng cần tìm (mặc định là false).</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Đối tượng kiểu <typeparamref name="TEntityFrom"/> nếu tìm thấy, hoặc null nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TEntityFrom> FindOneSortDeletedAsync(
            Expression<Func<TEntityFrom, bool>>[] filters,
            bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TEntityTo result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

                        query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

                        if (filters is not null && filters.Length > 0)
                        {
                            Expression<Func<TEntityTo, bool>>[] replaced =
                                filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                            query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().FirstOrDefaultAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result?.ProjectTo<TEntityTo, TEntityFrom>();
        }

        /// <summary>
        /// Tìm một đối tượng theo các điều kiện lọc và chuyển đổi kết quả sang kiểu <typeparamref name="TDto"/>.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu của đối tượng trả về.</typeparam>
        /// <param name="filters">Mảng các biểu thức điều kiện lọc.</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Đối tượng kiểu <typeparamref name="TDto"/> sau khi được chuyển đổi từ kết quả tìm kiếm, hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TDto> FindOneAsync<TDto>(
            Expression<Func<TEntityFrom, bool>>[] filters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TEntityTo result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

                        if (filters is not null && filters.Length > 0)
                        {
                            Expression<Func<TEntityTo, bool>>[] replaced =
                                filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                            query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().FirstOrDefaultAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result is null ? default : result.ProjectTo<TEntityTo, TDto>();
        }

        /// <summary>
        /// Tìm một đối tượng theo các điều kiện lọc và trả về đối tượng kiểu <typeparamref name="TEntityFrom"/>.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng trả về.</typeparam>
        /// <param name="filters">Mảng các biểu thức điều kiện lọc.</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Đối tượng kiểu <typeparamref name="TEntityFrom"/> nếu tìm thấy, hoặc null nếu không tìm thấy.</returns>
        ///
        public virtual async Task<TEntityFrom> FindOneAsync(
            Expression<Func<TEntityFrom, bool>>[] filters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TEntityTo result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

                        if (filters is not null && filters.Length > 0)
                        {
                            Expression<Func<TEntityTo, bool>>[] replaced =
                                filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                            query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().FirstOrDefaultAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result?.ProjectTo<TEntityTo, TEntityFrom>();
        }

        /// <summary>
        /// Tìm tất cả đối tượng theo các điều kiện lọc và trạng thái "IsDeleted", sau đó chuyển đổi kết quả sang kiểu <typeparamref name="TDto"/>.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu của đối tượng trả về.</typeparam>
        /// <param name="filters">Mảng các biểu thức điều kiện lọc.</param>
        /// <param name="isDeleted">Trạng thái "IsDeleted" của đối tượng cần tìm (mặc định là false).</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Danh sách các đối tượng kiểu <typeparamref name="TDto"/> sau khi được chuyển đổi từ kết quả tìm kiếm, hoặc null nếu không tìm thấy kết quả.</returns>
        ///
        public virtual async Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
            Expression<Func<TEntityFrom, bool>>[] filters,
            bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TEntityTo> result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

                        query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

                        if (filters is not null && filters.Length > 0)
                        {
                            Expression<Func<TEntityTo, bool>>[] replaced =
                                filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                            query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().ToListAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntityTo, TDto>();
        }

        /// <summary>
        /// Tìm tất cả đối tượng theo các điều kiện lọc và trạng thái "IsDeleted", sau đó trả về danh sách các đối tượng kiểu <typeparamref name="TEntityFrom"/>.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng trả về.</typeparam>
        /// <param name="filters">Mảng các biểu thức điều kiện lọc.</param>
        /// <param name="isDeleted">Trạng thái "IsDeleted" của đối tượng cần tìm (mặc định là false).</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Danh sách các đối tượng kiểu <typeparamref name="TEntityFrom"/> nếu tìm thấy, hoặc null nếu không tìm thấy kết quả.</returns>
        ///
        public virtual async Task<List<TEntityFrom>> FindAllSortDeletedAsync(
            Expression<Func<TEntityFrom, bool>>[] filters,
            bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TEntityTo> result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

                        query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

                        if (filters is not null && filters.Length > 0)
                        {
                            Expression<Func<TEntityTo, bool>>[] replaced =
                                filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                            query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().ToListAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntityTo, TEntityFrom>();
        }

        /// <summary>
        /// Tìm tất cả đối tượng theo các điều kiện lọc và chuyển đổi kết quả sang kiểu <typeparamref name="TDto"/>.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu của đối tượng trả về.</typeparam>
        /// <param name="filters">Mảng các biểu thức điều kiện lọc.</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Danh sách các đối tượng kiểu <typeparamref name="TDto"/> sau khi được chuyển đổi từ kết quả tìm kiếm, hoặc null nếu không tìm thấy kết quả.</returns>
        ///
        public virtual async Task<List<TDto>> FindAllAsync<TDto>(
            Expression<Func<TEntityFrom, bool>>[] filters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TEntityTo> result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

                        if (filters is not null && filters.Length > 0)
                        {
                            Expression<Func<TEntityTo, bool>>[] replaced =
                                filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                            query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().ToListAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntityTo, TDto>();
        }

        /// <summary>
        /// Tìm tất cả đối tượng theo các điều kiện lọc và trả về danh sách các đối tượng kiểu <typeparamref name="TEntityFrom"/>.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng trả về.</typeparam>
        /// <param name="filters">Mảng các biểu thức điều kiện lọc.</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Danh sách các đối tượng kiểu <typeparamref name="TEntityFrom"/> nếu tìm thấy, hoặc null nếu không tìm thấy kết quả.</returns>
        ///
        public virtual async Task<List<TEntityFrom>> FindAllAsync(
            Expression<Func<TEntityFrom, bool>>[] filters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TEntityTo> result =
                await _pipelineRead.ExecuteAsync(
                    callback: async ct =>
                    {
                        await using DBContextRead createDbContext =
                            await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: ct);

                        IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

                        if (filters is not null && filters.Length > 0)
                        {
                            Expression<Func<TEntityTo, bool>>[] replaced =
                                filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                            query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
                        }

                        return await query.AsNoTracking().ToListAsync(ct);
                    },
                    cancellationToken: cancellationToken);

            return result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntityTo, TEntityFrom>();
        }

        /// <summary>
        /// Tạo một đối tượng mới trong cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng được tạo.</typeparam>
        /// <param name="entity">Đối tượng cần tạo.</param>
        /// <param name="auditLog">Thông tin ghi log kiểm tra, có thể là null.</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Số lượng bản ghi đã được lưu thành công, mặc định là 0 nếu không thành công.</returns>
        ///
        public virtual async Task<int> CreateAsync(
            TEntityFrom entity,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entity is null");

                return 0;
            }

            IReadOnlyList<IDomainEvent> domainEvents =
                entity is IAggregate entityWithDomainEventsFrom ? entityWithDomainEventsFrom.DomainEvents : [];

            TEntityTo entityConvert =
                ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity);

            if (entityConvert is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entityConvert is null");

                return 0;
            }

            // Thêm các sự kiện vào entityConvert nếu là IAggregate
            if (entityConvert is IAggregate entityWithDomainEventsTo)
            {
                entityWithDomainEventsTo.DomainEvents.AddRange(domainEvents);
            }

            await using DBContextWrite createDbContext =
                await _dbContextWrite.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            int result = 0;

            await createDbContext.Set<TEntityTo>().AddAsync(
                entity: entityConvert, cancellationToken: cancellationToken);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await createDbContext.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Tạo một đối tượng mới trong cơ sở dữ liệu và lưu vào cơ sở dữ liệu theo cùng một transaction.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng ban đầu cần tạo.</typeparam>
        /// <param name="entity">Đối tượng cần tạo trong cơ sở dữ liệu.</param>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu để thực hiện thao tác lưu.</param>
        /// <param name="auditLog">Thông tin ghi log kiểm tra, có thể là null nếu không cần ghi log.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Trả về một tuple bao gồm mã kết quả và dữ liệu đối tượng đã được lưu trong cơ sở dữ liệu.</returns>
        ///
        public virtual async Task<(int Result, TEntityFrom Data)> CreateAsync(
            TEntityFrom entity,
            DBContextWrite context,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entity is null");

                return (Result: 0, Data: null);
            }

            IReadOnlyList<IDomainEvent> domainEvents =
                entity is IAggregate entityWithDomainEventsFrom ? entityWithDomainEventsFrom.DomainEvents : [];

            TEntityTo entityConvert =
                ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity);

            if (entityConvert is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entityConvert is null");

                return (Result: 0, Data: entity);
            }

            // Thêm các sự kiện vào entityConvert nếu là IAggregate
            if (entityConvert is IAggregate entityWithDomainEventsTo)
            {
                entityWithDomainEventsTo.DomainEvents.AddRange(domainEvents);
            }

            int result = 0;

            await context.Set<TEntityTo>().AddAsync(
                entity: entityConvert, cancellationToken: cancellationToken);

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
                        return (Result: result, Data: entityConvert.ProjectTo<TEntityTo, TEntityFrom>());
                    }
            }
        }

        /// <summary>
        /// Tạo một danh sách các đối tượng mới trong cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của các đối tượng được tạo.</typeparam>
        /// <param name="entities">Danh sách các đối tượng cần tạo.</param>
        /// <param name="auditLog">Thông tin ghi log kiểm tra, có thể là null.</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Số lượng bản ghi đã được lưu thành công, mặc định là 0 nếu không thành công.</returns>
        ///
        public virtual async Task<int> CreateAsync(
            IEnumerable<TEntityFrom> entities,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entities is empty");

                return default;
            }

            List<TEntityTo> entitiesConvert = [];

            foreach (TEntityFrom entity in entities)
            {
                IReadOnlyList<IDomainEvent> domainEvents =
                    entity is IAggregate entityWithDomainEventsFrom ? entityWithDomainEventsFrom.DomainEvents : [];

                TEntityTo entityConvert =
                    ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity);

                if (entityConvert is not null)
                {
                    // Thêm các sự kiện vào entityConvert nếu là IAggregate
                    if (entityConvert is IAggregate entityWithDomainEventsTo)
                    {
                        entityWithDomainEventsTo.DomainEvents.AddRange(domainEvents);
                    }

                    entitiesConvert.Add(entityConvert);
                }
            }

            if (entitiesConvert.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entitiesConvert is empty");

                return 0;
            }

            int result = 0;

            await using DBContextWrite createDbContext =
                await _dbContextWrite.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            await createDbContext.Set<TEntityTo>().AddRangeAsync(
                entities: entitiesConvert, cancellationToken: cancellationToken);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await createDbContext.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Tạo danh sách đối tượng mới trong cơ sở dữ liệu theo cùng một transaction.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng được tạo.</typeparam>
        /// <param name="entities">Danh sách các đối tượng cần tạo.</param>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu.</param>
        /// <param name="auditLog">Thông tin ghi log kiểm tra, có thể là null.</param>
        /// <param name="cancellationToken"></param>
        /// <returns> Một tuple gồm số lượng bản ghi đã lưu và danh sách đối tượng đầu vào.</returns>
        ///
        public virtual async Task<(int Result, IEnumerable<TEntityFrom> Data)> CreateAsync(
            IEnumerable<TEntityFrom> entities,
            DBContextWrite context,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entities is empty");

                return (Result: 0, Data: null);
            }

            List<TEntityTo> entitiesConvert = [];

            foreach (TEntityFrom entity in entities)
            {
                IReadOnlyList<IDomainEvent> domainEvents =
                    entity is IAggregate entityWithDomainEventsFrom ? entityWithDomainEventsFrom.DomainEvents : [];

                TEntityTo entityConvert =
                    ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity);

                if (entityConvert is not null)
                {
                    // Thêm các sự kiện vào entityConvert nếu là IAggregate
                    if (entityConvert is IAggregate entityWithDomainEventsTo)
                    {
                        entityWithDomainEventsTo.DomainEvents.AddRange(domainEvents);
                    }

                    entitiesConvert.Add(entityConvert);
                }
            }

            if (entitiesConvert.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(CreateAsync), "entitiesConvert is empty");

                return (Result: 0, Data: null);
            }

            int result = 0;

            await context.Set<TEntityTo>().AddRangeAsync(
                entities: entitiesConvert, cancellationToken: cancellationToken);

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
                        return (Result: 0, Data: []);
                    }
                case false:
                    {
                        return (Result: result, Data: entitiesConvert.ProjectTo<TEntityTo, TEntityFrom>());
                    }
            }
        }

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng cần cập nhật.</typeparam>
        /// <param name="entity">Đối tượng cần cập nhật.</param>
        /// <param name="auditLog">Thông tin ghi log kiểm tra, có thể là null.</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Số lượng bản ghi đã được lưu thành công, mặc định là 0 nếu không thành công.</returns>
        ///
        public virtual async Task<int> UpdateAsync(
            TEntityFrom entity,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entity is null");

                return 0;
            }

            IReadOnlyList<IDomainEvent> domainEvents =
                entity is IAggregate entityWithDomainEventsFrom ? entityWithDomainEventsFrom.DomainEvents : [];

            TEntityTo entityConvert =
                ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity);

            if (entityConvert is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entityConvert is null");

                return 0;
            }

            // Thêm các sự kiện vào entityConvert nếu là IAggregate
            if (entityConvert is IAggregate entityWithDomainEventsTo)
            {
                entityWithDomainEventsTo.DomainEvents.AddRange(domainEvents);
            }

            var result = 0;

            await using DBContextWrite createDbContext =
                await _dbContextWrite.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            createDbContext.Set<TEntityTo>().Update(entity: entityConvert);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await createDbContext.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu theo cùng một transaction.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng cần cập nhật.</typeparam>
        /// <param name="entity">Đối tượng cần cập nhật.</param>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu.</param>
        /// <param name="auditLog">Thông tin ghi log kiểm tra, có thể là null.</param>
        /// <param name="cancellationToken"></param>
        /// <returns> Một tuple gồm số lượng bản ghi đã cập nhật và đối tượng đầu vào. </returns>
        ///
        public virtual async Task<(int Result, TEntityFrom Data)> UpdateAsync(
            TEntityFrom entity,
            DBContextWrite context,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entity is null");

                return (Result: 0, Data: null);
            }

            IReadOnlyList<IDomainEvent> domainEvents =
                entity is IAggregate entityWithDomainEventsFrom ? entityWithDomainEventsFrom.DomainEvents : [];

            TEntityTo entityConvert =
                ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity);

            if (entityConvert is null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entityConvert is null");

                return (Result: 0, Data: entity);
            }

            // Thêm các sự kiện vào entityConvert nếu là IAggregate
            if (entityConvert is IAggregate entityWithDomainEventsTo)
            {
                entityWithDomainEventsTo.DomainEvents.AddRange(domainEvents);
            }

            int result = 0;

            context.Set<TEntityTo>().Update(entity: entityConvert);

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
                        return (Result: result, Data: entityConvert.ProjectTo<TEntityTo, TEntityFrom>());
                    }
            }
        }

        /// <summary>
        /// Cập nhật một danh sách các đối tượng trong cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của các đối tượng cần cập nhật.</typeparam>
        /// <param name="entities">Danh sách các đối tượng cần cập nhật.</param>
        /// <param name="auditLog">Thông tin ghi log kiểm tra, có thể là null.</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
        /// <returns>Số lượng bản ghi đã được lưu thành công, mặc định là 0 nếu không thành công.</returns>
        ///
        public virtual async Task<int> UpdateAsync(
            IEnumerable<TEntityFrom> entities,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entities is empty");

                return 0;
            }

            List<TEntityTo> entitiesConvert = [];

            foreach (TEntityFrom entity in entities)
            {
                IReadOnlyList<IDomainEvent> domainEvents =
                    entity is IAggregate entityWithDomainEventsFrom ? entityWithDomainEventsFrom.DomainEvents : [];

                TEntityTo entityConvert =
                    ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity);

                if (entityConvert is not null)
                {
                    // Thêm các sự kiện vào entityConvert nếu là IAggregate
                    if (entityConvert is IAggregate entityWithDomainEventsTo)
                    {
                        entityWithDomainEventsTo.DomainEvents.AddRange(domainEvents);
                    }

                    entitiesConvert.Add(entityConvert);
                }
            }

            if (entitiesConvert.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entitiesConvert is empty");

                return 0;
            }

            await using DBContextWrite createDbContext =
                await _dbContextWrite.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            int result = 0;

            createDbContext.Set<TEntityTo>().UpdateRange(entities: entitiesConvert);

            result =
                await _pipelineWrite.ExecuteAsync(
                    async ct =>
                        await createDbContext.SaveChangesAsync(
                            audit: auditLog, cancellationToken: ct).ConfigureAwait(false),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Cập nhật danh sách các đối tượng trong cơ sở dữ liệu theo cùng một transaction.
        /// </summary>
        /// <typeparam name="TEntityFrom">Kiểu dữ liệu của đối tượng cần cập nhật.</typeparam>
        /// <param name="entities">Danh sách các đối tượng cần cập nhật.</param>
        /// <param name="context">Ngữ cảnh cơ sở dữ liệu.</param>
        /// <param name="auditLog">Thông tin ghi log kiểm tra, có thể là null.</param>
        /// <param name="cancellationToken"></param>
        /// <returns> Một tuple gồm số lượng bản ghi đã cập nhật và danh sách đối tượng đầu vào. </returns>
        ///
        public virtual async Task<(int Result, IEnumerable<TEntityFrom> Data)> UpdateAsync(
            IEnumerable<TEntityFrom> entities,
            DBContextWrite context,
            AuditModel auditLog = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities.IsNullOrEmpty())
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entities is empty");

                return (Result: 0, Data: null);
            }

            List<TEntityTo> entitiesConvert = [];

            foreach (TEntityFrom entity in entities)
            {
                IReadOnlyList<IDomainEvent> domainEvents =
                    entity is IAggregate entityWithDomainEventsFrom ? entityWithDomainEventsFrom.DomainEvents : [];

                TEntityTo entityConvert =
                    ProjectToExtensions.MapUsingExpression<TEntityFrom, TEntityTo>(entity);

                if (entityConvert is not null)
                {
                    // Thêm các sự kiện vào entityConvert nếu là IAggregate
                    if (entityConvert is IAggregate entityWithDomainEventsTo)
                    {
                        entityWithDomainEventsTo.DomainEvents.AddRange(domainEvents);
                    }

                    entitiesConvert.Add(entityConvert);
                }
            }

            if (entitiesConvert is null || entitiesConvert.Count <= 0)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(UpdateAsync), "entitiesConvert is empty");

                return (Result: 0, Data: null);
            }

            int result = 0;

            context.Set<TEntityTo>().UpdateRange(entities: entitiesConvert);

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
                        return (Result: result, Data: entitiesConvert.ProjectTo<TEntityTo, TEntityFrom>());
                    }
            }
        }
    }

    #endregion :::::::::::::::::::::::::::::::::: BaseSQLRepository Tồn tại <TEntityFrom, TEntityTo> ::::::::::::::::::::::::::::::::::
}
