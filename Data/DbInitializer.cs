using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Data
{
    public static class DbInitializer
    {
        /// <param name="initialAdminPassword">Mật khẩu ban đầu cho tài khoản quản trị (SeedAdmin:Password). Bắt buộc khi tạo tài khoản lần đầu.</param>
        /// <param name="seedDemo">Chỉ true khi Development và Seed:Demo=true. Dữ liệu mẫu không bao giờ chạy ở production.</param>
        public static async Task SeedAsync(AppDbContext context, string? initialAdminPassword = null, bool seedDemo = false)
        {
            // 1. Áp dụng migration EF Core: tạo DB/bảng cho DB mới, cập nhật DB đã có mà không mất dữ liệu
            await MigrateSchemaAsync(context);

            // 2. Dữ liệu nền (SeedCore) chạy ở mọi môi trường
            await SeedCoreDataAsync(context, initialAdminPassword);

            // 3. Dữ liệu mẫu (SeedDemo) chỉ chạy khi được bật rõ ràng
            if (seedDemo)
            {
                await SeedDemoDataAsync(context);
            }
        }

        /// <summary>
        /// Hậu tố tên migration baseline (tạo bằng: dotnet ef migrations add InitialBaseline).
        /// Baseline phản ánh model hiện tại; DB đã có sẵn được đánh dấu là đã áp dụng baseline.
        /// </summary>
        private const string BaselineMigrationSuffix = "_InitialBaseline";

        /// <summary>
        /// Áp dụng các migration còn thiếu. Với DB đã có bảng nhưng chưa có bảng lịch sử migration
        /// (tạo bằng EnsureCreated trước đây), đánh dấu baseline là đã áp dụng để không chạy lại CREATE TABLE
        /// trên dữ liệu thật. Không xóa và không sửa dữ liệu nghiệp vụ.
        /// </summary>
        public static async Task MigrateSchemaAsync(AppDbContext context)
        {
            var baseline = context.Database.GetMigrations()
                .FirstOrDefault(id => id.EndsWith(BaselineMigrationSuffix, StringComparison.Ordinal));
            if (baseline == null)
            {
                throw new InvalidOperationException(
                    "Thiếu migration baseline (…_InitialBaseline). Chạy: dotnet ef migrations add InitialBaseline.");
            }

            // Chỉ đánh dấu khi DB đã có bảng Users (DB đang dùng) và chưa có bảng lịch sử migration
            // Database chưa tồn tại (DB mới): bỏ qua bước này, MigrateAsync sẽ tạo database.
            if (await context.Database.CanConnectAsync())
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    -- Đồng bộ bảng lịch sử EF Migrations với database đã có sẵn bảng nghiệp vụ
                    IF OBJECT_ID(N'dbo.Users') IS NOT NULL
                    BEGIN
                        IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NULL
                        BEGIN
                            CREATE TABLE dbo.__EFMigrationsHistory (
                                MigrationId nvarchar(150) NOT NULL PRIMARY KEY,
                                ProductVersion nvarchar(32) NOT NULL
                            );
                        END;

                        -- 1. Baseline migration
                        IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = @baseline)
                        BEGIN
                            INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
                            VALUES (@baseline, N'8.0.0');
                        END;

                        -- 2. AddSeedHistory
                        IF OBJECT_ID(N'dbo.SeedHistories', N'U') IS NOT NULL
                           AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261104082158_AddSeedHistory')
                        BEGIN
                            INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
                            VALUES (N'20261104082158_AddSeedHistory', N'8.0.0');
                        END;

                        -- 3. V6_00_Security_And_Architecture
                        IF (COL_LENGTH(N'dbo.Products', N'UnitId') IS NOT NULL OR COL_LENGTH(N'dbo.Users', N'MustChangePassword') IS NOT NULL)
                           AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261104175015_V6_00_Security_And_Architecture')
                        BEGIN
                            INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
                            VALUES (N'20261104175015_V6_00_Security_And_Architecture', N'8.0.0');
                        END;

                        -- 4. V6_01_EnterpriseHRM_Schema_And_Indexes
                        IF OBJECT_ID(N'dbo.EmployeeLeaveBalances', N'U') IS NOT NULL
                           AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261104191312_V6_01_EnterpriseHRM_Schema_And_Indexes')
                        BEGIN
                            INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
                            VALUES (N'20261104191312_V6_01_EnterpriseHRM_Schema_And_Indexes', N'8.0.0');
                        END;
                    END;

                    -- Đảm bảo các ràng buộc và chỉ mục tồn tại trên database cũ để EF Core DROP CONSTRAINT an toàn
                    IF OBJECT_ID(N'dbo.Departments', N'U') IS NOT NULL 
                       AND OBJECT_ID(N'dbo.FK_Departments_Departments_ParentId', N'F') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Departments] WITH NOCHECK ADD CONSTRAINT [FK_Departments_Departments_ParentId] FOREIGN KEY([ParentId]) REFERENCES [dbo].[Departments]([Id]);
                    END

                    IF OBJECT_ID(N'dbo.Departments', N'U') IS NOT NULL 
                       AND OBJECT_ID(N'dbo.FK_Departments_Branches_BranchId', N'F') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Departments] WITH NOCHECK ADD CONSTRAINT [FK_Departments_Branches_BranchId] FOREIGN KEY([BranchId]) REFERENCES [dbo].[Branches]([Id]);
                    END

                    IF OBJECT_ID(N'dbo.Warehouses', N'U') IS NOT NULL 
                       AND OBJECT_ID(N'dbo.FK_Warehouses_Branches_BranchId', N'F') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Warehouses] WITH NOCHECK ADD CONSTRAINT [FK_Warehouses_Branches_BranchId] FOREIGN KEY([BranchId]) REFERENCES [dbo].[Branches]([Id]);
                    END

                    IF OBJECT_ID(N'dbo.Employees', N'U') IS NOT NULL 
                       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employees_DepartmentId' AND object_id = OBJECT_ID(N'dbo.Employees'))
                    BEGIN
                        CREATE NONCLUSTERED INDEX [IX_Employees_DepartmentId] ON [dbo].[Employees]([DepartmentId]);
                    END

                    IF OBJECT_ID(N'dbo.Candidates', N'U') IS NOT NULL 
                       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Candidates_JobPostingId' AND object_id = OBJECT_ID(N'dbo.Candidates'))
                    BEGIN
                        CREATE NONCLUSTERED INDEX [IX_Candidates_JobPostingId] ON [dbo].[Candidates]([JobPostingId]);
                    END
