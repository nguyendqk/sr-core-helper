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

        public async Task<DBContextWrite> Context(CancellationToken cancellationToken = default)
        {
            if (_context is not null)
            {
                return _context;
            }

            _context = await dbContext.Value.CreateDbContextAsync(cancellationToken: cancellationToken);

            return _context;
        }

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

        public async Task RollbackAsync()
        {
            await _transaction.RollbackAsync();

            await DisposeTransactionAsync();

            logger.Warning(nameof(UnitOfWork), nameof(RollbackAsync), "[TRANSACTION] - Rollback transaction.");
        }

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