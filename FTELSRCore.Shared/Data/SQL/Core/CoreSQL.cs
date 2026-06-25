using Dapper;
using FTELSRCore.Abstractions;
using FTELSRCore.Data.SQL.Dapper;
using FTELSRCore.Data.SQL.DbContexts.Read;
using FTELSRCore.Data.SQL.DbContexts.Write;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Polly;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using static Dapper.SqlMapper;

namespace FTELSRCore.Data.SQL.Core
{
    #region :::::::::::::::::::::::::::::::::: BaseSQLRepository Tồn tại <TEntity> ::::::::::::::::::::::::::::::::::

    public class CoreSQL<TEntity, DBContextRead, DBContextWrite> : ICoreSQL<TEntity, DBContextRead, DBContextWrite>
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
        public async Task<TDto> FindOneWithScriptAsync<TDto>(
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

            string sqlQuery = @$"
                SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                BEGIN TRANSACTION;

                {scriptSQLQuery}

                COMMIT;
            ";

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
        public async Task<IEnumerable<TDto>> FindAllWithScriptAsync<TDto>(
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

            string sqlQuery = @$"
                SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                BEGIN TRANSACTION;

                {scriptSQLQuery}

                COMMIT;
            ";

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
        public async Task<TDto> FindOneWithScalarScriptAsync<TDto>(
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

            return await _pipelineRead.ExecuteAsync(
                async cancellationToken =>
                {
                    return
                        await _dapperDbContext.Value.GetOneExecute<TDto>(
                             pParams: parameters,
                             commandType: commandType,
                             pSqlQuery: scriptSQLQuery,
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
        public async Task<bool> IsSAPWithScalarScriptAsync(
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
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(IsSAPWithScalarScriptAsync), "scriptSQLQuery is empty");

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
        public async Task<TEntity> FindByIdAsync(
            object id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (id == null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntity, DBContextRead, DBContextWrite>), nameof(FindByIdAsync), "id is null");

                return null;
            }

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            DbSet<TEntity> dbReadSet = createDbContext.Set<TEntity>();

            return await _pipelineRead.ExecuteAsync(
                async cancellationToken =>
                {
                    return
                        await dbReadSet.FindAsync(
                            keyValues: [id], cancellationToken: cancellationToken).ConfigureAwait(false);
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc và trạng thái "isDeleted" đã chỉ định, sau đó chuyển đổi nó thành đối tượng DTO.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="isDeleted">Trạng thái "isDeleted" của thực thể (true hoặc false).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Đối tượng DTO của thực thể đầu tiên thỏa mãn bộ lọc và trạng thái "isDeleted", hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        public async Task<TDto> FindOneSortDeletedAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters, bool isDeleted = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntity> query = createDbContext.Set<TEntity>();

            query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

            if (filters is not null && filters.Length > 0)
            {
                query = filters.Aggregate(query, (current, filter) => current.Where(filter));
            }

            TEntity result = null;

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().FirstOrDefaultAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

            return result is null ? default : result.ProjectTo<TEntity, TDto>();
        }

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc và trạng thái "isDeleted" đã chỉ định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="isDeleted">Trạng thái "isDeleted" của thực thể (true hoặc false).</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Thực thể đầu tiên thỏa mãn bộ lọc và trạng thái "isDeleted", hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        public async Task<TEntity> FindOneSortDeletedAsync(
            Expression<Func<TEntity, bool>>[] filters, bool isDeleted = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                 await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntity> query = createDbContext.Set<TEntity>();

            query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

            if (filters is not null && filters.Length > 0)
            {
                query = filters.Aggregate(query, (current, filter) => current.Where(filter));
            }

            TEntity result = null;

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().FirstOrDefaultAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken);
                });

            return result;
        }

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc đã chỉ định và chuyển đổi nó sang kiểu dữ liệu DTO.
        /// </summary>
        /// <typeparam name="TDto">Kiểu dữ liệu DTO mà thực thể sẽ được chuyển đổi thành.</typeparam>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>DTO của thực thể đầu tiên thỏa mãn bộ lọc, hoặc giá trị mặc định nếu không tìm thấy.</returns>
        ///
        public async Task<TDto> FindOneAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                 await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntity> query = createDbContext.Set<TEntity>();

            if (filters is not null && filters.Length > 0)
            {
                query = filters.Aggregate(query, (current, filter) => current.Where(filter));
            }

            TEntity result = null;

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction =
                    await createDbContext.Database.BeginTransactionAsync(
                        isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                result =
                    await _pipelineRead.ExecuteAsync(
                        async cancellationToken =>
                        {
                            return
                                await query.AsNoTracking().FirstOrDefaultAsync(
                                    cancellationToken: cancellationToken).ConfigureAwait(false);
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken: cancellationToken);
            });

            return result is null ? default : result.ProjectTo<TEntity, TDto>();
        }

        /// <summary>
        /// Tìm một thực thể đầu tiên thỏa mãn các bộ lọc đã chỉ định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Thực thể đầu tiên thỏa mãn bộ lọc, hoặc null nếu không tìm thấy.</returns>
        ///
        public async Task<TEntity> FindOneAsync(
            Expression<Func<TEntity, bool>>[] filters, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                 await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntity> query = createDbContext.Set<TEntity>();

            if (filters is not null && filters.Length > 0)
            {
                query = filters.Aggregate(query, (current, filter) => current.Where(filter));
            }

            TEntity result = null;

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().FirstOrDefaultAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

            return result;
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
        public async Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters, bool isDeleted = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                 await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntity> query = createDbContext.Set<TEntity>();

            query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

            if (filters is not null && filters.Length > 0)
            {
                query = filters.Aggregate(query, (current, filter) => current.Where(filter));
            }

            List<TEntity> result = [];

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().ToListAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

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
        public async Task<List<TEntity>> FindAllSortDeletedAsync(
            Expression<Func<TEntity, bool>>[] filters, bool isDeleted = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntity> query = createDbContext.Set<TEntity>();

            query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

            if (filters is not null && filters.Length > 0)
            {
                query = filters.Aggregate(query, (current, filter) => current.Where(filter));
            }

            List<TEntity> result = [];

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().ToListAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken);
                });

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
        public async Task<List<TDto>> FindAllAsync<TDto>(
            Expression<Func<TEntity, bool>>[] filters, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                 await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntity> query = createDbContext.Set<TEntity>();

            if (filters is not null && filters.Length > 0)
            {
                query = filters.Aggregate(query, (current, filter) => current.Where(filter));
            }

            List<TEntity> result = [];

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().ToListAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

            return result.IsNullOrEmpty() ? [] : result.ProjectTo<TEntity, TDto>();
        }

        /// <summary>
        /// Tìm tất cả các thực thể trong cơ sở dữ liệu thỏa mãn các bộ lọc đã chỉ định.
        /// </summary>
        /// <param name="filters">Các bộ lọc được áp dụng để tìm kiếm các thực thể.</param>
        /// <param name="cancellationToken">Token hủy bỏ thao tác (tuỳ chọn).</param>
        /// <returns>Danh sách các thực thể thỏa mãn bộ lọc.</returns>
        ///
        public async Task<List<TEntity>> FindAllAsync(
            Expression<Func<TEntity, bool>>[] filters, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                 await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntity> query = createDbContext.Set<TEntity>();

            if (filters is not null && filters.Length > 0)
            {
                query = filters.Aggregate(query, (current, filter) => current.Where(filter));
            }

            List<TEntity> result = [];

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction =
                    await createDbContext.Database.BeginTransactionAsync(
                        isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                result =
                    await _pipelineRead.ExecuteAsync(
                        async cancellationToken =>
                        {
                            return
                                await query.AsNoTracking().ToListAsync(
                                    cancellationToken: cancellationToken).ConfigureAwait(false);
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken: cancellationToken);
            });

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
        public async Task<int> CreateAsync(
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

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await createDbContext.Set<TEntity>().AddAsync(
                        entity: entity, cancellationToken: cancellationToken);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await createDbContext.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

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
        public async Task<(int Result, TEntity Data)> CreateAsync(
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

            await context.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await context.Set<TEntity>().AddAsync(entity: entity, cancellationToken: cancellationToken);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await context.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

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
        public async Task<int> CreateAsync(
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

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await createDbContext.Set<TEntity>().AddRangeAsync(
                    entities: entities, cancellationToken: cancellationToken);

                result =
                    await _pipelineWrite.ExecuteAsync(
                        async cancellationToken =>
                        {
                            return
                                await createDbContext.SaveChangesAsync(
                                    audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);
            });

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
        public async Task<(int Result, IEnumerable<TEntity> Data)> CreateAsync(
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

            await context.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await context.Set<TEntity>().AddRangeAsync(
                        entities: entities, cancellationToken: cancellationToken);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await context.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

            return result is 0
                ? (Result: 0, Data: entities)
                : (Result: result, Data: entities);
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
        public async Task<int> UpdateAsync(
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

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    createDbContext.Set<TEntity>().Update(entity: entity);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await createDbContext.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

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
        public async Task<(int Result, TEntity Data)> UpdateAsync(
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

            await context.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    context.Set<TEntity>().Update(entity: entity);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await context.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

            return result is 0
                ? (Result: 0, Data: entity)
                : (Result: result, Data: entity);
        }

        /// <summary>
        /// Cập nhật nhiều thực thể trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="entities">Danh sách thực thể cần cập nhật.</param>
        /// <param name="auditLog">Nhật ký kiểm toán (tùy chọn).</param>
        /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
        /// <returns>Số lượng bản ghi được cập nhật.</returns>
        ///
        public async Task<int> UpdateAsync(
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

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    createDbContext.Set<TEntity>().UpdateRange(entities: entities);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await createDbContext.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

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
        public async Task<(int Result, IEnumerable<TEntity> Data)> UpdateAsync(
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

            await context.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    context.Set<TEntity>().UpdateRange(entities: entities);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                     await context.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

            return result is 0
                ? (Result: 0, Data: entities)
                : (Result: result, Data: entities);
        }
    }

    #endregion :::::::::::::::::::::::::::::::::: BaseSQLRepository Tồn tại <TEntity> ::::::::::::::::::::::::::::::::::

    #region :::::::::::::::::::::::::::::::::: BaseSQLRepository Tồn tại <TEntityFrom, TEntityTo> ::::::::::::::::::::::::::::::::::

    public class CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>
        : ICoreSQL<TEntityFrom, DBContextRead, DBContextWrite>
        where TEntityFrom : class
        where TEntityTo : class
        where DBContextRead : ReadDbContext<DBContextRead>
        where DBContextWrite : WriteDbContext<DBContextWrite>
    {
        private readonly ILogger<CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>> _logger;

        private readonly ResiliencePipeline _pipelineRead;

        private readonly ResiliencePipeline _pipelineWrite;

        private const string IsDeleted = "IsDeleted";

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
        public async Task<TDto> FindOneWithScriptAsync<TDto>(
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

            string sqlQuery = @$"
                SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                BEGIN TRANSACTION;

                {scriptSQLQuery}

                COMMIT;
            ";

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
        public async Task<IEnumerable<TDto>> FindAllWithScriptAsync<TDto>(
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

            string sqlQuery = @$"
                SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                BEGIN TRANSACTION;

                {scriptSQLQuery}

                COMMIT;
            ";

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
        public async Task<TDto> FindOneWithScalarScriptAsync<TDto>(
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

            return await _pipelineRead.ExecuteAsync(
                async cancellationToken =>
                {
                    return
                        await _dapperDbContext.Value.GetOneExecute<TDto>(
                            pParams: parameters,
                            commandType: commandType,
                            pSqlQuery: scriptSQLQuery,
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
        public async Task<bool> IsSAPWithScalarScriptAsync(
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
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(IsSAPWithScalarScriptAsync), "scriptSQLQuery is empty");

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
        public async Task<TEntityFrom> FindByIdAsync(
            object id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (id == null)
            {
                _logger.FailLogic(nameof(CoreSQL<TEntityFrom, TEntityTo, DBContextRead, DBContextWrite>), nameof(FindByIdAsync), "id is null");

                return null;
            }

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            TEntityTo result = null;

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result = await _pipelineRead.ExecuteAsync(
                        async cancellationToken =>
                        {
                            return
                                await createDbContext.Set<TEntityTo>().FindAsync(
                                    keyValues: [id], cancellationToken: cancellationToken).ConfigureAwait(false);
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

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
        public async Task<TDto> FindOneSortDeletedAsync<TDto>(
            Expression<Func<TEntityFrom, bool>>[] filters, bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

            query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

            if (filters is not null && filters.Length > 0)
            {
                Expression<Func<TEntityTo, bool>>[] replaced =
                    filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
            }

            TEntityTo result = null;

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result = await _pipelineRead.ExecuteAsync(
                        async cancellationToken =>
                        {
                            return
                                await query.AsNoTracking().FirstOrDefaultAsync(
                                    cancellationToken: cancellationToken).ConfigureAwait(false);
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

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
        public async Task<TEntityFrom> FindOneSortDeletedAsync(
            Expression<Func<TEntityFrom, bool>>[] filters, bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

            query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

            if (filters is not null && filters.Length > 0)
            {
                Expression<Func<TEntityTo, bool>>[] replaced =
                    filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
            }

            TEntityTo result = null;

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().FirstOrDefaultAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken);
                });

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
        public async Task<TDto> FindOneAsync<TDto>(
            Expression<Func<TEntityFrom, bool>>[] filters, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

            if (filters is not null && filters.Length > 0)
            {
                Expression<Func<TEntityTo, bool>>[] replaced =
                    filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
            }

            TEntityTo result = null;

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().FirstOrDefaultAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

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
        public async Task<TEntityFrom> FindOneAsync(
            Expression<Func<TEntityFrom, bool>>[] filters, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

            if (filters is not null && filters.Length > 0)
            {
                Expression<Func<TEntityTo, bool>>[] replaced =
                    filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
            }

            TEntityTo result = null;

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().FirstOrDefaultAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

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
        public async Task<List<TDto>> FindAllSortDeletedAsync<TDto>(
            Expression<Func<TEntityFrom, bool>>[] filters, bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

            query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

            if (filters is not null && filters.Length > 0)
            {
                Expression<Func<TEntityTo, bool>>[] replaced =
                    filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
            }

            List<TEntityTo> result = [];

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction =
                    await createDbContext.Database.BeginTransactionAsync(
                        isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                result =
                    await _pipelineRead.ExecuteAsync(
                        async cancellationToken =>
                        {
                            return
                                await query.AsNoTracking().ToListAsync(
                                    cancellationToken: cancellationToken).ConfigureAwait(false);
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken: cancellationToken);
            });

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
        public async Task<List<TEntityFrom>> FindAllSortDeletedAsync(
            Expression<Func<TEntityFrom, bool>>[] filters, bool isDeleted = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

            query = query.Where(x => EF.Property<bool>(x, IsDeleted) == isDeleted);

            if (filters is not null && filters.Length > 0)
            {
                Expression<Func<TEntityTo, bool>>[] replaced =
                    filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
            }

            List<TEntityTo> result = [];

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().ToListAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

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
        public async Task<List<TDto>> FindAllAsync<TDto>(
            Expression<Func<TEntityFrom, bool>>[] filters, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

            if (filters is not null && filters.Length > 0)
            {
                Expression<Func<TEntityTo, bool>>[] replaced =
                    filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
            }

            List<TEntityTo> result = [];

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().ToListAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken: cancellationToken);
                });

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
        public async Task<List<TEntityFrom>> FindAllAsync(
            Expression<Func<TEntityFrom, bool>>[] filters, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using DBContextRead createDbContext =
                await _dbContextRead.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            IQueryable<TEntityTo> query = createDbContext.Set<TEntityTo>();

            if (filters is not null && filters.Length > 0)
            {
                Expression<Func<TEntityTo, bool>>[] replaced =
                    filters.ReplaceParameters<TEntityFrom, TEntityTo>();

                query = replaced.Aggregate(query, (current, filter) => current.Where(filter));
            }

            List<TEntityTo> result = [];

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await using IDbContextTransaction transaction =
                        await createDbContext.Database.BeginTransactionAsync(
                            isolationLevel: IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);

                    result =
                        await _pipelineRead.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await query.AsNoTracking().ToListAsync(
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken);
                });

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
        public async Task<int> CreateAsync(
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

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await createDbContext.Set<TEntityTo>().AddAsync(
                        entity: entityConvert, cancellationToken: cancellationToken);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await createDbContext.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

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
        public async Task<(int Result, TEntityFrom Data)> CreateAsync(
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

            await context.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await context.Set<TEntityTo>().AddAsync(
                        entity: entityConvert, cancellationToken: cancellationToken);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await context.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

            return result <= 0
                ? (Result: 0, Data: entity)
                : (Result: result, Data: entityConvert.ProjectTo<TEntityTo, TEntityFrom>());
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
        public async Task<int> CreateAsync(
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

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    await createDbContext.Set<TEntityTo>().AddRangeAsync(
                        entities: entitiesConvert, cancellationToken: cancellationToken);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await createDbContext.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

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
        public async Task<(int Result, IEnumerable<TEntityFrom> Data)> CreateAsync(
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

            await context.Set<TEntityTo>()
                .AddRangeAsync(entities: entitiesConvert, cancellationToken: cancellationToken);

            int result = 0;

            await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await context.Set<TEntityTo>().AddRangeAsync(
                    entities: entitiesConvert, cancellationToken: cancellationToken);

                result =
                    await _pipelineWrite.ExecuteAsync(
                        async cancellationToken =>
                        {
                            return
                                await context.SaveChangesAsync(
                                    audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);
            });

            return result <= 0
                ? (Result: 0, Data: [])
                : (Result: result, Data: entitiesConvert.ProjectTo<TEntityTo, TEntityFrom>());
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
        public async Task<int> UpdateAsync(
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

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    createDbContext.Set<TEntityTo>().Update(entity: entityConvert);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await createDbContext.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

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
        public async Task<(int Result, TEntityFrom Data)> UpdateAsync(
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

            await context.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    context.Set<TEntityTo>().Update(entity: entityConvert);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await context.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

            return result <= 0
                ? (Result: 0, Data: entity)
                : (Result: result, Data: entityConvert.ProjectTo<TEntityTo, TEntityFrom>());
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
        public async Task<int> UpdateAsync(
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

            await createDbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                async () =>
                {
                    createDbContext.Set<TEntityTo>().UpdateRange(entities: entitiesConvert);

                    result =
                        await _pipelineWrite.ExecuteAsync(
                            async cancellationToken =>
                            {
                                return
                                    await createDbContext.SaveChangesAsync(
                                        audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                });

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
        public async Task<(int Result, IEnumerable<TEntityFrom> Data)> UpdateAsync(
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

            await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                context.Set<TEntityTo>().UpdateRange(entities: entitiesConvert);

                result =
                    await _pipelineWrite.ExecuteAsync(
                        async cancellationToken =>
                        {
                            return
                                await context.SaveChangesAsync(
                                    audit: auditLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);
            });

            return result is 0
                ? (Result: 0, Data: entities)
                : (Result: result, Data: entitiesConvert.ProjectTo<TEntityTo, TEntityFrom>());
        }
    }

    #endregion :::::::::::::::::::::::::::::::::: BaseSQLRepository Tồn tại <TEntityFrom, TEntityTo> ::::::::::::::::::::::::::::::::::
}