",
                    new Microsoft.Data.SqlClient.SqlParameter("@baseline", baseline));
            }

            await context.Database.MigrateAsync();
        }

        /// <summary>
        /// SeedCore: bổ sung cột/bảng cho dữ liệu đã có, quyền, tài khoản quản trị và cấu hình site.
        /// Không tạo dữ liệu nghiệp vụ mẫu và không tạo tài khoản đăng nhập mẫu.
        /// </summary>
        private static async Task SeedCoreDataAsync(AppDbContext context, string? initialAdminPassword)
        {
            await SeedAuthAndPermissionsAsync(context);
            await SeedAdminAccountsAsync(context, initialAdminPassword);
            await SeedSiteSettingsAsync(context);
            await SeedPayrollSettingsAsync(context);
        }

        /// <summary>
        /// Loại phụ cấp mặc định cho bảng lương. Chỉ thêm khi bảng còn trống, không ghi đè cấu hình đã chỉnh.
        /// Cờ miễn thuế của cả hai loại đang là TODO: cần kế toán xác nhận trước khi dùng thật.
        /// </summary>
        private static async Task SeedPayrollSettingsAsync(AppDbContext context)
        {
            if (await context.PayrollAllowanceTypes.AnyAsync()) return;

            context.PayrollAllowanceTypes.AddRange(
                new PayrollAllowanceType
                {
                    Code = "LUNCH",
                    Name = "Phụ cấp ăn trưa",
                    DefaultAmount = 730000m,
                    IsTaxExempt = true,
                    TaxExemptLimit = 730000m,
                    SourceNote = "TODO: xác nhận hạn mức miễn thuế tiền ăn giữa ca và điều kiện áp dụng (chưa đối chiếu được nguồn).",
                    NeedsConfirmation = true
                },
                new PayrollAllowanceType
                {
                    Code = "FUEL_PHONE",
                    Name = "Phụ cấp xăng xe, điện thoại",
                    DefaultAmount = 500000m,
                    IsTaxExempt = false,
                    TaxExemptLimit = null,
                    SourceNote = "TODO: xác nhận có chịu thuế TNCN hay không. Cấu hình cũ coi là miễn thuế; mặc định mới là chịu thuế.",
                    NeedsConfirmation = true
                });

            await context.SaveChangesAsync();
        }

        /// <summary>SeedDemo: dữ liệu nhân sự, CMS, bán hàng, kho, mua hàng và lương mẫu.</summary>
        private static async Task SeedDemoDataAsync(AppDbContext context)
        {
            await SeedHrmDataAsync(context);
            await AssignDemoRolesAsync(context);
            await SeedCmsDataAsync(context);
            await SeedSalesAndInventoryDataAsync(context);
            await SeedScmDataAsync(context);
            await SeedHrmAdvancedDataAsync(context);
            await Seed200EmployeesAsync(context);
        }

        private static async Task SeedAuthAndPermissionsAsync(AppDbContext context)
        {
            // Danh sách Modules
            var modules = new List<Module>
            {
                new Module { Code = "auth", Name = "Quản trị Người dùng & Phân quyền", Icon = "bi-shield-lock", OrderIndex = 1 },
                new Module { Code = "hrm_emp", Name = "Quản lý Nhân sự & Hợp đồng", Icon = "bi-people", OrderIndex = 2 },
                new Module { Code = "hrm_att", Name = "Quản lý Ca & Chấm công", Icon = "bi-calendar-check", OrderIndex = 3 },
                new Module { Code = "cms_menu", Name = "Quản trị Menu Trang web", Icon = "bi-list-stars", OrderIndex = 4 },
                new Module { Code = "cms_content", Name = "Quản trị Nội dung & Banner", Icon = "bi-newspaper", OrderIndex = 5 },
                new Module { Code = "cms_layout", Name = "Giao diện & Banner", Icon = "bi-layout-text-window-reverse", OrderIndex = 6 },
                new Module { Code = "product", Name = "Quản lý Hàng hóa & Danh mục", Icon = "bi-box-seam", OrderIndex = 7 },
                new Module { Code = "order", Name = "Quản lý Đơn hàng & POS", Icon = "bi-cart-check", OrderIndex = 8 },
                new Module { Code = "inventory", Name = "Quản trị Kho & Điều chuyển", Icon = "bi-building-down", OrderIndex = 9 },
                new Module { Code = "report", Name = "Báo cáo & Thống kê Tổng quan", Icon = "bi-bar-chart", OrderIndex = 10 },
                new Module { Code = "scm", Name = "Quản lý Chuỗi Cung Ứng (SCM & PO)", Icon = "bi-truck", OrderIndex = 11 },
                new Module { Code = "debt", Name = "Quản trị Công Nợ Doanh Nghiệp", Icon = "bi-cash-coin", OrderIndex = 12 },
                new Module { Code = "hrm_leave", Name = "Quản lý Đơn từ & Phê duyệt", Icon = "bi-file-earmark-check", OrderIndex = 13 },
                new Module { Code = "hrm_payroll", Name = "Bảng lương & Chế độ phúc lợi", Icon = "bi-cash-stack", OrderIndex = 14 }
            };

            foreach (var mod in modules)
            {
                if (!await context.Modules.AnyAsync(m => m.Code.ToLower() == mod.Code.ToLower()))
                {
                    context.Modules.Add(mod);
                }
            }
            await context.SaveChangesAsync();

            var allModules = await context.Modules.ToListAsync();
            int GetModId(string code) =>
                allModules.FirstOrDefault(m => string.Equals(m.Code, code, StringComparison.OrdinalIgnoreCase))?.Id ?? allModules.First().Id;

            // Danh sách Permissions chuẩn mapping với các Controller Actions
            var permissions = new List<(string ModCode, string Code, string Name)>
            {
                // Auth & Matrix
                ("auth", "auth.matrix.view", "Xem ma trận phân quyền"),
                ("auth", "auth.matrix.update", "Cập nhật ma trận phân quyền"),
                ("auth", "auth.view", "Xem nhật ký kiểm toán hệ thống"),
                ("auth", "auth.user.manage", "Quản lý tài khoản: duyệt, tạo, khóa, gán vai trò"),
                ("auth", "auth.user.resetpassword", "Đặt lại mật khẩu tài khoản (tài khoản quản trị tổng chỉ SuperAdmin)"),

                // HRM Nhân viên & Hợp đồng
                ("hrm_emp", "hrm.employee.view", "Xem danh sách nhân viên & hồ sơ"),
                ("hrm_emp", "hrm.employee.manage", "Quản lý hồ sơ nhân viên & hợp đồng"),

                // HRM Chấm công
                ("hrm_att", "hrm.attendance.view", "Xem bảng chấm công"),
                ("hrm_att", "hrm.attendance.manage", "Quản lý chấm công & điểm danh"),

                // CMS Menu
                ("cms_menu", "menu.view", "Xem danh sách menu"),
                ("cms_menu", "menu.create", "Thêm menu mới"),
                ("cms_menu", "menu.edit", "Chỉnh sửa menu"),
                ("cms_menu", "menu.delete", "Xóa menu"),

                // CMS Content & Layout
                ("cms_content", "content.view", "Xem danh sách bài viết"),
                ("cms_content", "content.create", "Tạo bài viết mới"),
                ("cms_content", "content.edit", "Sửa bài viết"),
                ("cms_content", "content.delete", "Xóa bài viết"),
                ("cms_layout", "layout.manage", "Quản lý giao diện & Banner"),

                // Bán hàng - Sản phẩm
                ("product", "product.view", "Xem danh mục & sản phẩm"),
                ("product", "product.manage", "Quản lý sản phẩm (Thêm/Sửa/Xóa)"),

                // Bán hàng - Đơn hàng & POS
                ("order", "order.pos", "Bán hàng tại quầy POS"),
                ("order", "order.view", "Xem danh sách đơn hàng"),
                ("order", "order.manage", "Quản trị & Duyệt đơn hàng"),

                // Kho & Tồn kho
                ("inventory", "inventory.view", "Xem tồn kho thực tế"),
                ("inventory", "inventory.transaction", "Thực hiện nhập / xuất / điều chuyển kho"),
                ("inventory", "inventory.stocktaking", "Kiểm kê kho & Cân bằng kho"),

                // Báo cáo
                ("report", "report.sales", "Báo cáo doanh số bán hàng"),
                ("report", "report.hrm", "Báo cáo biến động nhân sự"),

                // SCM & Nhà Cung Cấp
                ("scm", "scm.supplier.view", "Xem danh sách nhà cung cấp"),
                ("scm", "scm.supplier.manage", "Quản lý nhà cung cấp"),
                ("scm", "scm.po.view", "Xem danh sách đơn mua hàng PO"),
                ("scm", "scm.po.manage", "Tạo, duyệt và nhận hàng đơn PO"),

                // Quản lý Công nợ
                ("debt", "debt.view", "Xem báo cáo công nợ & tuổi nợ"),
                ("debt", "debt.manage", "Ghi nhận thanh toán / thu nợ"),

                // Phân hệ Đơn từ & Phê duyệt (Phiên 4)
                ("hrm_leave", "hrm.leave.view", "Xem danh sách đơn từ trực tuyến"),
                ("hrm_leave", "hrm.leave.manage", "Tạo và nộp đơn từ trực tuyến"),
                ("hrm_leave", "hrm.leave.approve", "Phê duyệt hoặc từ chối đơn từ"),
                // Quyền tự phục vụ: chỉ thao tác trên dữ liệu của chính nhân viên đăng nhập
                ("hrm_leave", "hrm.self.service", "Tự phục vụ: tạo đơn từ và xem chấm công của chính mình"),

                // Phân hệ Bảng lương & Chi trả (Phiên 4)
                ("hrm_payroll", "hrm.payroll.view", "Xem bảng lương & phiếu lương"),
                ("hrm_payroll", "hrm.payroll.manage", "Tính toán & chốt bảng lương"),
                ("hrm_payroll", "hrm.payroll.send", "Gửi phiếu lương nhân sự")
            };

            foreach (var (modCode, permCode, permName) in permissions)
            {
                if (!await context.Permissions.AnyAsync(p => p.Code.ToLower() == permCode.ToLower()))
                {
                    context.Permissions.Add(new Permission
                    {
                        ModuleId = GetModId(modCode),
                        Code = permCode,
                        Name = permName
                    });
                }
            }
            await context.SaveChangesAsync();

            // Danh sách Roles chuẩn
            var roles = new List<Role>
            {
                new Role { Code = "SUPERADMIN", Name = "Quản trị viên Cấp cao", Description = "Toàn quyền hệ thống", IsSystem = true },
                new Role { Code = "HR_MANAGER", Name = "Trưởng phòng Nhân sự", Description = "Quản lý nhân viên, chấm công, hợp đồng", IsSystem = false },
                new Role { Code = "SALES_STAFF", Name = "Nhân viên Bán hàng", Description = "Bán hàng POS và xử lý đơn hàng", IsSystem = false },
                new Role { Code = "SALES_DIRECTOR", Name = "Giám đốc Kinh doanh", Description = "Quản lý đơn hàng, xem sản phẩm và báo cáo bán hàng", IsSystem = false },
                new Role { Code = "WAREHOUSE_STAFF", Name = "Thủ kho Chuyên trách", Description = "Quản lý tồn kho và nhập xuất kho", IsSystem = false },
                new Role { Code = "EMPLOYEE", Name = "Nhân viên", Description = "Tự phục vụ: đơn từ và chấm công của chính mình", IsSystem = false },
                new Role { Code = "ADMIN_MENU", Name = "Quản trị Menu website", Description = "Quản lý menu điều hướng website", IsSystem = false },
                new Role { Code = "ADMIN_CONTENT", Name = "Quản trị Nội dung", Description = "Quản lý bài viết, banner và giao diện", IsSystem = false },
                // Các admin nhóm: mỗi nhóm chỉ giữ đúng nhóm quyền của mình
                new Role { Code = "ADMIN_SANPHAM", Name = "Quản lý sản phẩm", Description = "Quản lý danh mục và sản phẩm", IsSystem = false },
                new Role { Code = "ADMIN_DONHANG", Name = "Quản lý đơn hàng", Description = "Quản lý đơn hàng, bán hàng và báo cáo bán hàng", IsSystem = false },
                new Role { Code = "ADMIN_NHANVIEN", Name = "Quản lý nhân viên", Description = "Quản lý hồ sơ, chấm công và đơn từ nhân viên", IsSystem = false },
                new Role { Code = "ADMIN_BANGLUONG", Name = "Quản lý lương", Description = "Tính lương, bảng lương và phiếu lương", IsSystem = false },
                new Role { Code = "ADMIN_KHO", Name = "Quản lý kho & mua hàng", Description = "Tồn kho, nhập xuất, đơn mua hàng và nhà cung cấp", IsSystem = false },
                new Role { Code = "BRANCH_MANAGER", Name = "Quản lý Chi nhánh", Description = "Quản lý vận hành, nhân sự, bán hàng và kho trong phạm vi chi nhánh", IsSystem = false },
                new Role { Code = "DEPARTMENT_HEAD", Name = "Trưởng phòng ban", Description = "Quản lý nhân sự, chấm công và duyệt đơn từ trong phạm vi phòng ban", IsSystem = false }
            };

            foreach (var r in roles)
            {
                if (!await context.Roles.AnyAsync(x => x.Code.ToUpper() == r.Code.ToUpper()))
                {
                    context.Roles.Add(r);
                }
            }
            await context.SaveChangesAsync();

            // Gán quyền mặc định theo vai trò: mỗi cặp (vai trò, quyền) chỉ được xét một lần (xem SeedHistory)
            await GanQuyenMacDinhMotLanAsync(context);
        }

        /// <summary>
        /// Quy tắc quyền mặc định theo vai trò. Quyền mới thêm vào quy tắc sẽ được gán đúng một lần ở lần khởi động sau.
        /// </summary>
        private static List<(string RoleCode, Func<Permission, bool> Chon)> QuyTacQuyenMacDinh() => new()
        {
            // SUPERADMIN có toàn bộ quyền
            ("SUPERADMIN", _ => true),
            // HR_MANAGER có các quyền nhân sự và chấm công
            ("HR_MANAGER", p => p.Code.StartsWith("hrm.")),
            // SALES_STAFF có quyền bán hàng, xem sản phẩm, đơn hàng
            ("SALES_STAFF", p => p.Code == "order.pos" || p.Code == "order.view" || p.Code == "product.view"),
            // SALES_DIRECTOR (Giám đốc Kinh doanh): quản trị đơn hàng, xem sản phẩm, báo cáo bán hàng
            ("SALES_DIRECTOR", p => p.Code.StartsWith("order.") || p.Code == "product.view" || p.Code == "report.sales"),
            // EMPLOYEE chỉ có quyền tự phục vụ
            ("EMPLOYEE", p => p.Code == "hrm.self.service"),
            // WAREHOUSE_STAFF có quyền xem và nhập xuất kho
            ("WAREHOUSE_STAFF", p => p.Code.StartsWith("inventory.") || p.Code == "product.view"),
            ("ADMIN_MENU", p => p.Code.StartsWith("menu.") || p.Code == "product.view"),
            ("ADMIN_CONTENT", p => p.Code.StartsWith("content.") || p.Code == "layout.manage"),
            // Mỗi admin nhóm chỉ nhận đúng nhóm mã quyền theo tiền tố
            ("ADMIN_SANPHAM", p => p.Code.StartsWith("product.") || p.Code == "inventory.view"),
            ("ADMIN_DONHANG", p => p.Code.StartsWith("order.") || p.Code == "debt.view" || p.Code == "report.sales"),
            ("ADMIN_NHANVIEN", p => p.Code.StartsWith("hrm.employee.") || p.Code.StartsWith("hrm.attendance.") || p.Code.StartsWith("hrm.leave.")),
            ("ADMIN_BANGLUONG", p => p.Code.StartsWith("hrm.payroll.") || p.Code == "hrm.employee.view"),
            ("ADMIN_KHO", p => p.Code.StartsWith("inventory.") || p.Code.StartsWith("scm.")),
            ("BRANCH_MANAGER", p => p.Code == "hrm.employee.view" || p.Code == "hrm.attendance.view" || p.Code.StartsWith("hrm.leave.") || p.Code.StartsWith("order.") || p.Code.StartsWith("inventory.") || p.Code == "product.view" || p.Code == "scm.po.view" || p.Code == "report.sales"),
            ("DEPARTMENT_HEAD", p => p.Code == "hrm.employee.view" || p.Code == "hrm.attendance.view" || p.Code.StartsWith("hrm.leave.")),
        };

        /// <summary>
        /// Gán quyền mặc định, nhưng không bao giờ khôi phục quyền mà quản trị viên đã gỡ:
        /// - DB mới (chưa có quyền nào): gán toàn bộ quy tắc và ghi lịch sử.
        /// - DB đã có từ trước, chưa có lịch sử: chỉ ghi nhận các cặp hiện có, KHÔNG gán lại.
        ///   Riêng SUPERADMIN nhận các quyền mới chưa thuộc vai trò nào.
        /// - Lần sau: chỉ gán các cặp (vai trò, quyền) chưa từng được xét (quyền mới của phiên bản mới).
        /// Hàm này an toàn khi chạy lại nhiều lần.
        /// </summary>
        public static async Task GanQuyenMacDinhMotLanAsync(AppDbContext context)
        {
            const string tienTo = "QUYEN:";

            var allPerms = await context.Permissions.ToListAsync();
            var allRoles = await context.Roles.ToListAsync();
            var daXet = (await context.SeedHistories.Select(h => h.SeedKey).ToListAsync()).ToHashSet(StringComparer.Ordinal);

            bool chuaCoLichSu = !daXet.Any(k => k.StartsWith(tienTo, StringComparison.Ordinal));
            bool dbDaCoQuyen = await context.RolePermissions.AnyAsync();
            var permDaCoVaiTro = (await context.RolePermissions.Select(rp => rp.PermissionId).Distinct().ToListAsync()).ToHashSet();

            foreach (var (roleCode, chon) in QuyTacQuyenMacDinh())
            {
                var role = allRoles.FirstOrDefault(r => string.Equals(r.Code, roleCode, StringComparison.OrdinalIgnoreCase));
                if (role == null) continue;

                foreach (var perm in allPerms.Where(chon))
                {
                    string key = $"{tienTo}{role.Code.ToUpperInvariant()}:{perm.Code}";
                    if (daXet.Contains(key)) continue; // đã xét trước đây: không gán lại dù quản trị viên đã gỡ

                    bool gan;
                    if (chuaCoLichSu && dbDaCoQuyen)
                    {
                        // DB có sẵn: chỉ SUPERADMIN nhận quyền mới chưa thuộc vai trò nào
                        gan = string.Equals(role.Code, "SUPERADMIN", StringComparison.OrdinalIgnoreCase)
                              && !permDaCoVaiTro.Contains(perm.Id);
                    }
                    else
                    {
                        gan = true; // DB mới, hoặc quyền mới của phiên bản sau
                    }

                    if (gan && !await context.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == perm.Id))
                    {
                        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
                    }

                    context.SeedHistories.Add(new SeedHistory { SeedKey = key });
                    daXet.Add(key);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedHrmDataAsync(AppDbContext context)
        {
            // 1. Công ty
            if (!await context.Companies.AnyAsync())
            {
                context.Companies.Add(new Company
                {
                    Code = "TPG",
                    Name = "TẬP ĐOÀN CÔNG NGHỆ TIÊN PHONG",
                    TaxCode = "0109998888",
                    Phone = "1900 6868",
                    Email = "contact@tienphong.vn",
                    Address = "Số 1 Duy Tân, Cầu Giấy, Hà Nội",
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }

            var company = await context.Companies.FirstAsync();

            // 2. Chi nhánh (2 chi nhánh mẫu)
            if (!await context.Branches.AnyAsync(b => b.Code == "CN-HN"))
            {
                context.Branches.Add(new Branch
                {
                    CompanyId = company.Id,
                    Code = "CN-HN",
                    Name = "Chi nhánh Hà Nội (Trụ sở chính)",
                    Phone = "024.3333.8888",
                    Address = "Số 1 Duy Tân, Cầu Giấy, Hà Nội",
                    IsActive = true
                });
            }

            if (!await context.Branches.AnyAsync(b => b.Code == "CN-HCM"))
            {
                context.Branches.Add(new Branch
                {
                    CompanyId = company.Id,
                    Code = "CN-HCM",
                    Name = "Chi nhánh TP. Hồ Chí Minh",
                    Phone = "028.3333.9999",
                    Address = "Số 123 Nguyễn Thị Minh Khai, Quận 1, TP. HCM",
                    IsActive = true
                });
            }
            await context.SaveChangesAsync();

            var branchHN = await context.Branches.FirstAsync(b => b.Code == "CN-HN");

            // 3. Phòng ban (4 phòng ban)
            var depts = new List<Department>
            {
                new Department { BranchId = branchHN.Id, Code = "PB-IT", Name = "Phòng Công nghệ & Hệ thống", IsActive = true },
                new Department { BranchId = branchHN.Id, Code = "PB-HR", Name = "Phòng Nhân sự & Đào tạo", IsActive = true },
                new Department { BranchId = branchHN.Id, Code = "PB-KD", Name = "Phòng Kinh doanh & Bán lẻ", IsActive = true },
                new Department { BranchId = branchHN.Id, Code = "PB-KHO", Name = "Phòng Quản lý Kho vận", IsActive = true }
            };

            foreach (var d in depts)
            {
                if (!await context.Departments.AnyAsync(x => x.Code == d.Code))
                {
                    context.Departments.Add(d);
                }
            }
            await context.SaveChangesAsync();

            // 4. Chức vụ (Positions)
            var positions = new List<Position>
            {
                new Position { Code = "CEO", Name = "Tổng Giám Đốc", BaseSalary = 50000000, AllowanceRate = 0.5m, IsActive = true },
                new Position { Code = "TP-HR", Name = "Trưởng Phòng Nhân Sự", BaseSalary = 25000000, AllowanceRate = 0.2m, IsActive = true },
                new Position { Code = "NV-KD", Name = "Nhân Viên Kinh Doanh", BaseSalary = 12000000, AllowanceRate = 0.1m, IsActive = true },
                new Position { Code = "NV-KHO", Name = "Thủ Kho Chuyên Trách", BaseSalary = 10000000, AllowanceRate = 0.1m, IsActive = true }
            };

            foreach (var p in positions)
            {
                if (!await context.Positions.AnyAsync(x => x.Code == p.Code))
                {
                    context.Positions.Add(p);
                }
            }
            await context.SaveChangesAsync();

            // 5. Ca làm việc chuẩn (Shifts)
            if (!await context.Shifts.AnyAsync(s => s.Code == "HC"))
            {
                context.Shifts.Add(new Shift
                {
                    Code = "HC",
                    Name = "Ca Hành Chính (8h00 - 17h30)",
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(17, 30, 0),
                    BreakStartTime = new TimeSpan(12, 0, 0),
                    BreakEndTime = new TimeSpan(13, 30, 0),
                    WorkHours = 8.0m,
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }

            // 6. Đảm bảo EmployeeDetail và Contract cho các nhân viên hiện có
            var employees = await context.Employees.ToListAsync();
            foreach (var emp in employees)
            {
                if (!await context.EmployeeDetails.AnyAsync(ed => ed.EmployeeId == emp.Id))
                {
                    context.EmployeeDetails.Add(new EmployeeDetail
                    {
                        EmployeeId = emp.Id,
                        CurrentAddress = "Hà Nội",
                        EmergencyContactName = "Người thân",
                        EmergencyContactPhone = "0988111222",
                        BankAccountNumber = "190333888999",
                        BankName = "Techcombank",
                        EducationLevel = "Đại học"
                    });
                }

                if (!await context.Contracts.AnyAsync(c => c.EmployeeId == emp.Id))
                {
                    context.Contracts.Add(new Contract
                    {
                        EmployeeId = emp.Id,
                        ContractNumber = $"HDLD-{emp.EmployeeCode}",
                        ContractType = "Xác định thời hạn 12 tháng",
                        StartDate = new DateTime(2024, 1, 1),
                        EndDate = new DateTime(2025, 1, 1),
                        BasicSalary = 15000000,
                        InsuranceSalary = 5000000,
                        Status = "Active",
                        SignDate = new DateTime(2023, 12, 28)
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private const string AdminUsername = "admin";
        private const string QuanTriTongUsername = "quantritong";

        /// <summary>
        /// Kiểm tra cấu hình khi phải tạo tài khoản quản trị lần đầu.
        /// Thiếu mật khẩu thì dừng khởi động: không sinh mật khẩu ngẫu nhiên và không in ra console.
        /// </summary>
        public static void EnsureInitialAdminPasswordConfigured(bool accountMissing, string? initialAdminPassword)
        {
            if (accountMissing && string.IsNullOrWhiteSpace(initialAdminPassword))
            {
                throw new InvalidOperationException(
                    "Thiếu cấu hình SeedAdmin:Password. Cần mật khẩu ban đầu cho tài khoản quản trị khi khởi tạo lần đầu. " +
                    "Ở Development đặt bằng User Secrets; ở môi trường khác đặt biến môi trường SeedAdmin__Password.");
            }
        }

        /// <summary>
        /// Tạo tài khoản "admin" và "quantritong" (IsSuperAdmin = true) nếu chưa có.
        /// Không ghi đè mật khẩu của tài khoản đã tồn tại.
        /// </summary>
        private static async Task SeedAdminAccountsAsync(AppDbContext context, string? initialAdminPassword)
        {
            bool adminMissing = !await context.Users.AnyAsync(u => u.Username == AdminUsername);
            bool quanTriTongMissing = !await context.Users.AnyAsync(u => u.Username == QuanTriTongUsername);

            EnsureInitialAdminPasswordConfigured(adminMissing || quanTriTongMissing, initialAdminPassword);

            if (adminMissing)
            {
                await CreateSuperAdminAccountAsync(context, AdminUsername, initialAdminPassword!);
            }
            if (quanTriTongMissing)
            {
                await CreateSuperAdminAccountAsync(context, QuanTriTongUsername, initialAdminPassword!);
            }

            // Mọi tài khoản quản trị tổng đều có vai trò SUPERADMIN
            var superRole = await context.Roles.FirstOrDefaultAsync(r => r.Code.ToUpper() == "SUPERADMIN");
            if (superRole != null)
            {
                foreach (var username in new[] { AdminUsername, QuanTriTongUsername })
                {
                    var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
                    if (user != null && !await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == superRole.Id))
                    {
                        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = superRole.Id });
                    }
                }
                await context.SaveChangesAsync();
            }
        }

        private static async Task CreateSuperAdminAccountAsync(AppDbContext context, string username, string password)
        {
            string salt = MatKhauHelper.GenerateSalt();
            context.Users.Add(new User
            {
                Username = username,
                PasswordSalt = salt,
                PasswordHash = MatKhauHelper.HashPassword(password, salt),
                IsActive = true,
                IsSuperAdmin = true,
                EmployeeId = null,
                CreatedAt = VietnamTime.Now
            });
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Gán vai trò cho các tài khoản demo nếu chúng tồn tại (chỉ chạy trong SeedDemo).
        /// Không đổi mật khẩu của các tài khoản này.
        /// </summary>
        private static async Task AssignDemoRolesAsync(AppDbContext context)
        {
            var demoUsers = new (string Username, string RoleCode)[]
            {
                ("superadmin", "SUPERADMIN"),
                ("ketoan", "ADMIN_BANGLUONG"),
                ("qtvp", "HR_MANAGER"),
                ("hr_manager", "HR_MANAGER"),
                ("salestaff", "SALES_STAFF"),
                ("sales_staff", "SALES_STAFF"),
                ("warehouse", "WAREHOUSE_STAFF"),
                ("warehouse_staff", "WAREHOUSE_STAFF")
            };

            foreach (var (username, roleCode) in demoUsers)
            {
                var role = await context.Roles.FirstOrDefaultAsync(r => r.Code.ToUpper() == roleCode.ToUpper());
                if (role == null) continue;

                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    string salt = MatKhauHelper.GenerateSalt();
                    user = new User
                    {
                        Username = username,
                        PasswordSalt = salt,
                        PasswordHash = MatKhauHelper.HashPassword("Admin@123456", salt),
                        IsActive = true,
                        IsSuperAdmin = (roleCode == "SUPERADMIN"),
                        CreatedAt = VietnamTime.Now
                    };
                    context.Users.Add(user);
                    await context.SaveChangesAsync();
                }

                if (!await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id))
                {
                    context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                }
            }
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Cấu hình site (tên thương hiệu, hotline, email, địa chỉ, bản quyền). Thêm các khóa còn thiếu, không ghi đè giá trị đã có.
        /// </summary>
        private static async Task SeedSiteSettingsAsync(AppDbContext context)
        {
            var settings = new List<SiteSetting>
            {
                new SiteSetting { SettingKey = "Site_Name", SettingValue = "TIEN PHONG ENTERPRISE", SettingGroup = "General", Description = "Tên hiển thị hệ thống" },
                new SiteSetting { SettingKey = "Site_Short_Name", SettingValue = "TIEN PHONG ERP", SettingGroup = "General", Description = "Tên ngắn trên thanh điều hướng quản trị và trang đăng nhập" },
                new SiteSetting { SettingKey = "Site_Hotline", SettingValue = "1900 6868", SettingGroup = "General", Description = "Hotline liên hệ" },
                new SiteSetting { SettingKey = "Site_Email", SettingValue = "contact@tienphong.vn", SettingGroup = "General", Description = "Email hỗ trợ" },
                new SiteSetting { SettingKey = "Site_Address", SettingValue = "Số 1 Duy Tân, Cầu Giấy, Hà Nội", SettingGroup = "General", Description = "Địa chỉ trụ sở" },
                new SiteSetting { SettingKey = "Site_Copyright", SettingValue = "© 2026 Tien Phong Corp. All rights reserved.", SettingGroup = "General", Description = "Bản quyền chân trang" }
            };

            foreach (var s in settings)
            {
                if (!await context.SiteSettings.AnyAsync(x => x.SettingKey == s.SettingKey))
                {
                    context.SiteSettings.Add(s);
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedCmsDataAsync(AppDbContext context)
        {
            // 1. Cấu hình site: xem SeedSiteSettingsAsync (SeedCore)

            // 2. Banners
            if (!await context.Banners.AnyAsync())
            {
                context.Banners.AddRange(
                    new Banner
                    {
                        Title = "Giải Pháp Quản Trị Doanh Nghiệp Toàn Diện",
                        SubTitle = "Tích hợp HRM, CMS, Bán Hàng & Quản Trị Kho Vận Tinh Gọn",
                        ImageUrl = "/images/banner-1.svg",
                        LinkUrl = "/BanHang/POS",
                        Position = "HomeSlider",
                        OrderIndex = 1,
                        IsActive = true
                    },
                    new Banner
                    {
                        Title = "Tối Ưu Vận Hành - Nâng Tầm Doanh Nghiệp",
                        SubTitle = "Kiểm soát dòng tiền, đơn hàng và kho tức thời",
                        ImageUrl = "/images/banner-2.svg",
                        LinkUrl = "/Public/TinTuc",
                        Position = "HomeSlider",
                        OrderIndex = 2,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // 3. Menus
            if (!await context.Menus.AnyAsync())
            {
                context.Menus.AddRange(
                    new Menu { Title = "Trang Chủ", CustomUrl = "/", Position = "MainNav", OrderIndex = 1, LinkType = "CustomUrl", IsActive = true },
                    new Menu { Title = "Bán Hàng POS", CustomUrl = "/BanHang/POS", Position = "MainNav", OrderIndex = 2, LinkType = "FunctionPage", IsActive = true },
                    new Menu { Title = "Kho Hàng", CustomUrl = "/Kho/TonKho", Position = "MainNav", OrderIndex = 3, LinkType = "FunctionPage", IsActive = true },
                    new Menu { Title = "Tin Tức & Hoạt Động", CustomUrl = "/Public/TinTuc", Position = "MainNav", OrderIndex = 4, LinkType = "FunctionPage", IsActive = true },
                    new Menu { Title = "Giới Thiệu", CustomUrl = "/Public/GioiThieu", Position = "MainNav", OrderIndex = 5, LinkType = "FunctionPage", IsActive = true },
                    new Menu { Title = "Liên Hệ", CustomUrl = "/Public/LienHe", Position = "MainNav", OrderIndex = 6, LinkType = "FunctionPage", IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            // 4. Contents (Bài viết mẫu)
            if (!await context.Contents.AnyAsync())
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                context.Contents.AddRange(
                    new Content
                    {
                        Title = "Khai trương Trung tâm Vận hành & Phân phối mới tại TP. Hồ Chí Minh",
                        Slug = "khai-truong-trung-tam-phan-phoi-tp-hcm",
                        Summary = "Mở rộng mạng lưới kho vận và phân phối hiện đại nhằm phục vụ khách hàng khu vực phía Nam với chuẩn giao hàng siêu tốc.",
                        BodyHtml = "<p>Ngày 15/01/2026, Tập đoàn Tiên Phong chính thức đưa vào vận hành trung tâm phân phối và kho vận quy mô lớn tại TP. HCM. Hệ thống được trang bị phần mềm ERP tích hợp quản lý kho thông minh giúp tối ưu hóa 40% thời gian xử lý đơn hàng.</p>",
                        ThumbnailUrl = "/images/news-1.svg",
                        Status = "Published",
                        AuthorId = adminUser?.Id,
                        CreatedAt = VietnamTime.Now
                    },
                    new Content
                    {
                        Title = "Tiên Phong Enterprise công bố hệ sinh thái quản trị số phiên bản 2026",
                        Slug = "tien-phong-cong-bo-he-sinh-thai-2026",
                        Summary = "Đột phá kiến trúc tích hợp liên thông HRM, CMS và Bán lẻ, mang lại giải pháp toàn diện cho doanh nghiệp Việt Nam.",
                        BodyHtml = "<p>Hệ sinh thái ERP_HRM_CMS mới kết nối liền mạch từ khâu quản lý hồ sơ nhân viên, tính công tự động tới quản trị kho hàng đa chi nhánh và điểm bán lẻ POS tập trung.</p>",
                        ThumbnailUrl = "/images/news-2.svg",
                        Status = "Published",
                        AuthorId = adminUser?.Id,
                        CreatedAt = VietnamTime.Now
                    }
                );
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSalesAndInventoryDataAsync(AppDbContext context)
        {
            // 1. Đảm bảo có ít nhất 3 Danh mục sản phẩm (Categories)
            var categories = new List<Category>
            {
                new Category { Code = "CAT-TBVP", Name = "Thiết bị Tin học & Văn phòng", Slug = "thiet-bi-tin-hoc-van-phong", OrderIndex = 1, IsActive = true },
                new Category { Code = "CAT-SRV", Name = "Giải pháp Máy chủ & Lưu trữ", Slug = "giai-phap-may-chu-luu-tru", OrderIndex = 2, IsActive = true },
                new Category { Code = "CAT-OFFICE", Name = "Văn phòng phẩm & Tiện ích VP", Slug = "van-phong-pham-tien-ich", OrderIndex = 3, IsActive = true }
            };

            foreach (var cat in categories)
            {
                if (!await context.Categories.AnyAsync(c => c.Code == cat.Code))
                {
                    context.Categories.Add(cat);
                }
            }
            await context.SaveChangesAsync();

            var catDict = await context.Categories.ToDictionaryAsync(c => c.Code, c => c.Id);

            // 2. Đảm bảo có đủ 5 Sản phẩm mẫu với hình ảnh thật
            var sampleProducts = new List<(string SKU, string Name, string Slug, string CatCode, string Unit, decimal Cost, decimal Price, string Img, string Desc)>
            {
                ("DELL-LAT-5440", "Laptop Dell Latitude 5440 Core i7-1365U", "laptop-dell-latitude-5440", "CAT-TBVP", "Chiếc", 19500000, 23500000, "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?auto=format&fit=crop&w=500&q=80", "Laptop doanh nhân hiệu năng cao, màn hình 14 inch FHD IPS, RAM 16GB, SSD 512GB."),
                ("LOGI-MX-M3S", "Chuột Không dây Logitech MX Master 3S", "chuot-khong-day-logitech-mx-master-3s", "CAT-TBVP", "Chiếc", 1850000, 2450000, "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?auto=format&fit=crop&w=500&q=80", "Chuột công thái học cao cấp, cảm biến 8000 DPI, cuộn MagSpeed siêu tốc."),
                ("SRV-DL380-G10", "Máy chủ HP ProLiant DL380 Gen10 Plus", "may-chu-hp-proliant-dl380-gen10", "CAT-SRV", "Bộ", 65000000, 78000000, "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?auto=format&fit=crop&w=500&q=80", "Máy chủ rack 2U tiêu chuẩn doanh nghiệp, 2 socket Intel Xeon Silver, 64GB RAM ECC."),
                ("DELL-U2422H", "Màn hình Dell UltraSharp U2422H 24 inch FHD", "man-hinh-dell-ultrasharp-u2422h", "CAT-TBVP", "Chiếc", 5200000, 6390000, "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?auto=format&fit=crop&w=500&q=80", "Màn hình đồ họa cao cấp 100% sRGB, viền siêu mỏng InfinityEdge, chuẩn kết nối USB-C."),
                ("PAPER-A4-70", "Giấy in Double A A4 70gsm (Thùng 5 ram)", "giay-in-double-a-a4-70gsm", "CAT-OFFICE", "Thùng", 290000, 370000, "https://images.unsplash.com/photo-1586075010923-2dd4570fb338?auto=format&fit=crop&w=500&q=80", "Giấy in trắng sáng độ mịn cao, chống kẹt giấy hoàn hảo cho in ấn văn phòng.")
            };

            foreach (var p in sampleProducts)
            {
                if (!await context.Products.AnyAsync(x => x.SKU == p.SKU))
                {
                    context.Products.Add(new Product
                    {
                        SKU = p.SKU,
                        Name = p.Name,
                        Slug = p.Slug,
                        CategoryId = catDict.ContainsKey(p.CatCode) ? catDict[p.CatCode] : catDict.Values.First(),
                        Unit = p.Unit,
                        CostPrice = p.Cost,
                        SalePrice = p.Price,
                        ImageUrl = p.Img,
                        Description = p.Desc,
                        IsActive = true
                    });
                }
            }
            await context.SaveChangesAsync();

            // 3. Tồn kho ban đầu (Inventories) cho từng Chi nhánh
            var branches = await context.Branches.ToListAsync();
            var products = await context.Products.ToListAsync();

            foreach (var b in branches)
            {
                foreach (var prod in products)
                {
                    if (!await context.Inventories.AnyAsync(i => i.ProductId == prod.Id && i.BranchId == b.Id))
                    {
                        int initialStock = b.Code == "CN-HN" ? 100 : 50;
                        context.Inventories.Add(new Inventory
                        {
                            ProductId = prod.Id,
                            BranchId = b.Id,
                            StockQuantity = initialStock,
                            MinStockAlert = 5,
                            MaxStockLimit = 500,
                            UpdatedAt = VietnamTime.Now
                        });
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedScmDataAsync(AppDbContext context)
        {
            // 1. Nhà cung cấp mẫu
            if (!await context.Suppliers.AnyAsync())
            {
                var suppliers = new List<Supplier>
                {
                    new Supplier
                    {
                        Code = "NCC-DELL",
                        Name = "Dell Technologies Vietnam",
                        TaxCode = "0302839182",
                        Phone = "028 3823 8888",
                        Email = "sales.vietnam@dell.com",
                        Address = "Tòa nhà Bitexco, Q.1, TP. Hồ Chí Minh",
                        TotalDebt = 150000000,
                        IsActive = true
                    },
                    new Supplier
                    {
                        Code = "NCC-HP",
                        Name = "Hewlett Packard Enterprise (HPE) Vietnam",
                        TaxCode = "0309928371",
                        Phone = "024 3933 6688",
                        Email = "partner.vn@hpe.com",
                        Address = "Tòa nhà CornerStone, 16 Phan Chu Trinh, Hoàn Kiếm, Hà Nội",
                        TotalDebt = 52000000,
                        IsActive = true
                    },
                    new Supplier
                    {
                        Code = "NCC-FPT",
                        Name = "Synnex FPT Distribution",
                        TaxCode = "0101778163",
                        Phone = "024 7300 6666",
                        Email = "scm@synnexfpt.com.vn",
                        Address = "Tòa nhà FPT Cầu Giấy, Phố Duy Tân, Hà Nội",
                        TotalDebt = 0,
                        IsActive = true
                    },
                    new Supplier
                    {
                        Code = "NCC-CISCO",
                        Name = "Cisco Systems Distribution Partner",
                        TaxCode = "0102938475",
                        Phone = "028 7309 8899",
                        Email = "channel@cisco-dist.vn",
                        Address = "Tầng 12, Saigon Centre, 65 Lê Lợi, Q.1, TP. HCM",
                        TotalDebt = 45000000,
                        IsActive = true
                    }
                };

                context.Suppliers.AddRange(suppliers);
                await context.SaveChangesAsync();
            }

            var dellSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Code == "NCC-DELL");
            var hpSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Code == "NCC-HP");
            var ciscoSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Code == "NCC-CISCO");
            var hanoiBranch = await context.Branches.FirstOrDefaultAsync(b => b.Code == "CN-HN") ?? await context.Branches.FirstAsync();
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin") ?? await context.Users.FirstAsync();
            var products = await context.Products.ToListAsync();

            // 2. Đơn đặt mua hàng (PO - Purchase Order)
            if (!await context.PurchaseOrders.AnyAsync() && hpSupplier != null && dellSupplier != null && products.Any())
            {
                var srvProd = products.FirstOrDefault(p => p.SKU == "SRV-DL380-G10") ?? products.First();
                var dellLaptop = products.FirstOrDefault(p => p.SKU == "DELL-LAT-5440") ?? products.First();
                var dellMonitor = products.FirstOrDefault(p => p.SKU == "DELL-U2422H") ?? products.First();

                // PO 1: Đã nhập kho
                var po1 = new PurchaseOrder
                {
                    POCode = "PO261001001",
                    SupplierId = hpSupplier.Id,
                    BranchId = hanoiBranch.Id,
                    OrderDate = VietnamTime.Now.AddDays(-5),
                    ExpectedDeliveryDate = VietnamTime.Now.AddDays(-2),
                    ReceivedDate = VietnamTime.Now.AddDays(-2),
                    TotalAmount = 82000000,
                    DiscountAmount = 0,
                    FinalAmount = 82000000,
                    PaidAmount = 30000000,
                    DebtAmount = 52000000,
                    Status = PurchaseOrderStatus.Received,
                    CreatedBy = adminUser.Id,
                    ApprovedBy = adminUser.Id,
                    Note = "Nhập lô máy chủ HP ProLiant cho dự án ngân hàng",
                    CreatedAt = VietnamTime.Now.AddDays(-5)
                };
                po1.PurchaseOrderDetails.Add(new PurchaseOrderDetail
                {
                    ProductId = srvProd.Id,
                    Quantity = 1,
                    UnitPrice = 65000000,
                    TotalPrice = 65000000,
                    Note = "Máy chủ chính hãng kèm bảo hành 3 năm"
                });
                po1.PurchaseOrderDetails.Add(new PurchaseOrderDetail
                {
                    ProductId = dellMonitor.Id,
                    Quantity = 3,
                    UnitPrice = 5200000,
                    TotalPrice = 15600000,
                    Note = "Màn hình kiểm thử NOC"
                });

                // PO 2: Đã duyệt, chờ kho nhận hàng
                var po2 = new PurchaseOrder
                {
                    POCode = "PO261002002",
                    SupplierId = dellSupplier.Id,
                    BranchId = hanoiBranch.Id,
                    OrderDate = VietnamTime.Now.AddDays(-1),
                    ExpectedDeliveryDate = VietnamTime.Now.AddDays(2),
                    TotalAmount = 195000000,
                    DiscountAmount = 0,
                    FinalAmount = 195000000,
                    PaidAmount = 45000000,
                    DebtAmount = 150000000,
                    Status = PurchaseOrderStatus.Approved,
                    CreatedBy = adminUser.Id,
                    ApprovedBy = adminUser.Id,
                    Note = "Nhập bổ sung 10 laptop Dell Latitude cho chi nhánh Hà Nội",
                    CreatedAt = VietnamTime.Now.AddDays(-1)
                };
                po2.PurchaseOrderDetails.Add(new PurchaseOrderDetail
                {
                    ProductId = dellLaptop.Id,
                    Quantity = 10,
                    UnitPrice = 19500000,
                    TotalPrice = 195000000,
                    Note = "Hàng đợt 1 tháng 10/2026"
                });

                // PO 3: Chờ duyệt
                var po3 = new PurchaseOrder
                {
                    POCode = "PO261002003",
                    SupplierId = ciscoSupplier?.Id ?? dellSupplier.Id,
                    BranchId = hanoiBranch.Id,
                    OrderDate = VietnamTime.Now,
                    ExpectedDeliveryDate = VietnamTime.Now.AddDays(5),
                    TotalAmount = 45000000,
                    DiscountAmount = 0,
                    FinalAmount = 45000000,
                    PaidAmount = 0,
                    DebtAmount = 45000000,
                    Status = PurchaseOrderStatus.PendingApproval,
                    CreatedBy = adminUser.Id,
                    Note = "Đề xuất nhập bổ sung thiết bị mạng dự phòng",
                    CreatedAt = VietnamTime.Now
                };
                po3.PurchaseOrderDetails.Add(new PurchaseOrderDetail
                {
                    ProductId = dellMonitor.Id,
                    Quantity = 8,
                    UnitPrice = 5200000,
                    TotalPrice = 41600000,
                    Note = "Màn hình làm việc phòng Kỹ thuật"
                });

                context.PurchaseOrders.AddRange(po1, po2, po3);
                await context.SaveChangesAsync();
            }

            // 3. Công nợ (DebtRecords)
            if (!await context.DebtRecords.AnyAsync())
            {
                var poReceived = await context.PurchaseOrders.FirstOrDefaultAsync(p => p.Status == PurchaseOrderStatus.Received);
                var debts = new List<DebtRecord>
                {
                    // Phải trả NCC HP
                    new DebtRecord
                    {
                        DebtType = "PAYABLE",
                        PartnerType = "SUPPLIER",
                        PartnerName = hpSupplier?.Name ?? "Hewlett Packard Enterprise",
                        PartnerPhone = hpSupplier?.Phone,
                        SupplierId = hpSupplier?.Id,
                        PurchaseOrderId = poReceived?.Id,
                        DocumentCode = poReceived?.POCode ?? "PO261001001",
                        TotalAmount = 82000000,
                        PaidAmount = 30000000,
                        RemainingAmount = 52000000,
                        DueDate = VietnamTime.Now.AddDays(10),
                        Status = DebtStatus.PartiallyPaid,
                        Note = "Thanh toán đợt 1 30tr, đợt 2 theo điều khoản tín dụng 15 ngày",
                        CreatedAt = VietnamTime.Now.AddDays(-5)
                    },
                    // Phải trả NCC Dell
                    new DebtRecord
                    {
                        DebtType = "PAYABLE",
                        PartnerType = "SUPPLIER",
                        PartnerName = dellSupplier?.Name ?? "Dell Technologies Vietnam",
                        PartnerPhone = dellSupplier?.Phone,
                        SupplierId = dellSupplier?.Id,
                        DocumentCode = "PO261002002",
                        TotalAmount = 195000000,
                        PaidAmount = 45000000,
                        RemainingAmount = 150000000,
                        DueDate = VietnamTime.Now.AddDays(25),
                        Status = DebtStatus.PartiallyPaid,
                        Note = "Đặt cọc trước 45tr, tất toán khi nhận đủ hóa đơn GTGT",
                        CreatedAt = VietnamTime.Now.AddDays(-1)
                    },
                    // Phải thu Khách hàng (Đơn hàng dự án)
                    new DebtRecord
                    {
                        DebtType = "RECEIVABLE",
                        PartnerType = "CUSTOMER",
                        PartnerName = "Tổng Công Ty Công Nghệ Viễn Thông Quân Đội",
                        PartnerPhone = "0988 123 456",
                        DocumentCode = "DH261001001",
                        TotalAmount = 75000000,
                        PaidAmount = 25000000,
                        RemainingAmount = 50000000,
                        DueDate = VietnamTime.Now.AddDays(7),
                        Status = DebtStatus.PartiallyPaid,
                        Note = "Hợp đồng cung cấp máy chủ & thiết bị ngoại vi, trả chậm 15 ngày",
                        CreatedAt = VietnamTime.Now.AddDays(-3)
                    },
                    // Phải thu Khách hàng (Quá hạn cảnh báo)
                    new DebtRecord
                    {
                        DebtType = "RECEIVABLE",
                        PartnerType = "CUSTOMER",
                        PartnerName = "Công Ty CP Đầu Tư Sao Mai Vina",
                        PartnerPhone = "0912 345 678",
                        DocumentCode = "DH260910002",
                        TotalAmount = 36500000,
                        PaidAmount = 0,
                        RemainingAmount = 36500000,
                        DueDate = VietnamTime.Now.AddDays(-12),
                        Status = DebtStatus.Overdue,
                        Note = "Đơn hàng mua sắm quý 3/2026 đã quá hạn 12 ngày, đã nhắc nợ lần 2",
                        CreatedAt = VietnamTime.Now.AddDays(-27)
                    }
                };

                context.DebtRecords.AddRange(debts);
                await context.SaveChangesAsync();

                // Lịch sử thanh toán mẫu
                string targetDocCode = poReceived?.POCode ?? "PO261001001";
                var hpDebt = await context.DebtRecords.FirstOrDefaultAsync(d => d.DocumentCode == targetDocCode);
                if (hpDebt != null)
                {
                    context.DebtPayments.Add(new DebtPayment
                    {
                        DebtRecordId = hpDebt.Id,
                        PaymentCode = "PC261001001",
                        Amount = 30000000,
                        PaymentMethod = "BANK_TRANSFER",
                        PaymentDate = VietnamTime.Now.AddDays(-4),
                        Note = "Ủy nhiệm chi tạm ứng đợt 1 ngân hàng Vietcombank",
                        CreatedBy = adminUser.Id
                    });
                    await context.SaveChangesAsync();
                }
            }

            // 4. Phiếu Kiểm kê Kho (Stocktaking)
            if (!await context.StocktakingSheets.AnyAsync() && products.Any())
            {
                var sheet = new StocktakingSheet
                {
                    Code = "KK261001001",
                    BranchId = hanoiBranch.Id,
                    CheckDate = VietnamTime.Now.AddDays(-3),
                    TotalSystemQty = 120,
                    TotalActualQty = 119,
                    TotalDifferenceQty = -1,
                    TotalDifferenceValue = -1850000,
                    Status = "Balanced",
                    CreatedBy = adminUser.Id,
                    Note = "Kiểm kê định kỳ đầu tháng 10/2026. Lệch 1 Chuột Logitech do rơi vỡ bảo hành.",
                    CreatedAt = VietnamTime.Now.AddDays(-3)
                };

                foreach (var prod in products.Take(3))
                {
                    int sys = prod.SKU == "LOGI-MX-M3S" ? 20 : 10;
                    int act = prod.SKU == "LOGI-MX-M3S" ? 19 : 10;
                    int diff = act - sys;
                    decimal diffVal = diff * prod.CostPrice;

                    sheet.StocktakingDetails.Add(new StocktakingDetail
                    {
                        ProductId = prod.Id,
                        SystemQuantity = sys,
                        ActualQuantity = act,
                        DifferenceQuantity = diff,
                        CostPrice = prod.CostPrice,
                        DifferenceValue = diffVal,
                        Reason = diff < 0 ? "Vỡ hỏng vỏ ngoài khi bốc xếp" : "Khớp số liệu"
                    });
                }

                context.StocktakingSheets.Add(sheet);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Nạp 200 nhân sự thực tế với đầy đủ hồ sơ chi tiết, hợp đồng lao động và quỹ phép năm.
        /// Đảm bảo tính lũy tiến (idempotent), tự bù đủ 200 nhân viên nếu cơ sở dữ liệu có ít hơn 200.
        /// </summary>
        public static async Task Seed200EmployeesAsync(AppDbContext context)
        {
            int currentCount = await context.Employees.CountAsync();
            if (currentCount >= 200) return;

            var branchHN = await context.Branches.FirstOrDefaultAsync(b => b.Code == "CN-HN") ?? await context.Branches.FirstAsync();
            var branchHCM = await context.Branches.FirstOrDefaultAsync(b => b.Code == "CN-HCM") ?? branchHN;

            // Bổ sung đầy đủ phòng ban thực tế nếu chưa có
            var extraDepts = new List<Department>
            {
                new Department { BranchId = branchHN.Id, Code = "PB-MKT", Name = "Phòng Marketing & Truyền Thông", IsActive = true },
                new Department { BranchId = branchHN.Id, Code = "PB-KT", Name = "Phòng Tài Chính - Kế Toán", IsActive = true },
                new Department { BranchId = branchHN.Id, Code = "PB-CSKH", Name = "Phòng Chăm Sóc Khách Hàng", IsActive = true },
                new Department { BranchId = branchHCM.Id, Code = "PB-CN-HCM", Name = "Chi Nhánh Miền Nam", IsActive = true }
            };

            foreach (var d in extraDepts)
            {
                if (!await context.Departments.AnyAsync(x => x.Code == d.Code))
                {
                    context.Departments.Add(d);
                }
            }
            await context.SaveChangesAsync();

            // Bổ sung các chức danh thực tế nếu chưa có
            var extraPositions = new List<Position>
            {
                new Position { Code = "TP-IT", Name = "Trưởng Phòng Công Nghệ", BaseSalary = 30000000, AllowanceRate = 0.25m, IsActive = true },
                new Position { Code = "DEV-SR", Name = "Kỹ Sư Phần Mềm Cao Cấp", BaseSalary = 24000000, AllowanceRate = 0.15m, IsActive = true },
                new Position { Code = "DEV-JR", Name = "Kỹ Sư Phần Mềm", BaseSalary = 14000000, AllowanceRate = 0.1m, IsActive = true },
                new Position { Code = "TP-KD", Name = "Trưởng Phòng Kinh Doanh", BaseSalary = 26000000, AllowanceRate = 0.25m, IsActive = true },
                new Position { Code = "TP-KT", Name = "Kế Toán Trưởng", BaseSalary = 25000000, AllowanceRate = 0.2m, IsActive = true },
                new Position { Code = "NV-KT", Name = "Chuyên Viên Kế Toán", BaseSalary = 13000000, AllowanceRate = 0.1m, IsActive = true },
                new Position { Code = "TP-MKT", Name = "Trưởng Phòng Marketing", BaseSalary = 23000000, AllowanceRate = 0.2m, IsActive = true },
                new Position { Code = "NV-MKT", Name = "Chuyên Viên Marketing", BaseSalary = 12000000, AllowanceRate = 0.1m, IsActive = true },
                new Position { Code = "NV-HR", Name = "Chuyên Viên Tuyển Dụng & C&B", BaseSalary = 13000000, AllowanceRate = 0.1m, IsActive = true },
                new Position { Code = "NV-CSKH", Name = "Chuyên Viên Chăm Sóc Khách Hàng", BaseSalary = 11000000, AllowanceRate = 0.08m, IsActive = true }
            };

            foreach (var p in extraPositions)
            {
                if (!await context.Positions.AnyAsync(x => x.Code == p.Code))
                {
                    context.Positions.Add(p);
                }
            }
            await context.SaveChangesAsync();

            // Đảm bảo có loại nghỉ phép năm
            var annualLeave = await context.LeaveTypes.FirstOrDefaultAsync(lt => lt.Code == "ANNUAL_LEAVE" || lt.Code == "ANNUAL");
            if (annualLeave == null)
            {
                annualLeave = new LeaveType
                {
                    Code = "ANNUAL_LEAVE",
                    Name = "Nghỉ phép năm",
                    MaxDaysPerYear = 12,
                    IsPaid = true,
                    AllowHalfDay = true,
                    Description = "Nghỉ phép năm hưởng nguyên lương theo Bộ luật Lao động",
                    IsActive = true
                };
                context.LeaveTypes.Add(annualLeave);
                await context.SaveChangesAsync();
            }

            var depts = await context.Departments.ToListAsync();
            var positions = await context.Positions.ToListAsync();
            var existingCodes = (await context.Employees.Select(e => e.EmployeeCode).ToListAsync()).ToHashSet();
            var existingEmails = (await context.Employees.Select(e => e.Email).ToListAsync()).ToHashSet();

            var lastNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đào", "Đoàn", "Mai", "Trịnh" };
            var maleNames = new[] {
                "Văn Hùng", "Minh Tuấn", "Hoàng Long", "Quang Huy", "Tuấn Anh",
                "Đức Thắng", "Quốc Bảo", "Hữu Thắng", "Tiến Dũng", "Trọng Nghĩa",
                "Đình Trọng", "Văn Nam", "Hải Đăng", "Thành Đạt", "Việt Anh",
                "Xuân Bách", "Minh Triết", "Phúc Thịnh", "Duy Khang", "Nhật Minh"
            };
            var femaleNames = new[] {
                "Thị Mai", "Thị Hương", "Thu Hà", "Ngọc Anh", "Thùy Linh",
                "Thanh Huyền", "Bích Ngọc", "Phương Thảo", "Mỹ Linh", "Khánh Vy",
                "Lan Anh", "Quỳnh Nga", "Diệu Linh", "Hoài Thương", "Kim Oanh",
                "Tuyết Mai", "Ánh Tuyết", "Cẩm Tú", "Thảo Nguyên", "Bảo Trâm"
            };
            var banks = new[] { "Techcombank", "Vietcombank", "MB Bank", "BIDV", "VPBank", "ACB", "TPBank" };
            var addresses = new[] {
                "Cầu Giấy, Hà Nội", "Đống Đa, Hà Nội", "Thanh Xuân, Hà Nội", "Nam Từ Liêm, Hà Nội",
                "Quận 1, TP. HCM", "Quận Bình Thạnh, TP. HCM", "Quận Tân Bình, TP. HCM", "TP. Thủ Đức, TP. HCM",
                "Hải Châu, Đà Nẵng", "Ngô Quyền, Hải Phòng", "TP. Bắc Ninh"
            };

            var newEmployees = new List<Employee>();

            for (int i = 1; i <= 200; i++)
            {
                string code = $"NV{i:D3}";
                if (existingCodes.Contains(code)) continue;

                bool isMale = (i % 2 != 0);
                string gender = isMale ? "Nam" : "Nữ";
                string lastName = lastNames[(i - 1) % lastNames.Length];
                string middleAndFirst = isMale
                    ? maleNames[(i / 2) % maleNames.Length]
                    : femaleNames[(i / 2) % femaleNames.Length];
                string fullName = $"{lastName} {middleAndFirst}";

                string email = $"nv{i:D3}@tienphong.vn";
                int emailSuffix = 1;
                while (existingEmails.Contains(email))
                {
                    email = $"nv{i:D3}.{emailSuffix}@tienphong.vn";
                    emailSuffix++;
                }
                existingEmails.Add(email);

                var dept = depts[i % depts.Count];
                var pos = positions[i % positions.Count];

                // Phân bổ trạng thái: ~85% Active, ~8% Probation, ~7% Resigned
                string status;
                DateTime hireDate;
                if (i % 15 == 0)
                {
                    status = "Resigned";
                    hireDate = new DateTime(2021 + (i % 4), 1 + (i % 12), 1 + (i % 26));
                }
                else if (i % 10 == 0)
                {
                    status = "Probation";
                    hireDate = VietnamTime.Today.AddDays(-(i % 45) - 5);
                }
                else
                {
                    status = "Active";
                    hireDate = new DateTime(2021 + (i % 5), 1 + (i % 12), 1 + (i % 26));
                }

                var emp = new Employee
                {
                    EmployeeCode = code,
                    FullName = fullName,
                    Gender = gender,
                    DateOfBirth = new DateTime(1986 + (i % 16), 1 + ((i * 3) % 12), 1 + ((i * 5) % 27)),
                    Email = email,
                    Phone = $"09{80000000 + i}",
                    IdentityCard = $"enc:v1:0{i:D11}",
                    DepartmentId = dept.Id,
                    PositionId = pos.Id,
                    Status = status,
                    HireDate = hireDate,
                    CreatedAt = hireDate
                };

                newEmployees.Add(emp);
            }

            // Lưu Employee theo từng đợt để lấy Id
            for (int chunk = 0; chunk < newEmployees.Count; chunk += 50)
            {
                var batch = newEmployees.Skip(chunk).Take(50).ToList();
                context.Employees.AddRange(batch);
                await context.SaveChangesAsync();

                foreach (var emp in batch)
                {
                    int empId = emp.Id;
                    int i = int.TryParse(emp.EmployeeCode.Replace("NV", ""), out int num) ? num : empId;
                    var empPos = positions.FirstOrDefault(p => p.Id == emp.PositionId) ?? positions.First();

                    // 1. EmployeeDetail
                    context.EmployeeDetails.Add(new EmployeeDetail
                    {
                        EmployeeId = empId,
                        CurrentAddress = addresses[i % addresses.Length],
                        BankAccountNumber = $"190333{i:D6}",
                        BankName = banks[i % banks.Length],
                        EmergencyContactName = $"Người thân {emp.FullName.Split(' ').FirstOrDefault()}",
                        EmergencyContactPhone = $"090{7000000 + i}",
                        EducationLevel = (i % 5 == 0) ? "Thạc sĩ" : ((i % 7 == 0) ? "Cao đẳng" : "Đại học")
                    });

                    // 2. Contract
                    decimal basicSal = empPos.BaseSalary > 0 ? empPos.BaseSalary : 12000000m;
                    string contractType;
                    DateTime? endDate = null;
                    string contractStatus = "Active";

                    if (emp.Status == "Probation")
                    {
                        contractType = "Hợp đồng thử việc 60 ngày";
                        endDate = emp.HireDate.AddDays(60);
                        basicSal = Math.Round(basicSal * 0.85m, 0);
                    }
                    else if (emp.Status == "Resigned")
                    {
                        contractType = "Hợp đồng đã thanh lý";
                        contractStatus = "Terminated";
                        endDate = emp.HireDate.AddYears(1);
                    }
                    else
                    {
                        if (i % 3 == 0)
                        {
                            contractType = "Hợp đồng lao động không thời hạn";
                            endDate = null;
                        }
                        else
                        {
                            contractType = "Hợp đồng lao động xác định thời hạn 12 tháng";
                            endDate = emp.HireDate.AddYears(1);
                        }
                    }

                    context.Contracts.Add(new Contract
                    {
                        EmployeeId = empId,
                        ContractNumber = $"HDLD-{emp.EmployeeCode}",
                        ContractType = contractType,
                        StartDate = emp.HireDate,
                        EndDate = endDate,
                        BasicSalary = basicSal,
                        InsuranceSalary = Math.Min(basicSal, 20000000m),
                        Status = contractStatus,
                        SignDate = emp.HireDate,
                        CreatedAt = emp.HireDate
                    });

                    // 3. EmployeeLeaveBalance
                    if (annualLeave != null)
                    {
                        decimal seniority = (2026 - emp.HireDate.Year >= 5) ? 1.00m : 0.00m;
                        decimal used = emp.Status == "Probation" ? 0.00m : (decimal)(i % 5);
                        context.EmployeeLeaveBalances.Add(new EmployeeLeaveBalance
                        {
                            EmployeeId = empId,
                            Year = 2026,
                            LeaveTypeId = annualLeave.Id,
                            StandardDays = 12.00m,
                            SeniorityBonusDays = seniority,
                            CarriedOverDays = (i % 3 == 0) ? 1.50m : 0.00m,
                            UsedDays = used,
                            RemainingDays = Math.Max(0m, 12.00m + seniority - used),
                            CreatedAt = emp.HireDate
                        });
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedHrmAdvancedDataAsync(AppDbContext context)
        {
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            if (adminUser == null) return;

            var depts = await context.Departments.ToListAsync();
            var positions = await context.Positions.ToListAsync();
            var shifts = await context.Shifts.ToListAsync();
            var hcShift = shifts.FirstOrDefault(s => s.Code == "HC") ?? shifts.FirstOrDefault();

            // 1. Đảm bảo có ít nhất 4 nhân sự mẫu với đầy đủ hồ sơ, chức vụ và hợp đồng
            if (!await context.Employees.AnyAsync())
            {
                var itDept = depts.FirstOrDefault(d => d.Code == "PB-IT") ?? depts.First();
                var hrDept = depts.FirstOrDefault(d => d.Code == "PB-HR") ?? depts.First();
                var kdDept = depts.FirstOrDefault(d => d.Code == "PB-KD") ?? depts.First();
                var khoDept = depts.FirstOrDefault(d => d.Code == "PB-KHO") ?? depts.First();

                var ceoPos = positions.FirstOrDefault(p => p.Code == "CEO") ?? positions.First();
                var hrPos = positions.FirstOrDefault(p => p.Code == "TP-HR") ?? positions.First();
                var kdPos = positions.FirstOrDefault(p => p.Code == "NV-KD") ?? positions.First();
                var khoPos = positions.FirstOrDefault(p => p.Code == "NV-KHO") ?? positions.First();

                var emps = new List<Employee>
                {
                    new Employee { EmployeeCode = "NV001", FullName = "Nguyễn Văn An", DepartmentId = itDept.Id, PositionId = ceoPos.Id, Email = "an.nguyen@tienphong.vn", Phone = "0988111222", Status = "Active", HireDate = new DateTime(2022, 1, 15) },
                    new Employee { EmployeeCode = "NV002", FullName = "Trần Thị Bích", DepartmentId = hrDept.Id, PositionId = hrPos.Id, Email = "bich.tran@tienphong.vn", Phone = "0988222333", Status = "Active", HireDate = new DateTime(2022, 6, 1) },
                    new Employee { EmployeeCode = "NV003", FullName = "Lê Hoàng Nam", DepartmentId = kdDept.Id, PositionId = kdPos.Id, Email = "nam.le@tienphong.vn", Phone = "0988333444", Status = "Active", HireDate = new DateTime(2023, 3, 10) },
                    new Employee { EmployeeCode = "NV004", FullName = "Phạm Minh Đức", DepartmentId = khoDept.Id, PositionId = khoPos.Id, Email = "duc.pham@tienphong.vn", Phone = "0988444555", Status = "Active", HireDate = new DateTime(2023, 8, 20) }
                };

                context.Employees.AddRange(emps);
                await context.SaveChangesAsync();

                foreach (var emp in emps)
                {
                    context.EmployeeDetails.Add(new EmployeeDetail
                    {
                        EmployeeId = emp.Id,
                        CurrentAddress = "Hà Nội",
                        BankAccountNumber = "190333" + emp.Id.ToString("D6"),
                        BankName = "Techcombank",
                        EmergencyContactName = "Người thân",
                        EmergencyContactPhone = "0909123456"
                    });

                    decimal bSal = emp.PositionId == ceoPos.Id ? 35000000m : (emp.PositionId == hrPos.Id ? 22000000m : 12000000m);
                    context.Contracts.Add(new Contract
                    {
                        EmployeeId = emp.Id,
                        ContractNumber = $"HDLD-{emp.EmployeeCode}",
                        ContractType = "Hợp đồng lao động không thời hạn",
                        StartDate = emp.HireDate,
                        BasicSalary = bSal,
                        InsuranceSalary = Math.Min(bSal, 20000000m),
                        Status = "Active",
                        SignDate = emp.HireDate
                    });
                }
                await context.SaveChangesAsync();
            }

            var allEmployees = await context.Employees
                .Include(e => e.Position)
                .Include(e => e.Contracts)
                .Where(e => e.Status == "Active")
                .ToListAsync();

            // 2. Nạp dữ liệu Chấm công mẫu cho Tháng 10/2026 (ngày 1 đến 15) để Heatmap hiển thị rực rỡ
            int curYear = 2026;
            int curMonth = 10;
            if (hcShift != null && !await context.AttendanceDailies.AnyAsync(a => a.WorkDate.Year == curYear && a.WorkDate.Month == curMonth))
            {
                var attList = new List<AttendanceDaily>();
                for (int day = 1; day <= 15; day++)
                {
                    var d = new DateTime(curYear, curMonth, day);
                    if (d.DayOfWeek == DayOfWeek.Sunday) continue;

                    foreach (var emp in allEmployees)
                    {
                        var att = new AttendanceDaily
                        {
                            EmployeeId = emp.Id,
                            ShiftId = hcShift.Id,
                            WorkDate = d,
                            FirstInTime = new TimeSpan(8, 0, 0),
                            LastOutTime = new TimeSpan(17, 30, 0),
                            WorkHourCount = 8.0m,
                            AttendanceStatus = "Present",
                            CreatedAt = d
                        };

                        // Tạo vài biến động thực tế
                        if (emp.EmployeeCode == "NV002" && day == 3)
                        {
                            att.FirstInTime = new TimeSpan(8, 25, 0);
                            att.LateMinutes = 25;
                            att.AttendanceStatus = "Late";
                        }
                        else if (emp.EmployeeCode == "NV001" && day == 6)
                        {
                            att.LastOutTime = new TimeSpan(19, 30, 0);
                            att.OvertimeHours = 2.0m;
                        }
                        else if (emp.EmployeeCode == "NV003" && (day == 8 || day == 9))
                        {
                            att.AttendanceStatus = "BusinessTrip";
                            att.Note = "Công tác xúc tiến thị trường miền Bắc";
                        }
                        else if (emp.EmployeeCode == "NV004" && day == 4)
                        {
                            att.AttendanceStatus = "Leave";
                            att.Note = "Nghỉ ốm có duyệt đơn";
                        }

                        attList.Add(att);
                    }
                }
                context.AttendanceDailies.AddRange(attList);
                await context.SaveChangesAsync();
            }

            // 3. Nạp danh sách Đơn từ mẫu (LeaveRequests)
            if (!await context.LeaveRequests.AnyAsync())
            {
                var emp1 = allEmployees.FirstOrDefault(e => e.EmployeeCode == "NV001") ?? allEmployees.First();
                var emp2 = allEmployees.FirstOrDefault(e => e.EmployeeCode == "NV002") ?? allEmployees.First();
                var emp3 = allEmployees.FirstOrDefault(e => e.EmployeeCode == "NV003") ?? allEmployees.First();
                var emp4 = allEmployees.FirstOrDefault(e => e.EmployeeCode == "NV004") ?? allEmployees.First();

                var leaves = new List<LeaveRequest>
                {
                    new LeaveRequest
                    {
                        RequestCode = "LR-2610-001",
                        EmployeeId = emp1.Id,
                        RequestType = "ANNUAL_LEAVE",
                        FromDate = new DateTime(2026, 10, 18),
                        ToDate = new DateTime(2026, 10, 19),
                        TotalDays = 2.0m,
                        Reason = "Nghỉ phép thường niên đi du lịch gia đình đã lên lịch trước.",
                        Status = LeaveRequestStatus.DirectorApproved,
                        DepartmentHeadId = adminUser.Id,
                        DepartmentHeadApprovedAt = VietnamTime.Now.AddDays(-2),
                        DepartmentHeadNote = "Nhất trí phân công nhân sự hỗ trợ thay thế.",
                        DirectorId = adminUser.Id,
                        DirectorApprovedAt = VietnamTime.Now.AddDays(-1),
                        DirectorNote = "Đã phê duyệt 2 ngày phép năm.",
                        CreatedAt = VietnamTime.Now.AddDays(-3)
                    },
                    new LeaveRequest
                    {
                        RequestCode = "LR-2610-002",
                        EmployeeId = emp2.Id,
                        RequestType = "SICK_LEAVE",
                        FromDate = new DateTime(2026, 10, 4),
                        ToDate = new DateTime(2026, 10, 4),
                        TotalDays = 1.0m,
                        Reason = "Nghỉ ốm sốt phát ban, đã có giấy khám bệnh của bệnh viện Thu Cúc.",
                        Status = LeaveRequestStatus.DepartmentApproved,
                        DepartmentHeadId = adminUser.Id,
                        DepartmentHeadApprovedAt = VietnamTime.Now.AddDays(-5),
                        DepartmentHeadNote = "Đã nhận đơn và toa thuốc đính kèm.",
                        CreatedAt = VietnamTime.Now.AddDays(-6)
                    },
                    new LeaveRequest
                    {
                        RequestCode = "LR-2610-003",
                        EmployeeId = emp3.Id,
                        RequestType = "BUSINESS_TRIP",
                        FromDate = new DateTime(2026, 10, 22),
                        ToDate = new DateTime(2026, 10, 24),
                        TotalDays = 3.0m,
                        Reason = "Đi công tác thẩm định mở showroom đại lý ủy quyền tại Hải Phòng và Quảng Ninh.",
                        Status = LeaveRequestStatus.PendingApproval,
                        CreatedAt = VietnamTime.Now.AddDays(-1)
                    },
                    new LeaveRequest
                    {
                        RequestCode = "LR-2610-004",
                        EmployeeId = emp4.Id,
                        RequestType = "LATE_EARLY",
                        FromDate = new DateTime(2026, 10, 10),
                        ToDate = new DateTime(2026, 10, 10),
                        TotalDays = 0.5m,
                        Reason = "Xin về sớm 2 tiếng giải quyết việc cá nhân tại ngân hàng.",
                        Status = LeaveRequestStatus.Rejected,
                        DepartmentHeadId = adminUser.Id,
                        DepartmentHeadApprovedAt = VietnamTime.Now.AddDays(-4),
                        DepartmentHeadNote = "Từ chối vì kho đang có lịch nhập lô hàng Dell lớn buổi chiều.",
                        CreatedAt = VietnamTime.Now.AddDays(-5)
                    }
                };

                context.LeaveRequests.AddRange(leaves);
                await context.SaveChangesAsync();
            }
        }
    }
}
