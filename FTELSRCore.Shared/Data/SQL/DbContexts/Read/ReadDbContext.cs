using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FTELSRCore.Data.SQL.DbContexts.Read
{
    public class ReadDbContext<TContext> : DbContext where TContext : DbContext
    {
        public ReadDbContext(DbContextOptions<TContext> options, Lazy<IServiceScopeFactory> serviceScopeFactory) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);
        }
    }
}