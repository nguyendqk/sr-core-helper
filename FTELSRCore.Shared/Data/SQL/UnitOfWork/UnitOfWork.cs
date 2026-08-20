using FTELSRCore.Data.SQL.DbContexts.Write;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FTELSRCore.Data.SQL.UnitOfWork
{
    public partial class UnitOfWork<DBContextWrite>(
        ILogger<UnitOfWork<DBContextWrite>> logger, Lazy<IDbContextFactory<DBContextWrite>> dbContext)
        : IUnitOfWork<DBContextWrite>, IAsyncDisposable where DBContextWrite : WriteDbContext<DBContextWrite>
    {
        private bool _disposed = false;

        private DBContextWrite _context;

        private IDbContextTransaction _transaction;

        /// <summary>
        /// Lấy DbContext hiện tại, tạo mới thông qua <see cref="IDbContextFactory{TContext}"/> nếu chưa tồn tại
        /// (context được cache lại cho các lần gọi sau trong cùng vòng đời của UnitOfWork).
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>DbContext ghi (write) đang được quản lý bởi UnitOfWork.</returns>
        ///
        public async Task<DBContextWrite> Context(CancellationToken cancellationToken = default)
        {
            if (_context is not null)
            {
                return _context;
            }

            _context = await dbContext.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            return _context;
        }

        /// <summary>
        /// Khởi tạo transaction mới trên DbContext (đảm bảo Context đã được tạo trước). Nếu phát hiện transaction
        /// cũ chưa được commit/rollback, tự động rollback transaction cũ trước khi bắt đầu transaction mới.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Đối tượng <see cref="IDbContextTransaction"/> vừa được tạo.</returns>
        ///
        public async Task<IDbContextTransaction> CreateTransactionAsync(CancellationToken cancellationToken = default)
        {
            await Context(cancellationToken);

            if (_transaction is not null)
            {
                logger.Warning(nameof(UnitOfWork), nameof(CreateTransactionAsync),
                    "[TRANSACTION] - Phát hiện transaction cũ chưa commit/rollback, tự rollback trước khi tạo transaction mới.");

                await RollbackAsync();
            }

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            logger.Warning(nameof(UnitOfWork), nameof(CreateTransactionAsync), "[TRANSACTION] - Create transaction.");

            return _transaction;
        }

        /// <summary>
        /// Lưu các thay đổi (SaveChanges) rồi commit transaction hiện tại và giải phóng transaction.
        /// Nếu commit thất bại, tự động rollback transaction trước khi ném lại (throw) ngoại lệ gốc;
        /// nếu bản thân rollback cũng thất bại thì chỉ ghi log, không ném thêm ngoại lệ.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _ = await SaveChangeAsync(cancellationToken: cancellationToken);

                await _transaction.CommitAsync(cancellationToken);

                await DisposeTransactionAsync();

                logger.Warning(nameof(UnitOfWork), nameof(CommitAsync), "[TRANSACTION] - Commit transaction.");
            }
            catch (Exception exception)
            {
                logger.Warning(nameof(UnitOfWork), nameof(CommitAsync),
                    $"[TRANSACTION] - Lỗi khi commit, tiến hành rollback. Ex: {exception.Message}");

                try
                {
                    if (_transaction is not null)
                    {
                        await RollbackAsync();
                    }
                }
                catch (Exception rollbackException)
                {
                    logger.Warning(nameof(UnitOfWork), nameof(CommitAsync),
                        $"[TRANSACTION] - Rollback sau lỗi commit cũng thất bại. Ex: {rollbackException.Message}");
                }

                throw;
            }
        }

        /// <summary>
        /// Rollback transaction hiện tại và giải phóng (dispose) transaction đó.
        /// </summary>
        /// <returns></returns>
        ///
        public async Task RollbackAsync()
        {
            await _transaction.RollbackAsync();

            await DisposeTransactionAsync();

            logger.Warning(nameof(UnitOfWork), nameof(RollbackAsync), "[TRANSACTION] - Rollback transaction.");
        }

        /// <summary>
        /// Giải phóng transaction (nếu có) và DbContext hiện tại; đảm bảo chỉ thực hiện một lần (idempotent)
        /// thông qua cờ <c>_disposed</c>.
        /// </summary>
        /// <returns></returns>
        ///
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            await DisposeTransactionAsync();

            if (_context is not null)
            {
                await _context.DisposeAsync();

                _context = null;
            }
        }

        private async Task DisposeTransactionAsync()
        {
            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();

                _transaction = null;
            }
        }

        private Task<int> SaveChangeAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}