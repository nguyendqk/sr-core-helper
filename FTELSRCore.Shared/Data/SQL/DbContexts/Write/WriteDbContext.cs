using System.Formats.Asn1;
using FTELSRCore.Abstractions;
using FTELSRCore.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace FTELSRCore.Data.SQL.DbContexts.Write
{
    /// <summary>
    /// DbContext dùng cho các thao tác ghi (Insert/Update/Delete) trên nguồn dữ liệu chính (write primary),
    /// khác với <see cref="FTELSRCore.Data.SQL.DbContexts.Read.ReadDbContext{TContext}"/>. Tự động gán thông tin
    /// audit (người tạo/sửa, thời gian) trước khi lưu và phát tán (dispatch) domain event sau khi lưu thành công.
    /// </summary>
    /// <typeparam name="TContext">Kiểu DbContext cụ thể của ứng dụng, dùng để nạp cấu hình entity tương ứng.</typeparam>
    public partial class WriteDbContext<TContext> : DbContext where TContext : DbContext
    {
        private readonly Lazy<IServiceScopeFactory> _serviceScopeFactory;

        /// <summary>
        /// Khởi tạo WriteDbContext với các tùy chọn kết nối được cấu hình sẵn và factory tạo scope DI
        /// (dùng để resolve <see cref="IPublisher"/> khi dispatch domain event).
        /// </summary>
        /// <param name="options">Các tùy chọn cấu hình DbContext (connection string, provider, interceptor, v.v.).</param>
        /// <param name="serviceScopeFactory">Factory tạo scope DI dùng khi phát tán domain event.</param>
        ///
        public WriteDbContext(DbContextOptions<TContext> options, Lazy<IServiceScopeFactory> serviceScopeFactory) : base(options)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Nạp cấu hình entity (entity configurations) từ assembly chứa TContext để áp dụng cho mô hình dữ liệu.
        /// </summary>
        /// <param name="modelBuilder">Đối tượng dùng để xây dựng mô hình dữ liệu của DbContext.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Load configurations từ assembly của TContext để hỗ trợ đa dạng các context.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);
        }
    }

    public partial class WriteDbContext<TContext> where TContext : DbContext
    {
        /// <summary>
        /// Ghi đè SaveChanges đồng bộ: bổ sung thông tin audit trước khi lưu (không có tham số <see cref="AuditModel"/>
        /// nên chỉ dùng giá trị mặc định), sau đó phát tán domain event nếu có bản ghi thay đổi.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">true để chấp nhận toàn bộ thay đổi sau khi lưu thành công.</param>
        /// <returns>Số bản ghi bị ảnh hưởng.</returns>
        ///
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaveChanges();

            int result = base.SaveChanges(acceptAllChangesOnSuccess);

            if (result > 0)
            {
                _ = OnAfterSaveChanges();
            }

            return result;
        }

        /// <summary>
        /// Lưu thay đổi bất đồng bộ có kèm thông tin audit: bổ sung dữ liệu audit trước khi lưu, thu thập
        /// các thay đổi để audit (nếu <paramref name="audit"/> khác null) rồi phát tán domain event sau khi lưu thành công.
        /// </summary>
        /// <param name="audit">Thông tin người thực hiện/audit; null thì dùng giá trị mặc định (Anonymous).</param>
        /// <param name="acceptAllChangesOnSuccess">true để chấp nhận toàn bộ thay đổi sau khi lưu thành công.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Số bản ghi bị ảnh hưởng.</returns>
        ///
        public async Task<int> SaveChangesAsync(AuditModel audit = null, bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = default)
        {
            OnBeforeSaveChanges(audit);

            List<SnapshotAuditModel> detectChangesAudit = [];

            if (audit is not null)
            {
                detectChangesAudit = DetectChangesAudit(audit);
            }

            int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (result > 0)
            {
                await OnAfterSaveChanges(detectChangesAudit, cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Ghi đè SaveChangesAsync mặc định của EF Core: bổ sung thông tin audit (giá trị mặc định) trước khi lưu,
        /// sau đó phát tán domain event nếu có ít nhất một bản ghi thay đổi.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">true để chấp nhận toàn bộ thay đổi sau khi lưu thành công.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Số bản ghi bị ảnh hưởng.</returns>
        ///
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            OnBeforeSaveChanges();

            int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (result >= 1)
            {
                await OnAfterSaveChanges(cancellationToken: cancellationToken);
            }

            return result;
        }
    }

    /// <summary>
    /// Before Saving
    /// </summary>
    public partial class WriteDbContext<TContext> where TContext : DbContext
    {
        /// <summary>
        /// Bổ sung cập nhật các thông tin cơ bản.
        /// </summary>
        /// <param name="audit"></param>
        ///
        private void OnBeforeSaveChanges(AuditModel audit = null)
        {
            IEnumerable<EntityEntry> filtered =
                ChangeTracker.Entries()?.Where(x => x.Entity is IBaseEntitySQL);

            if (filtered.IsNullOrEmpty())
            {
                return;
            }

            string userName =
                !string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Name) ? audit?.CreatorInfo?.Name : CommonBaseConstant.Anonymous;

            string userCode =
                !string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Code) ? audit?.CreatorInfo?.Code : CommonBaseConstant.AnonymousCode;

            string organization =
                !string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Organization) ? audit?.CreatorInfo?.Organization : CommonBaseConstant.OrganizationForISC;

            foreach (var entry in filtered)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        {
                            ((IBaseEntitySQL)entry.Entity).IsDeleted = false;

                            ((IBaseEntitySQL)entry.Entity).CreatedDate ??= CommonBaseConstant.DateTimeUtc();
                            ((IBaseEntitySQL)entry.Entity).CreatedUser ??= userName;
                            ((IBaseEntitySQL)entry.Entity).CreatedUserCode ??= userCode;
                            ((IBaseEntitySQL)entry.Entity).CreatedUserOrganization ??= organization;

                            ((IBaseEntitySQL)entry.Entity).ModifiedDate = null;
                            ((IBaseEntitySQL)entry.Entity).ModifiedUser = null;
                            ((IBaseEntitySQL)entry.Entity).ModifiedUserCode = null;
                            ((IBaseEntitySQL)entry.Entity).ModifiedUserOrganization = null;

                            break;
                        }
                    case EntityState.Modified or EntityState.Detached:
                        {
                            if (audit is null)
                            {
                                break;
                            }

                            ((IBaseEntitySQL)entry.Entity).ModifiedDate = CommonBaseConstant.DateTimeUtc();
                            ((IBaseEntitySQL)entry.Entity).ModifiedUser = userName;
                            ((IBaseEntitySQL)entry.Entity).ModifiedUserCode = userCode;
                            ((IBaseEntitySQL)entry.Entity).ModifiedUserOrganization = organization;

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Lấy thông tin các trường thay đổi.
        /// </summary>
        /// <param name="audit"></param>
        /// <returns></returns>
        ///
        private List<SnapshotAuditModel> DetectChangesAudit(AuditModel audit = null)
        {
            if (audit is null)
            {
                return [];
            }

            ChangeTracker.DetectChanges();

            #region :::::::::::::::::::::::::::: NOT SUPPORT ::::::::::::::::::::::::::::

            //List<AuditEntry> auditEntries = [];

            //foreach (var entry in ChangeTracker.Entries())
            //{
            //    if (entry.State is EntityState.Detached or EntityState.Unchanged)
            //    {
            //        continue;
            //    }

            //    AuditEntry auditEntry = new()
            //    {
            //        TableName = entry.Entity.GetType().Name,
            //        Address = AuditModel.Address,
            //        Creator = AuditModel.CreatorInfo,
            //        Ip = AuditModel.Ip,
            //        Device = AuditModel.Device,
            //        Method = AuditModel.Method
            //    };

            //    auditEntries.Add(auditEntry);

            //    string[] ignoreProperty =
            //    [
            //        "CreatedUser",
            //        "CreatedDate",
            //        "CreatedUserCode",
            //        "CreatedUserOrganization",

            //        "ModifiedUser",
            //        "ModifiedDate",
            //        "ModifiedUserCode",
            //        "ModifiedUserOrganization"
            //    ];

            //    foreach (PropertyEntry property in entry.Properties)
            //    {
            //        if (property.IsTemporary)
            //        {
            //            // value will be generated by the database, get the value after saving
            //            auditEntry.TemporaryProperties.Add(property);
            //            continue;
            //        }

            //        var propertyName = property.Metadata.Name;

            //        if (ignoreProperty.Contains(propertyName))
            //        {
            //            continue;
            //        }

            //        if (property.OriginalValue.ToJSon() == property.CurrentValue.ToJSon()
            //            && entry.State != EntityState.Added && !property.Metadata.IsPrimaryKey())
            //        {
            //            continue;
            //        }

            //        if (property.Metadata.IsPrimaryKey())
            //        {
            //            auditEntry.KeyValues[propertyName] = property.CurrentValue;
            //            continue;
            //        }

            //        switch (entry.State)
            //        {
            //            case EntityState.Added:
            //                {
            //                    auditEntry.Type = ActivityTypeEnum.Create;
            //                    auditEntry.NewValues[propertyName] = property.CurrentValue;
            //                    break;
            //                }
            //            case EntityState.Modified:
            //                {
            //                    switch (auditEntry.Method)
            //                    {
            //                        case "PATCH" or "PUT":
            //                            {
            //                                if (property.IsModified)
            //                                {
            //                                    auditEntry.ChangedColumns.Add(propertyName);
            //                                    auditEntry.Type = ActivityTypeEnum.Update;
            //                                    auditEntry.OldValues[propertyName] = property.OriginalValue;
            //                                    auditEntry.NewValues[propertyName] = property.CurrentValue;
            //                                }

            //                                break;
            //                            }
            //                        case "DELETE":
            //                            auditEntry.Type = ActivityTypeEnum.Delete;
            //                            auditEntry.OldValues[propertyName] = property.OriginalValue;
            //                            break;
            //                    }

            //                    break;
            //                }
            //            default:
            //                {
            //                    auditEntry.Type = ActivityTypeEnum.None;
            //                    auditEntry.NewValues[propertyName] = property.CurrentValue;
            //                    break;
            //                }
            //        }

            //        switch (auditEntry.Method)
            //        {
            //            case "POST":
            //                {
            //                    auditEntry.Type = ActivityTypeEnum.Create;
            //                    auditEntry.NewValues[propertyName] = property.CurrentValue;
            //                    break;
            //                }

            //            case "PATCH" or "PUT":
            //                {
            //                    if (property.IsModified)
            //                    {
            //                        auditEntry.ChangedColumns.Add(propertyName);
            //                        auditEntry.Type = ActivityTypeEnum.Update;
            //                        auditEntry.OldValues[propertyName] = property.OriginalValue;
            //                        auditEntry.NewValues[propertyName] = property.CurrentValue;
            //                    }

            //                    break;
            //                }
            //            case "DELETE":
            //                {
            //                    auditEntry.Type = ActivityTypeEnum.Delete;
            //                    auditEntry.OldValues[propertyName] = property.OriginalValue;
            //                    break;
            //                }
            //            default:
            //                {
            //                    auditEntry.Type = ActivityTypeEnum.None;
            //                    auditEntry.NewValues[propertyName] = property.CurrentValue;
            //                    break;
            //                }
            //        }
            //    }
            //}

            //IEnumerable<AuditEntry> auditChange = auditEntries.Where(auditEntry
            //    => !auditEntry.HasTemporaryProperties && (auditEntry.ChangedColumns.Count != 0 || auditEntry.NewValues.Count != 0 || auditEntry.OldValues.Count != 0));

            //List<AuditLog> data = [];

            //foreach (AuditEntry auditEntry in auditChange)
            //{
            //    data.Add(auditEntry.ToAudit());
            //    // await AuditLogs.AddAsync(auditEntry.ToAudit());
            //}

            #endregion :::::::::::::::::::::::::::: NOT SUPPORT ::::::::::::::::::::::::::::

            // Lấy các thông tin có sự thêm mới.
            return []; //  auditEntries.Where(_ => _.HasTemporaryProperties).ToList();
        }
    }

    /// <summary>
    /// After Saving
    /// </summary>
    ///
    public partial class WriteDbContext<TContext> where TContext : DbContext
    {
        private async Task OnAfterSaveChanges(List<SnapshotAuditModel> auditEntries = null, CancellationToken cancellationToken = default)
        {
            await DispatchAuditLog(auditEntries);

            await DispatchDomainEvents(cancellationToken: cancellationToken);
        }

        private Task DispatchAuditLog(List<SnapshotAuditModel> auditEntries)
        {
            if (auditEntries is null || auditEntries.Count is 0)
            {
                return Task.CompletedTask;
            }

            #region :::::::::::::::::::::::::::: NOT SUPPORT ::::::::::::::::::::::::::::

            //List<AuditLog> data = [];

            //foreach (AuditEntry auditEntry in auditEntries)
            //{
            //    foreach (PropertyEntry prop in auditEntry.TemporaryProperties)
            //    {
            //        if (prop.Metadata.IsPrimaryKey())
            //        {
            //            auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
            //        }
            //        else
            //        {
            //            auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
            //        }
            //    }

            //    // await AuditLogs.AddAsync(auditEntry.ToAudit());
            //    data.Add(auditEntry.ToAudit());
            //}

            #endregion :::::::::::::::::::::::::::: NOT SUPPORT ::::::::::::::::::::::::::::

            return base.SaveChangesAsync();
        }

        /// <summary>
        /// Thu thập domain event từ các <see cref="Aggregate"/> đang được ChangeTracker theo dõi (gộp với danh sách
        /// truyền vào, loại trùng lặp), xóa ChangeTracker rồi publish tuần tự từng event qua <see cref="IPublisher"/>
        /// được resolve từ một scope DI mới tạo.
        /// </summary>
        /// <param name="domainEvents">Danh sách domain event bổ sung cần publish cùng; mặc định rỗng.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public async Task DispatchDomainEvents(List<IDomainEvent> domainEvents = null, CancellationToken cancellationToken = default)
        {
            domainEvents ??= [];

            IEnumerable<IDomainEvent> domainEventsInSaveChange =
                ChangeTracker.Entries<Aggregate>()
                .Select(x => x.Entity)
                .Where(x => x.DomainEvents.Count > 0)
                .SelectMany(x => x.DomainEvents);

            if (!domainEventsInSaveChange.IsNullOrEmpty())
            {
                domainEvents.AddRange(domainEventsInSaveChange);
            }

            List<IDomainEvent> domainEventsToPublish = [.. domainEvents.Distinct()];

            ChangeTracker.Clear();

            if (domainEventsToPublish.Count is 0)
            {
                return;
            }

            await using var scope = _serviceScopeFactory.Value.CreateAsyncScope();

            IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            foreach (IDomainEvent domainEvent in domainEventsToPublish)
            {
                await publisher.Publish(domainEvent, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }
}