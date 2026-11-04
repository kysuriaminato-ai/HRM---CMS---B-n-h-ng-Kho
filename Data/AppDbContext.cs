using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Helpers;

namespace ERP_HRM_CMS.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IDataProtectionProvider _dataProtectionProvider;

        // Các cột dữ liệu nhạy cảm: được mã hóa khi lưu và che số trong nhật ký kiểm toán.
        private const string IdentityCardColumn = "IdentityCard";
        private const string BankAccountColumn = "BankAccountNumber";

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IDataProtectionProvider dataProtectionProvider,
            IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _dataProtectionProvider = dataProtectionProvider;
        }

        /// <summary>Chuyển giá trị nhạy cảm sang dạng che số cho nhật ký kiểm toán.</summary>
        private static object? MaskForAudit(string propName, object? value)
        {
            if (propName == IdentityCardColumn || propName == BankAccountColumn)
            {
                return SensitiveDataMasker.Mask(value as string);
            }
            return value;
        }

        // Phân hệ Auth & Phân quyền
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

        // Phân hệ HRM
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<EmployeeDetail> EmployeeDetails => Set<EmployeeDetail>();
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<AttendanceDaily> AttendanceDailies => Set<AttendanceDaily>();

        // Phân hệ CMS
        public DbSet<SiteLayout> SiteLayouts => Set<SiteLayout>();
        public DbSet<Menu> Menus => Set<Menu>();
        public DbSet<Content> Contents => Set<Content>();
        public DbSet<Banner> Banners => Set<Banner>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

        // Phân hệ Bán hàng & Kho
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<CartItem> CartItems => Set<CartItem>();

        // Phân hệ Lõi Doanh Nghiệp (Enterprise Core)
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationRead> NotificationReads => Set<NotificationRead>();

        // Phân hệ Chuỗi Cung Ứng & SCM (Phiên 3)
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails => Set<PurchaseOrderDetail>();
        public DbSet<DebtRecord> DebtRecords => Set<DebtRecord>();
        public DbSet<DebtPayment> DebtPayments => Set<DebtPayment>();
        public DbSet<StocktakingSheet> StocktakingSheets => Set<StocktakingSheet>();
        public DbSet<StocktakingDetail> StocktakingDetails => Set<StocktakingDetail>();

        // Phiên 5: Hồ sơ nhân viên mở rộng
        public DbSet<EmployeeRelative> EmployeeRelatives => Set<EmployeeRelative>();
        public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();
        public DbSet<EmployeeCertificate> EmployeeCertificates => Set<EmployeeCertificate>();
        public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
        public DbSet<EmployeeChangeHistory> EmployeeChangeHistories => Set<EmployeeChangeHistory>();

        // Phân hệ HRM Nâng Cao & Tiền Lương
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
        public DbSet<SalarySlip> SalarySlips => Set<SalarySlip>();
        public DbSet<PayrollAllowanceType> PayrollAllowanceTypes => Set<PayrollAllowanceType>();
        public DbSet<EmployeeDependent> EmployeeDependents => Set<EmployeeDependent>();
        public DbSet<SeedHistory> SeedHistories => Set<SeedHistory>();

        // Phân hệ HRM — Tuyển dụng, Đào tạo, KPI (Phiên 4 DB)
        public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
        public DbSet<JobPosting> JobPostings => Set<JobPosting>();
        public DbSet<Candidate> Candidates => Set<Candidate>();
        public DbSet<Training> Trainings => Set<Training>();
        public DbSet<TrainingParticipant> TrainingParticipants => Set<TrainingParticipant>();
        public DbSet<KpiReview> KpiReviews => Set<KpiReview>();
        public DbSet<KpiCriteria> KpiCriteriaSet => Set<KpiCriteria>();

        // Phân hệ CMS mở rộng (Phiên 4 DB)
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<ContentTag> ContentTags => Set<ContentTag>();
        public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
        public DbSet<SeoMeta> SeoMetas => Set<SeoMeta>();

        // Phân hệ SCM mở rộng (Phiên 4 DB)
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Payment> Payments => Set<Payment>();

        /// <summary>
        /// Cột thời điểm cần đổi múi giờ: tên kết thúc bằng "At" (CreatedAt, ReadAt...)
        /// hoặc là các cột thời điểm đặc biệt. Không gồm cột ngày lịch hay cột "...Utc".
        /// </summary>
        public static bool IsInstantProperty(string propertyName)
        {
            if (propertyName.EndsWith("At", StringComparison.Ordinal)) return true;
            return propertyName is "ReceivedDate" or "PaymentDate" or "CheckDate";
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mã hóa số CCCD và số tài khoản ngân hàng khi lưu xuống database (xem ProtectedStringConverter).
            // Giá trị mã hóa dài hơn bản gốc nên cột được nới lên 512 ký tự.
            var sensitiveConverter = new ProtectedStringConverter(
                ProtectedStringConverter.CreateProtector(_dataProtectionProvider));

            modelBuilder.Entity<Employee>().Property(e => e.IdentityCard)
                .HasConversion(sensitiveConverter)
                .HasMaxLength(512);

            modelBuilder.Entity<EmployeeDetail>().Property(d => d.BankAccountNumber)
                .HasConversion(sensitiveConverter)
                .HasMaxLength(512);

            // Bộ đếm đăng nhập sai: một dòng cho mỗi cặp (tên đăng nhập, IP)
            modelBuilder.Entity<LoginAttempt>()
                .HasIndex(a => new { a.Username, a.IpAddress })
                .IsUnique();

            // Composite Primary Keys
            // Trạng thái nghiệp vụ: enum trong C#, lưu dưới dạng mã chuỗi cũ (xem WorkflowStatusCodes)
            modelBuilder.Entity<PurchaseOrder>().Property(p => p.Status)
                .HasConversion(new WorkflowStatusConverter<PurchaseOrderStatus>(WorkflowStatusCodes.PurchaseOrder));
            modelBuilder.Entity<DebtRecord>().Property(d => d.Status)
                .HasConversion(new WorkflowStatusConverter<DebtStatus>(WorkflowStatusCodes.Debt));
            modelBuilder.Entity<LeaveRequest>().Property(l => l.Status)
                .HasConversion(new WorkflowStatusConverter<LeaveRequestStatus>(WorkflowStatusCodes.LeaveRequest));
            modelBuilder.Entity<PayrollPeriod>().Property(p => p.Status)
                .HasConversion(new WorkflowStatusConverter<PayrollPeriodStatus>(WorkflowStatusCodes.PayrollPeriod));
            modelBuilder.Entity<SalarySlip>().Property(s => s.Status)
                .HasConversion(new WorkflowStatusConverter<SalarySlipStatus>(WorkflowStatusCodes.SalarySlip));

            // Thời điểm nghiệp vụ: database lưu UTC, ứng dụng làm việc theo giờ Việt Nam.
            // Cột ngày lịch (FromDate, WorkDate, DueDate...) và cột đã là UTC (...Utc) không đổi.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (!IsInstantProperty(property.Name)) continue;

                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(new VietnamTimeConverter());
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new NullableVietnamTimeConverter());
                    }
                }
            }

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            modelBuilder.Entity<AttendanceDaily>()
                .HasIndex(a => new { a.EmployeeId, a.WorkDate })
                .IsUnique();

            modelBuilder.Entity<Inventory>()
                .HasIndex(i => new { i.ProductId, i.BranchId })
                .IsUnique();

            // Indexes duy nhất (Unique Constraints) bảo vệ tính toàn vẹn dữ liệu
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            // Bản ghi cũ (trước khi có SecurityStamp) nhận giá trị ngẫu nhiên khi migration thêm cột
            modelBuilder.Entity<User>().Property(u => u.SecurityStamp).HasDefaultValueSql("NEWID()");

            // Mỗi nhân viên tối đa một tài khoản; nhiều tài khoản chưa gắn nhân viên (EmployeeId NULL) vẫn được phép.
            // Khớp với script Database/Legacy/SQL_fix_users_employeeid_unique.sql (cùng tên IX_Users_EmployeeId).
            modelBuilder.Entity<User>().HasIndex(u => u.EmployeeId).IsUnique().HasFilter("[EmployeeId] IS NOT NULL");

            modelBuilder.Entity<NotificationRead>().HasKey(r => new { r.NotificationId, r.UserId });
            modelBuilder.Entity<NotificationRead>()
                .HasOne(r => r.Notification)
                .WithMany()
                .HasForeignKey(r => r.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Role>().HasIndex(r => r.Code).IsUnique();
            modelBuilder.Entity<Module>().HasIndex(m => m.Code).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(p => p.Code).IsUnique();

            modelBuilder.Entity<Company>().HasIndex(c => c.Code).IsUnique();
            modelBuilder.Entity<Branch>().HasIndex(b => b.Code).IsUnique();
            modelBuilder.Entity<Department>().HasIndex(d => d.Code).IsUnique();
            modelBuilder.Entity<Position>().HasIndex(p => p.Code).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.EmployeeCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.Email).IsUnique();
            modelBuilder.Entity<Contract>().HasIndex(c => c.ContractNumber).IsUnique();
            modelBuilder.Entity<Shift>().HasIndex(s => s.Code).IsUnique();
            modelBuilder.Entity<PayrollAllowanceType>().HasIndex(t => t.Code).IsUnique();
            modelBuilder.Entity<EmployeeDependent>().HasIndex(d => new { d.EmployeeId, d.FromDate });

            modelBuilder.Entity<Category>().HasIndex(c => c.Code).IsUnique();
            modelBuilder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.SKU).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
            modelBuilder.Entity<InventoryTransaction>().HasIndex(t => t.TransactionCode).IsUnique();
            modelBuilder.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();

            modelBuilder.Entity<Supplier>().HasIndex(s => s.Code).IsUnique();
            modelBuilder.Entity<PurchaseOrder>().HasIndex(p => p.POCode).IsUnique();
            modelBuilder.Entity<DebtPayment>().HasIndex(p => p.PaymentCode).IsUnique();
            modelBuilder.Entity<StocktakingSheet>().HasIndex(s => s.Code).IsUnique();

            modelBuilder.Entity<LeaveRequest>().HasIndex(l => l.RequestCode).IsUnique();
            modelBuilder.Entity<PayrollPeriod>().HasIndex(p => new { p.Month, p.Year }).IsUnique();

            // Mọi cột decimal phải có kiểu rõ ràng. Cột đã khai báo TypeName (tiền: decimal(18,2), tỷ lệ: decimal(5,2)...)
            // giữ nguyên; cột nào thiếu kiểu được gán mặc định decimal(18,2) để không phát sinh kiểu ngầm định.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if ((property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                        && property.GetColumnType() == null)
                    {
                        property.SetColumnType("decimal(18,2)");
                    }
                }
            }

            // 1:1 Quan hệ Employee - User
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne()
                .HasForeignKey<User>(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // 1:1 Quan hệ Employee - EmployeeDetail
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.EmployeeDetail)
                .WithOne(ed => ed.Employee)
                .HasForeignKey<EmployeeDetail>(ed => ed.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình Quan hệ Cascade Delete cho các bảng chi tiết
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PurchaseOrderDetail>()
                .HasOne(pd => pd.PurchaseOrder)
                .WithMany(p => p.PurchaseOrderDetails)
                .HasForeignKey(pd => pd.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StocktakingDetail>()
                .HasOne(sd => sd.StocktakingSheet)
                .WithMany(s => s.StocktakingDetails)
                .HasForeignKey(sd => sd.StocktakingSheetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DebtPayment>()
                .HasOne(dp => dp.DebtRecord)
                .WithMany(d => d.DebtPayments)
                .HasForeignKey(dp => dp.DebtRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình Quan hệ SCM & Tránh xung đột khóa ngoại đa đường dẫn SQL Server
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.Creator)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.Approver)
                .WithMany()
                .HasForeignKey(p => p.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DebtRecord>()
                .HasOne(d => d.Supplier)
                .WithMany(s => s.DebtRecords)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DebtRecord>()
                .HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DebtRecord>()
                .HasOne(d => d.PurchaseOrder)
                .WithMany()
                .HasForeignKey(d => d.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DebtPayment>()
                .HasOne(p => p.Creator)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StocktakingSheet>()
                .HasOne(s => s.Creator)
                .WithMany()
                .HasForeignKey(s => s.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình Quan hệ HRM Nâng Cao (Phiên 4)
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.DepartmentHead)
                .WithMany()
                .HasForeignKey(l => l.DepartmentHeadId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Director)
                .WithMany()
                .HasForeignKey(l => l.DirectorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayrollPeriod>()
                .HasOne(p => p.Creator)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalarySlip>()
                .HasOne(s => s.PayrollPeriod)
                .WithMany(p => p.SalarySlips)
                .HasForeignKey(s => s.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalarySlip>()
                .HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ═══════════════════════════════════════════════════════════
            // PHIÊN 5 — Hồ sơ nhân viên mở rộng
            // ═══════════════════════════════════════════════════════════

            // Department: cây đệ quy (self-referencing)
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Parent)
                .WithMany(d => d.Children)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // EmployeeRelative
            modelBuilder.Entity<EmployeeRelative>()
                .HasOne(r => r.Employee).WithMany(e => e.Relatives)
                .HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Cascade);

            // EmployeeSkill
            modelBuilder.Entity<EmployeeSkill>()
                .HasOne(s => s.Employee).WithMany(e => e.Skills)
                .HasForeignKey(s => s.EmployeeId).OnDelete(DeleteBehavior.Cascade);

            // EmployeeCertificate
            modelBuilder.Entity<EmployeeCertificate>()
                .HasOne(c => c.Employee).WithMany(e => e.Certificates)
                .HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.Cascade);

            // EmployeeDocument
            modelBuilder.Entity<EmployeeDocument>()
                .HasOne(d => d.Employee).WithMany(e => e.Documents)
                .HasForeignKey(d => d.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EmployeeDocument>()
                .HasOne(d => d.Uploader).WithMany()
                .HasForeignKey(d => d.UploadedBy).OnDelete(DeleteBehavior.Restrict);

            // EmployeeChangeHistory
            modelBuilder.Entity<EmployeeChangeHistory>()
                .HasOne(h => h.Employee).WithMany(e => e.ChangeHistories)
                .HasForeignKey(h => h.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EmployeeChangeHistory>()
                .HasOne(h => h.ChangedByUser).WithMany()
                .HasForeignKey(h => h.ChangedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EmployeeChangeHistory>()
                .HasIndex(h => new { h.EmployeeId, h.ChangeType, h.EffectiveDate });

            // ═══════════════════════════════════════════════════════════
            // PHIÊN 4 DB — Entities mới: Fluent API
            // ═══════════════════════════════════════════════════════════

            // --- HRM: LeaveType ---
            modelBuilder.Entity<LeaveType>().HasIndex(t => t.Code).IsUnique();

            // --- HRM: JobPosting ---
            modelBuilder.Entity<JobPosting>().HasIndex(j => j.Code).IsUnique();
            modelBuilder.Entity<JobPosting>()
                .HasOne(j => j.Department).WithMany().HasForeignKey(j => j.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<JobPosting>()
                .HasOne(j => j.Position).WithMany().HasForeignKey(j => j.PositionId).OnDelete(DeleteBehavior.Restrict);

            // --- HRM: Candidate ---
            modelBuilder.Entity<Candidate>().HasIndex(c => c.Code).IsUnique();

            // --- HRM: Training ---
            modelBuilder.Entity<Training>().HasIndex(t => t.Code).IsUnique();

            // --- HRM: TrainingParticipant (composite PK) ---
            modelBuilder.Entity<TrainingParticipant>().HasKey(tp => new { tp.TrainingId, tp.EmployeeId });

            // --- HRM: KpiReview ---
            modelBuilder.Entity<KpiReview>().HasIndex(k => new { k.EmployeeId, k.PeriodMonth, k.PeriodYear });
            modelBuilder.Entity<KpiReview>()
                .HasOne(k => k.Reviewer).WithMany().HasForeignKey(k => k.ReviewerId).OnDelete(DeleteBehavior.Restrict);

            // --- CMS: Tag ---
            modelBuilder.Entity<Tag>().HasIndex(t => t.Name).IsUnique();
            modelBuilder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique();

            // --- CMS: ContentTag (composite PK) ---
            modelBuilder.Entity<ContentTag>().HasKey(ct => new { ct.ContentId, ct.TagId });

            // --- CMS: SeoMeta (unique per entity) ---
            modelBuilder.Entity<SeoMeta>().HasIndex(s => new { s.EntityType, s.EntityId }).IsUnique();

            // --- SCM: Customer ---
            modelBuilder.Entity<Customer>().HasIndex(c => c.Code).IsUnique();
            modelBuilder.Entity<Customer>().HasIndex(c => c.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL");

            // --- SCM: Unit ---
            modelBuilder.Entity<Unit>().HasIndex(u => u.Code).IsUnique();

            // --- SCM: Warehouse ---
            modelBuilder.Entity<Warehouse>().HasIndex(w => w.Code).IsUnique();
            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.Manager).WithMany().HasForeignKey(w => w.ManagerId).OnDelete(DeleteBehavior.SetNull);

            // --- SCM: Invoice ---
            modelBuilder.Entity<Invoice>().HasIndex(i => i.InvoiceCode).IsUnique();
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Order).WithMany().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.PurchaseOrder).WithMany().HasForeignKey(i => i.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Supplier).WithMany().HasForeignKey(i => i.SupplierId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Creator).WithMany().HasForeignKey(i => i.CreatedBy).OnDelete(DeleteBehavior.Restrict);

            // --- SCM: Payment ---
            modelBuilder.Entity<Payment>().HasIndex(p => p.PaymentCode).IsUnique();
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Creator).WithMany().HasForeignKey(p => p.CreatedBy).OnDelete(DeleteBehavior.Restrict);

            // --- SCM: DebtRecord → Customer (nullable FK mới) ---
            modelBuilder.Entity<DebtRecord>()
                .HasOne(d => d.Customer)
                .WithMany(c => c.DebtRecords)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Global Query Filter: Soft Delete cho IAuditable ---
            // Tự động lọc IsDeleted = false cho tất cả entity implement IAuditable
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IAuditable).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(AppDbContext)
                        .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                        .MakeGenericMethod(entityType.ClrType);
                    method.Invoke(null, new object[] { modelBuilder });
                }
            }
        }

        /// <summary>Áp dụng global query filter IsDeleted == false cho entity type T</summary>
        private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : class, IAuditable
        {
            modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();
            // Dữ liệu nghiệp vụ và AuditLog được lưu trong cùng một transaction (nếu chưa có transaction ngoài)
            await using var tx = Database.CurrentTransaction == null && auditEntries.Count > 0
                ? await Database.BeginTransactionAsync(cancellationToken)
                : null;
            var result = await base.SaveChangesAsync(cancellationToken);
            await OnAfterSaveChanges(auditEntries, cancellationToken);
            if (tx != null) await tx.CommitAsync(cancellationToken);
            return result;
        }

        public override int SaveChanges()
        {
            var auditEntries = OnBeforeSaveChanges();
            using var tx = Database.CurrentTransaction == null && auditEntries.Count > 0
                ? Database.BeginTransaction()
                : null;
            var result = base.SaveChanges();
            OnAfterSaveChanges(auditEntries, default).GetAwaiter().GetResult();
            tx?.Commit();
            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            var httpContext = _httpContextAccessor?.HttpContext;
            int? currentUserId = null;
            string? currentUsername = null;
            string? ipAddress = null;

            if (httpContext != null)
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int uid)) currentUserId = uid;
                else currentUserId = httpContext.Session.GetInt32("UserId");

                currentUsername = httpContext.User.Identity?.Name ?? httpContext.Session.GetString("Username") ?? "System";
                ipAddress = httpContext.Connection?.RemoteIpAddress?.ToString();
            }

            foreach (var entry in ChangeTracker.Entries())
            {
                // Bỏ qua AuditLog, Notification và bộ đếm đăng nhập sai (ghi mỗi lần đăng nhập thất bại, sẽ làm nhật ký nhiễu)
                if (entry.Entity is AuditLog || entry.Entity is Notification || entry.Entity is NotificationRead || entry.Entity is LoginAttempt || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    EntityName = entry.Entity.GetType().Name,
                    UserId = currentUserId,
                    Username = currentUsername,
                    IpAddress = ipAddress
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Action = "CREATE";
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                auditEntry.RecordId = prop.CurrentValue?.ToString();
                                continue;
                            }
                            string propName = prop.Metadata.Name;
                            object? val = prop.CurrentValue;
                            if (propName.Contains("Password", StringComparison.OrdinalIgnoreCase)) val = "***MASKED***";
                            else val = MaskForAudit(propName, val);
                            auditEntry.NewValues[propName] = val;
                        }
                        break;

                    case EntityState.Deleted:
                        auditEntry.Action = "DELETE";
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                auditEntry.RecordId = prop.OriginalValue?.ToString();
                                continue;
                            }
                            string propName = prop.Metadata.Name;
                            object? val = prop.OriginalValue;
                            if (propName.Contains("Password", StringComparison.OrdinalIgnoreCase)) val = "***MASKED***";
                            else val = MaskForAudit(propName, val);
                            auditEntry.OldValues[propName] = val;
                        }
                        break;

                    case EntityState.Modified:
                        auditEntry.Action = "UPDATE";
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                auditEntry.RecordId = prop.CurrentValue?.ToString() ?? prop.OriginalValue?.ToString();
                                continue;
                            }
                            if (prop.IsModified)
                            {
                                string propName = prop.Metadata.Name;
                                object? oldVal = prop.OriginalValue;
                                object? newVal = prop.CurrentValue;

                                if (propName.Contains("Password", StringComparison.OrdinalIgnoreCase))
                                {
                                    oldVal = "***MASKED***";
                                    newVal = "***MASKED***";
                                }
                                else
                                {
                                    oldVal = MaskForAudit(propName, oldVal);
                                    newVal = MaskForAudit(propName, newVal);
                                }

                                auditEntry.ChangedColumns.Add(propName);
                                auditEntry.OldValues[propName] = oldVal;
                                auditEntry.NewValues[propName] = newVal;
                            }
                        }
                        break;
                }

                auditEntries.Add(auditEntry);
            }

            return auditEntries;
        }

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
        {
            if (auditEntries == null || auditEntries.Count == 0) return;

            foreach (var auditEntry in auditEntries)
            {
                // Cập nhật RecordId cho các bản ghi vừa được tạo (Added) nếu ban đầu chưa có hoặc đang bằng "0"
                if (string.IsNullOrEmpty(auditEntry.RecordId) || auditEntry.RecordId == "0")
                {
                    foreach (var prop in auditEntry.Entry.Properties)
                    {
                        if (prop.Metadata.IsPrimaryKey())
                        {
                            auditEntry.RecordId = prop.CurrentValue?.ToString();
                            break;
                        }
                    }
                }

                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            await base.SaveChangesAsync(cancellationToken);
        }
    }
}