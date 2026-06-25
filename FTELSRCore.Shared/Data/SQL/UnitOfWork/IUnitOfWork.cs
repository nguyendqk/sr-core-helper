using FTELSRCore.Data.SQL.DbContexts.Write;
using Microsoft.EntityFrameworkCore.Storage;

namespace FTELSRCore.Data.SQL.UnitOfWork
{
    public interface IUnitOfWork<DBContextWrite> where DBContextWrite : WriteDbContext<DBContextWrite>, IAsyncDisposable
    {
        /// <summary>
        /// The following Property is going to hold the context object
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<DBContextWrite> Context(CancellationToken cancellationToken = default);

        /// <summary>
        /// Start the database Transaction
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task<IDbContextTransaction> CreateTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commit the database Transaction
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rollback the database Transaction
        /// </summary>
        /// <returns></returns>
        ///
        Task RollbackAsync();
    }
}