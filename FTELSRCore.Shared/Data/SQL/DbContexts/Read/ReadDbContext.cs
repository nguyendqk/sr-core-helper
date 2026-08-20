using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FTELSRCore.Data.SQL.DbContexts.Read
{
    /// <summary>
    /// DbContext dùng cho các thao tác đọc (read replica/nguồn dữ liệu chỉ đọc), tách biệt với
    /// <see cref="FTELSRCore.Data.SQL.DbContexts.Write.WriteDbContext{TContext}"/> để không chịu chi phí theo dõi
    /// thay đổi (change tracking), audit hay dispatch domain event của luồng ghi.
    /// </summary>
    /// <typeparam name="TContext">Kiểu DbContext cụ thể của ứng dụng, dùng để nạp cấu hình entity tương ứng.</typeparam>
    public class ReadDbContext<TContext> : DbContext where TContext : DbContext
    {
        /// <summary>
        /// Khởi tạo ReadDbContext với các tùy chọn kết nối được cấu hình sẵn.
        /// </summary>
        /// <param name="options">Các tùy chọn cấu hình DbContext (connection string, provider, interceptor, v.v.).</param>
        /// <param name="serviceScopeFactory">Factory tạo scope DI, hiện chưa được sử dụng trong lớp này.</param>
        ///
        public ReadDbContext(DbContextOptions<TContext> options, Lazy<IServiceScopeFactory> serviceScopeFactory) : base(options)
        {
        }

        /// <summary>
        /// Nạp cấu hình entity (entity configurations) từ assembly chứa TContext để áp dụng cho mô hình dữ liệu.
        /// </summary>
        /// <param name="modelBuilder">Đối tượng dùng để xây dựng mô hình dữ liệu của DbContext.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);
        }
    }
}