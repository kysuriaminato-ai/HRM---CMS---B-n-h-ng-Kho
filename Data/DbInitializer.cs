using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext context, string? initialAdminPassword = null)
        {
            // 1. Đảm bảo Database và toàn bộ cấu trúc bảng đã được tạo
            await context.Database.EnsureCreatedAsync();

            // Đảm bảo bảng AuditLogs và Notifications đã tồn tại trong SQL Server
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
                BEGIN
                    CREATE TABLE [AuditLogs] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] int NULL,
                        [Username] nvarchar(100) NULL,
                        [Action] nvarchar(50) NOT NULL,
                        [EntityName] nvarchar(100) NOT NULL,
                        [RecordId] nvarchar(100) NULL,
                        [OldValues] nvarchar(max) NULL,
                        [NewValues] nvarchar(max) NULL,
                        [AffectedColumns] nvarchar(max) NULL,
                        [IpAddress] nvarchar(50) NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE()
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
                BEGIN
                    CREATE TABLE [Notifications] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [ReceiverUserId] int NULL,
                        [TargetRole] nvarchar(50) NULL,
                        [Title] nvarchar(250) NOT NULL,
                        [Content] nvarchar(1000) NOT NULL,
                        [LinkUrl] nvarchar(500) NULL,
                        [Type] nvarchar(50) NOT NULL DEFAULT 'SYSTEM',
                        [IsRead] bit NOT NULL DEFAULT 0,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE()
                    );
                END;

                -- BẢNG PHÂN HỆ SCM & MUA HÀNG (PHIÊN 3)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
                BEGIN
                    CREATE TABLE [Suppliers] (
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Code] nvarchar(50) NOT NULL,
                        [Name] nvarchar(250) NOT NULL,
                        [TaxCode] nvarchar(50) NULL,
                        [Phone] nvarchar(20) NULL,
                        [Email] nvarchar(100) NULL,
                        [Address] nvarchar(300) NULL,
                        [TotalDebt] decimal(18,2) NOT NULL DEFAULT 0,
                        [IsActive] bit NOT NULL DEFAULT 1,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE()
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrders')
                BEGIN
                    CREATE TABLE [PurchaseOrders] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [POCode] nvarchar(50) NOT NULL,
                        [SupplierId] int NOT NULL FOREIGN KEY REFERENCES [Suppliers]([Id]),
                        [BranchId] int NOT NULL FOREIGN KEY REFERENCES [Branches]([Id]),
                        [OrderDate] datetime2 NOT NULL DEFAULT GETDATE(),
                        [ExpectedDeliveryDate] datetime2 NULL,
                        [ReceivedDate] datetime2 NULL,
                        [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [FinalAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [PaidAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [DebtAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [Status] nvarchar(50) NOT NULL DEFAULT 'PendingApproval',
                        [CreatedBy] int NOT NULL FOREIGN KEY REFERENCES [Users]([Id]),
                        [ApprovedBy] int NULL FOREIGN KEY REFERENCES [Users]([Id]),
                        [Note] nvarchar(500) NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE()
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrderDetails')
                BEGIN
                    CREATE TABLE [PurchaseOrderDetails] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [PurchaseOrderId] bigint NOT NULL FOREIGN KEY REFERENCES [PurchaseOrders]([Id]) ON DELETE CASCADE,
                        [ProductId] int NOT NULL FOREIGN KEY REFERENCES [Products]([Id]),
                        [Quantity] int NOT NULL DEFAULT 1,
                        [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0,
                        [TotalPrice] decimal(18,2) NOT NULL DEFAULT 0,
                        [Note] nvarchar(250) NULL
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DebtRecords')
                BEGIN
                    CREATE TABLE [DebtRecords] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [DebtType] nvarchar(20) NOT NULL,
                        [PartnerType] nvarchar(20) NOT NULL,
                        [PartnerName] nvarchar(200) NOT NULL,
                        [PartnerPhone] nvarchar(50) NULL,
                        [SupplierId] int NULL FOREIGN KEY REFERENCES [Suppliers]([Id]),
                        [OrderId] bigint NULL FOREIGN KEY REFERENCES [Orders]([Id]),
                        [PurchaseOrderId] bigint NULL FOREIGN KEY REFERENCES [PurchaseOrders]([Id]),
                        [DocumentCode] nvarchar(50) NOT NULL,
                        [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [PaidAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [RemainingAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [DueDate] datetime2 NOT NULL,
                        [Status] nvarchar(30) NOT NULL DEFAULT 'Unpaid',
                        [Note] nvarchar(500) NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE()
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DebtPayments')
                BEGIN
                    CREATE TABLE [DebtPayments] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [DebtRecordId] bigint NOT NULL FOREIGN KEY REFERENCES [DebtRecords]([Id]) ON DELETE CASCADE,
                        [PaymentCode] nvarchar(50) NOT NULL,
                        [Amount] decimal(18,2) NOT NULL DEFAULT 0,
                        [PaymentMethod] nvarchar(50) NOT NULL DEFAULT 'BANK_TRANSFER',
                        [PaymentDate] datetime2 NOT NULL DEFAULT GETDATE(),
                        [Note] nvarchar(500) NULL,
                        [CreatedBy] int NOT NULL FOREIGN KEY REFERENCES [Users]([Id])
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StocktakingSheets')
                BEGIN
                    CREATE TABLE [StocktakingSheets] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Code] nvarchar(50) NOT NULL,
                        [BranchId] int NOT NULL FOREIGN KEY REFERENCES [Branches]([Id]),
                        [CheckDate] datetime2 NOT NULL DEFAULT GETDATE(),
                        [TotalSystemQty] int NOT NULL DEFAULT 0,
                        [TotalActualQty] int NOT NULL DEFAULT 0,
                        [TotalDifferenceQty] int NOT NULL DEFAULT 0,
                        [TotalDifferenceValue] decimal(18,2) NOT NULL DEFAULT 0,
                        [Status] nvarchar(30) NOT NULL DEFAULT 'Draft',
                        [CreatedBy] int NOT NULL FOREIGN KEY REFERENCES [Users]([Id]),
                        [Note] nvarchar(500) NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE()
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StocktakingDetails')
                BEGIN
                    CREATE TABLE [StocktakingDetails] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [StocktakingSheetId] bigint NOT NULL FOREIGN KEY REFERENCES [StocktakingSheets]([Id]) ON DELETE CASCADE,
                        [ProductId] int NOT NULL FOREIGN KEY REFERENCES [Products]([Id]),
                        [SystemQuantity] int NOT NULL DEFAULT 0,
                        [ActualQuantity] int NOT NULL DEFAULT 0,
                        [DifferenceQuantity] int NOT NULL DEFAULT 0,
                        [CostPrice] decimal(18,2) NOT NULL DEFAULT 0,
                        [DifferenceValue] decimal(18,2) NOT NULL DEFAULT 0,
                        [Reason] nvarchar(250) NULL
                    );
                END;

                -- BẢNG PHÂN HỆ HRM NÂNG CAO & TIỀN LƯƠNG (PHIÊN 4)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LeaveRequests')
                BEGIN
                    CREATE TABLE [LeaveRequests] (
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [RequestCode] nvarchar(50) NOT NULL,
                        [EmployeeId] int NOT NULL FOREIGN KEY REFERENCES [Employees]([Id]),
                        [RequestType] nvarchar(50) NOT NULL DEFAULT 'ANNUAL_LEAVE',
                        [FromDate] datetime2 NOT NULL,
                        [ToDate] datetime2 NOT NULL,
                        [TotalDays] decimal(5,2) NOT NULL DEFAULT 1.0,
                        [Reason] nvarchar(500) NOT NULL,
                        [AttachmentUrl] nvarchar(500) NULL,
                        [Status] nvarchar(50) NOT NULL DEFAULT 'CHO_DUYET',
                        [DepartmentHeadId] int NULL FOREIGN KEY REFERENCES [Users]([Id]),
                        [DepartmentHeadApprovedAt] datetime2 NULL,
                        [DepartmentHeadNote] nvarchar(500) NULL,
                        [DirectorId] int NULL FOREIGN KEY REFERENCES [Users]([Id]),
                        [DirectorApprovedAt] datetime2 NULL,
                        [DirectorNote] nvarchar(500) NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE()
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PayrollPeriods')
                BEGIN
                    CREATE TABLE [PayrollPeriods] (
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Name] nvarchar(150) NOT NULL,
                        [Month] int NOT NULL,
                        [Year] int NOT NULL,
                        [StandardWorkDays] int NOT NULL DEFAULT 22,
                        [Status] nvarchar(50) NOT NULL DEFAULT 'DRAFT',
                        [TotalGrossSalary] decimal(18,2) NOT NULL DEFAULT 0,
                        [TotalNetSalary] decimal(18,2) NOT NULL DEFAULT 0,
                        [TotalInsurance] decimal(18,2) NOT NULL DEFAULT 0,
                        [TotalPIT] decimal(18,2) NOT NULL DEFAULT 0,
                        [CreatedBy] int NULL FOREIGN KEY REFERENCES [Users]([Id]),
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
                        [LockedAt] datetime2 NULL
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalarySlips')
                BEGIN
                    CREATE TABLE [SalarySlips] (
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [PayrollPeriodId] int NOT NULL FOREIGN KEY REFERENCES [PayrollPeriods]([Id]) ON DELETE CASCADE,
                        [EmployeeId] int NOT NULL FOREIGN KEY REFERENCES [Employees]([Id]),
                        [BasicSalary] decimal(18,2) NOT NULL DEFAULT 0,
                        [InsuranceSalary] decimal(18,2) NOT NULL DEFAULT 0,
                        [StandardDays] int NOT NULL DEFAULT 22,
                        [ActualWorkDays] decimal(5,2) NOT NULL DEFAULT 0,
                        [PaidLeaveDays] decimal(5,2) NOT NULL DEFAULT 0,
                        [UnpaidLeaveDays] decimal(5,2) NOT NULL DEFAULT 0,
                        [OvertimeHours] decimal(5,2) NOT NULL DEFAULT 0,
                        [StandardWorkSalary] decimal(18,2) NOT NULL DEFAULT 0,
                        [OvertimeSalary] decimal(18,2) NOT NULL DEFAULT 0,
                        [PositionAllowance] decimal(18,2) NOT NULL DEFAULT 0,
                        [LunchAllowance] decimal(18,2) NOT NULL DEFAULT 0,
                        [OtherAllowance] decimal(18,2) NOT NULL DEFAULT 0,
                        [Bonus] decimal(18,2) NOT NULL DEFAULT 0,
                        [GrossSalary] decimal(18,2) NOT NULL DEFAULT 0,
                        [LatePenalty] decimal(18,2) NOT NULL DEFAULT 0,
                        [SocialInsurance] decimal(18,2) NOT NULL DEFAULT 0,
                        [HealthInsurance] decimal(18,2) NOT NULL DEFAULT 0,
                        [UnemploymentInsurance] decimal(18,2) NOT NULL DEFAULT 0,
                        [TotalInsurance] decimal(18,2) NOT NULL DEFAULT 0,
                        [TaxableIncome] decimal(18,2) NOT NULL DEFAULT 0,
                        [PersonalIncomeTax] decimal(18,2) NOT NULL DEFAULT 0,
                        [NetSalary] decimal(18,2) NOT NULL DEFAULT 0,
                        [Status] nvarchar(50) NOT NULL DEFAULT 'DRAFT',
                        [Note] nvarchar(500) NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE()
                    );
                END;
            ");

            // Bảng trạng thái đã đọc của thông báo chung (mỗi người đọc riêng) cho database đã tồn tại
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationReads')
                BEGIN
                    CREATE TABLE [NotificationReads] (
                        [NotificationId] bigint NOT NULL,
                        [UserId] int NOT NULL,
                        [ReadAt] datetime2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT [PK_NotificationReads] PRIMARY KEY ([NotificationId], [UserId]),
                        CONSTRAINT [FK_NotificationReads_Notifications] FOREIGN KEY ([NotificationId])
                            REFERENCES [Notifications]([Id]) ON DELETE CASCADE
                    );
                END;
            ");

            // 2. KHỞI TẠO PHÂN HỆ QUẢN TRỊ & PHÂN QUYỀN (Auth & Permissions)
            await SeedAuthAndPermissionsAsync(context);

            // 3. KHỞI TẠO PHÂN HỆ HRM (Công ty, Chi nhánh, Phòng ban, Chức vụ, Ca làm việc, Nhân viên, Hợp đồng, Chi tiết)
            await SeedHrmDataAsync(context);

            // 4. KHỞI TẠO TÀI KHOẢN NGƯỜI DÙNG & GÁN VAI TRÒ
            await SeedUsersAndRolesAsync(context, initialAdminPassword);

            // 5. KHỞI TẠO PHÂN HỆ CMS (SiteSettings, Banners, Menus, Contents)
            await SeedCmsDataAsync(context);

            // 6. KHỞI TẠO PHÂN HỆ BÁN HÀNG & KHO (Danh mục, Sản phẩm, Tồn kho ban đầu)
            await SeedSalesAndInventoryDataAsync(context);

            // 7. KHỞI TẠO PHÂN HỆ SCM, MUA HÀNG, CÔNG NỢ & KIỂM KÊ (Phiên 3)
            await SeedScmDataAsync(context);

            // 8. KHỞI TẠO PHÂN HỆ HRM NÂNG CAO & TIỀN LƯƠNG (Phiên 4)
            await SeedHrmAdvancedDataAsync(context);
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
                ("auth", "auth.user.manage", "Quản lý tài khoản: duyệt, tạo, khóa, gán vai trò, đặt lại mật khẩu"),

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
                new Role { Code = "WAREHOUSE_STAFF", Name = "Thủ kho Chuyên trách", Description = "Quản lý tồn kho và nhập xuất kho", IsSystem = false },
                new Role { Code = "ADMIN_MENU", Name = "Quản trị Menu website", Description = "Quản lý menu điều hướng website", IsSystem = false },
                new Role { Code = "ADMIN_CONTENT", Name = "Quản trị Nội dung", Description = "Quản lý bài viết, banner và giao diện", IsSystem = false },
                // Các admin nhóm: mỗi nhóm chỉ giữ đúng nhóm quyền của mình
                new Role { Code = "ADMIN_SANPHAM", Name = "Quản lý sản phẩm", Description = "Quản lý danh mục và sản phẩm", IsSystem = false },
                new Role { Code = "ADMIN_DONHANG", Name = "Quản lý đơn hàng", Description = "Quản lý đơn hàng, bán hàng và báo cáo bán hàng", IsSystem = false },
                new Role { Code = "ADMIN_NHANVIEN", Name = "Quản lý nhân viên", Description = "Quản lý hồ sơ, chấm công và đơn từ nhân viên", IsSystem = false },
                new Role { Code = "ADMIN_BANGLUONG", Name = "Quản lý lương", Description = "Tính lương, bảng lương và phiếu lương", IsSystem = false },
                new Role { Code = "ADMIN_KHO", Name = "Quản lý kho & mua hàng", Description = "Tồn kho, nhập xuất, đơn mua hàng và nhà cung cấp", IsSystem = false }
            };

            foreach (var r in roles)
            {
                if (!await context.Roles.AnyAsync(x => x.Code.ToUpper() == r.Code.ToUpper()))
                {
                    context.Roles.Add(r);
                }
            }
            await context.SaveChangesAsync();

            // Gán quyền cho các Roles
            var allPerms = await context.Permissions.ToListAsync();
            var allRoles = await context.Roles.ToListAsync();

            async Task AssignPermissionsToRole(string roleCode, Func<Permission, bool> predicate)
            {
                var role = allRoles.FirstOrDefault(r => string.Equals(r.Code, roleCode, StringComparison.OrdinalIgnoreCase));
                if (role == null) return;
                var targetPerms = allPerms.Where(predicate).ToList();
                foreach (var p in targetPerms)
                {
                    if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == p.Id))
                    {
                        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
                    }
                }
            }

            // SUPERADMIN có toàn bộ quyền
            await AssignPermissionsToRole("SUPERADMIN", _ => true);

            // HR_MANAGER có các quyền nhân sự và chấm công
            await AssignPermissionsToRole("HR_MANAGER", p => p.Code.StartsWith("hrm."));

            // SALES_STAFF có quyền bán hàng, xem sản phẩm, đơn hàng
            await AssignPermissionsToRole("SALES_STAFF", p => p.Code == "order.pos" || p.Code == "order.view" || p.Code == "product.view");

            // WAREHOUSE_STAFF có quyền xem và nhập xuất kho
            await AssignPermissionsToRole("WAREHOUSE_STAFF", p => p.Code.StartsWith("inventory.") || p.Code == "product.view");
            await AssignPermissionsToRole("ADMIN_MENU", p => p.Code.StartsWith("menu.") || p.Code == "product.view");
            await AssignPermissionsToRole("ADMIN_CONTENT", p => p.Code.StartsWith("content.") || p.Code == "layout.manage");

            // [OOP - ĐÓNG GÓI QUYỀN] Mỗi admin nhóm chỉ nhận đúng nhóm mã quyền theo tiền tố
            await AssignPermissionsToRole("ADMIN_SANPHAM", p => p.Code.StartsWith("product.") || p.Code == "inventory.view");
            await AssignPermissionsToRole("ADMIN_DONHANG", p => p.Code.StartsWith("order.") || p.Code == "debt.view" || p.Code == "report.sales");
            await AssignPermissionsToRole("ADMIN_NHANVIEN", p => p.Code.StartsWith("hrm.employee.") || p.Code.StartsWith("hrm.attendance.") || p.Code.StartsWith("hrm.leave."));
            await AssignPermissionsToRole("ADMIN_BANGLUONG", p => p.Code.StartsWith("hrm.payroll.") || p.Code == "hrm.employee.view");
            await AssignPermissionsToRole("ADMIN_KHO", p => p.Code.StartsWith("inventory.") || p.Code.StartsWith("scm."));

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Tạo tài khoản "quantritong" (IsSuperAdmin = true, vai trò SUPERADMIN) nếu chưa có.
        /// Không ghi đè mật khẩu nếu tài khoản đã tồn tại.
        /// </summary>
        private static async Task EnsureQuanTriTongAsync(AppDbContext context, string? initialPassword)
        {
            const string username = "quantritong";
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                bool coCauHinh = !string.IsNullOrWhiteSpace(initialPassword);
                string matKhau = coCauHinh
                    ? initialPassword!
                    : Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));
                string salt = MatKhauHelper.GenerateSalt();
                user = new User
                {
                    Username = username,
                    PasswordSalt = salt,
                    PasswordHash = MatKhauHelper.HashPassword(matKhau, salt),
                    IsActive = true,
                    IsSuperAdmin = true,
                    EmployeeId = null,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                if (!coCauHinh)
                {
                    Console.WriteLine($"[SEED] Đã tạo tài khoản quản trị tổng '{username}' với mật khẩu ngẫu nhiên: {matKhau} . Hãy đặt SeedAdmin:Password và đổi mật khẩu ngay.");
                }
            }

            var superRole = await context.Roles.FirstOrDefaultAsync(r => r.Code.ToUpper() == "SUPERADMIN");
            if (superRole != null && !await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == superRole.Id))
            {
                context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = superRole.Id });
                await context.SaveChangesAsync();
            }
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

        private static async Task SeedUsersAndRolesAsync(AppDbContext context, string? initialAdminPassword)
        {
            var rolesList = await context.Roles.ToListAsync();
            int? GetRoleId(string code) => rolesList.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase))?.Id;

            // 1. Tạo tài khoản SuperAdmin "admin" CHỈ KHI chưa tồn tại (không ghi đè mật khẩu của tài khoản đã có).
            //    Mật khẩu lấy từ cấu hình SeedAdmin:Password; nếu để trống sẽ sinh mật khẩu ngẫu nhiên và in ra console một lần.
            if (!await context.Users.AnyAsync(u => u.Username == "admin"))
            {
                bool coCauHinh = !string.IsNullOrWhiteSpace(initialAdminPassword);
                string matKhau = coCauHinh
                    ? initialAdminPassword!
                    : Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));
                string salt = MatKhauHelper.GenerateSalt();

                context.Users.Add(new User
                {
                    Username = "admin",
                    PasswordSalt = salt,
                    PasswordHash = MatKhauHelper.HashPassword(matKhau, salt),
                    IsActive = true,
                    IsSuperAdmin = true,
                    EmployeeId = null,
                    CreatedAt = DateTime.Now
                });
                await context.SaveChangesAsync();

                if (!coCauHinh)
                {
                    Console.WriteLine($"[SEED] Đã tạo tài khoản admin với mật khẩu ngẫu nhiên: {matKhau} . Hãy đặt SeedAdmin:Password trong appsettings và đổi mật khẩu ngay.");
                }
            }

            // 1b. [QUẢN TRỊ TỔNG] Tài khoản quản trị tổng thứ hai, có toàn quyền và dùng để tạo các admin nhóm.
            await EnsureQuanTriTongAsync(context, initialAdminPassword);

            // 2. Đảm bảo admin có vai trò SUPERADMIN
            var adminUser = await context.Users.FirstAsync(u => u.Username == "admin");
            int? superRoleId = GetRoleId("SUPERADMIN");
            if (superRoleId.HasValue && !await context.UserRoles.AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == superRoleId.Value))
            {
                context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = superRoleId.Value });
                await context.SaveChangesAsync();
            }

            // 3. Gán vai trò cho các tài khoản demo đã có (KHÔNG đổi mật khẩu của họ)
            var demoRoles = new Dictionary<string, string>
            {
                ["superadmin"] = "SUPERADMIN",
                ["qtvp"] = "HR_MANAGER",
                ["hr_manager"] = "HR_MANAGER",
                ["salestaff"] = "SALES_STAFF",
                ["sales_staff"] = "SALES_STAFF",
                ["warehouse"] = "WAREHOUSE_STAFF",
                ["warehouse_staff"] = "WAREHOUSE_STAFF"
            };

            foreach (var (username, roleCode) in demoRoles)
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
                int? roleId = GetRoleId(roleCode);
                if (user == null || !roleId.HasValue) continue;

                if (!await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == roleId.Value))
                {
                    context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId.Value });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedCmsDataAsync(AppDbContext context)
        {
            // 1. SiteSettings
            var settings = new List<SiteSetting>
            {
                new SiteSetting { SettingKey = "Site_Name", SettingValue = "TIEN PHONG ENTERPRISE", SettingGroup = "General", Description = "Tên hiển thị hệ thống" },
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
                        CreatedAt = DateTime.Now
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
                        CreatedAt = DateTime.Now
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
                            UpdatedAt = DateTime.Now
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
                    OrderDate = DateTime.Now.AddDays(-5),
                    ExpectedDeliveryDate = DateTime.Now.AddDays(-2),
                    ReceivedDate = DateTime.Now.AddDays(-2),
                    TotalAmount = 82000000,
                    DiscountAmount = 0,
                    FinalAmount = 82000000,
                    PaidAmount = 30000000,
                    DebtAmount = 52000000,
                    Status = "Received",
                    CreatedBy = adminUser.Id,
                    ApprovedBy = adminUser.Id,
                    Note = "Nhập lô máy chủ HP ProLiant cho dự án ngân hàng",
                    CreatedAt = DateTime.Now.AddDays(-5)
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
                    OrderDate = DateTime.Now.AddDays(-1),
                    ExpectedDeliveryDate = DateTime.Now.AddDays(2),
                    TotalAmount = 195000000,
                    DiscountAmount = 0,
                    FinalAmount = 195000000,
                    PaidAmount = 45000000,
                    DebtAmount = 150000000,
                    Status = "Approved",
                    CreatedBy = adminUser.Id,
                    ApprovedBy = adminUser.Id,
                    Note = "Nhập bổ sung 10 laptop Dell Latitude cho chi nhánh Hà Nội",
                    CreatedAt = DateTime.Now.AddDays(-1)
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
                    OrderDate = DateTime.Now,
                    ExpectedDeliveryDate = DateTime.Now.AddDays(5),
                    TotalAmount = 45000000,
                    DiscountAmount = 0,
                    FinalAmount = 45000000,
                    PaidAmount = 0,
                    DebtAmount = 45000000,
                    Status = "PendingApproval",
                    CreatedBy = adminUser.Id,
                    Note = "Đề xuất nhập bổ sung thiết bị mạng dự phòng",
                    CreatedAt = DateTime.Now
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
                var poReceived = await context.PurchaseOrders.FirstOrDefaultAsync(p => p.Status == "Received");
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
                        DueDate = DateTime.Now.AddDays(10),
                        Status = "PartiallyPaid",
                        Note = "Thanh toán đợt 1 30tr, đợt 2 theo điều khoản tín dụng 15 ngày",
                        CreatedAt = DateTime.Now.AddDays(-5)
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
                        DueDate = DateTime.Now.AddDays(25),
                        Status = "PartiallyPaid",
                        Note = "Đặt cọc trước 45tr, tất toán khi nhận đủ hóa đơn GTGT",
                        CreatedAt = DateTime.Now.AddDays(-1)
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
                        DueDate = DateTime.Now.AddDays(7),
                        Status = "PartiallyPaid",
                        Note = "Hợp đồng cung cấp máy chủ & thiết bị ngoại vi, trả chậm 15 ngày",
                        CreatedAt = DateTime.Now.AddDays(-3)
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
                        DueDate = DateTime.Now.AddDays(-12),
                        Status = "Overdue",
                        Note = "Đơn hàng mua sắm quý 3/2026 đã quá hạn 12 ngày, đã nhắc nợ lần 2",
                        CreatedAt = DateTime.Now.AddDays(-27)
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
                        PaymentDate = DateTime.Now.AddDays(-4),
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
                    CheckDate = DateTime.Now.AddDays(-3),
                    TotalSystemQty = 120,
                    TotalActualQty = 119,
                    TotalDifferenceQty = -1,
                    TotalDifferenceValue = -1850000,
                    Status = "Balanced",
                    CreatedBy = adminUser.Id,
                    Note = "Kiểm kê định kỳ đầu tháng 10/2026. Lệch 1 Chuột Logitech do rơi vỡ bảo hành.",
                    CreatedAt = DateTime.Now.AddDays(-3)
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
                        Status = "GIAM_DOC_DUYET",
                        DepartmentHeadId = adminUser.Id,
                        DepartmentHeadApprovedAt = DateTime.Now.AddDays(-2),
                        DepartmentHeadNote = "Nhất trí phân công nhân sự hỗ trợ thay thế.",
                        DirectorId = adminUser.Id,
                        DirectorApprovedAt = DateTime.Now.AddDays(-1),
                        DirectorNote = "Đã phê duyệt 2 ngày phép năm.",
                        CreatedAt = DateTime.Now.AddDays(-3)
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
                        Status = "TRUONG_PHONG_DUYET",
                        DepartmentHeadId = adminUser.Id,
                        DepartmentHeadApprovedAt = DateTime.Now.AddDays(-5),
                        DepartmentHeadNote = "Đã nhận đơn và toa thuốc đính kèm.",
                        CreatedAt = DateTime.Now.AddDays(-6)
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
                        Status = "CHO_DUYET",
                        CreatedAt = DateTime.Now.AddDays(-1)
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
                        Status = "TU_CHOI",
                        DepartmentHeadId = adminUser.Id,
                        DepartmentHeadApprovedAt = DateTime.Now.AddDays(-4),
                        DepartmentHeadNote = "Từ chối vì kho đang có lịch nhập lô hàng Dell lớn buổi chiều.",
                        CreatedAt = DateTime.Now.AddDays(-5)
                    }
                };

                context.LeaveRequests.AddRange(leaves);
                await context.SaveChangesAsync();
            }

            // 4. Khởi tạo Bảng lương Tháng 10/2026 & Phiếu lương chi tiết (PayrollPeriod & SalarySlips)
            if (!await context.PayrollPeriods.AnyAsync(p => p.Month == curMonth && p.Year == curYear))
            {
                var period = new PayrollPeriod
                {
                    Name = $"Bảng lương Kỳ Tháng {curMonth:D2}/{curYear}",
                    Month = curMonth,
                    Year = curYear,
                    StandardWorkDays = 22,
                    Status = "CALCULATED",
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.Now
                };

                decimal totalGross = 0;
                decimal totalNet = 0;
                decimal totalIns = 0;
                decimal totalPit = 0;

                foreach (var emp in allEmployees)
                {
                    var contract = emp.Contracts.FirstOrDefault(c => c.Status == "Active");
                    decimal baseSal = contract?.BasicSalary ?? (emp.Position?.BaseSalary > 0 ? emp.Position.BaseSalary : 12000000m);
                    decimal insSal = contract?.InsuranceSalary ?? Math.Min(baseSal, 18000000m);

                    decimal actDays = 21.0m;
                    decimal paidLeaves = (emp.EmployeeCode == "NV001") ? 1.0m : 0m;
                    decimal otHours = (emp.EmployeeCode == "NV001") ? 2.0m : 0m;

                    decimal dailyRate = baseSal / 22m;
                    decimal hourlyRate = dailyRate / 8m;
                    decimal stdSal = Math.Round(dailyRate * (actDays + paidLeaves), 0);
                    decimal otSal = Math.Round(hourlyRate * 1.5m * otHours, 0);

                    decimal posAllowance = Math.Round(baseSal * (emp.Position?.AllowanceRate ?? 0), 0);
                    decimal lunch = 730000m;
                    decimal other = 500000m;
                    decimal bonus = (emp.Position?.Code == "NV-KD") ? 2500000m : 1000000m;

                    decimal gross = stdSal + otSal + posAllowance + lunch + other + bonus;

                    decimal latePen = (emp.EmployeeCode == "NV002") ? 100000m : 0m;
                    decimal bhxh = Math.Round(insSal * 0.08m, 0);
                    decimal bhyt = Math.Round(insSal * 0.015m, 0);
                    decimal bhtn = Math.Round(insSal * 0.01m, 0);
                    decimal empIns = bhxh + bhyt + bhtn;

                    decimal nonTax = lunch + other + empIns + 11000000m;
                    decimal taxBase = Math.Max(0, gross - nonTax);
                    decimal pit = 0;
                    if (taxBase > 0)
                    {
                        if (taxBase <= 5000000m) pit = taxBase * 0.05m;
                        else if (taxBase <= 10000000m) pit = (taxBase * 0.10m) - 250000m;
                        else pit = (taxBase * 0.15m) - 750000m;
                    }

                    decimal net = gross - empIns - pit - latePen;

                    totalGross += gross;
                    totalNet += net;
                    totalIns += empIns;
                    totalPit += pit;

                    period.SalarySlips.Add(new SalarySlip
                    {
                        EmployeeId = emp.Id,
                        BasicSalary = baseSal,
                        InsuranceSalary = insSal,
                        StandardDays = 22,
                        ActualWorkDays = actDays,
                        PaidLeaveDays = paidLeaves,
                        OvertimeHours = otHours,
                        StandardWorkSalary = stdSal,
                        OvertimeSalary = otSal,
                        PositionAllowance = posAllowance,
                        LunchAllowance = lunch,
                        OtherAllowance = other,
                        Bonus = bonus,
                        GrossSalary = gross,
                        LatePenalty = latePen,
                        SocialInsurance = bhxh,
                        HealthInsurance = bhyt,
                        UnemploymentInsurance = bhtn,
                        TotalInsurance = empIns,
                        TaxableIncome = taxBase,
                        PersonalIncomeTax = pit,
                        NetSalary = net,
                        Status = "SENT",
                        Note = "Đã tính lương tự động & gửi phiếu lương tới nhân sự."
                    });
                }

                period.TotalGrossSalary = totalGross;
                period.TotalNetSalary = totalNet;
                period.TotalInsurance = totalIns;
                period.TotalPIT = totalPit;

                context.PayrollPeriods.Add(period);
                await context.SaveChangesAsync();
            }
        }
    }
}
