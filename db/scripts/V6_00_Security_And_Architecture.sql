-- ==========================================================================================
-- PHIÊN 1: TÁI CẤU TRÚC CLEAN ARCHITECTURE, DATA SCOPE & KHẮC PHỤC BẢO MẬT P0
-- Script: db/scripts/V6_00_Security_And_Architecture.sql
-- Ngày: 2026-11-05
-- Mô tả:
--   1. Bổ sung cột Users.MustChangePassword (bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên)
--   2. Bổ sung cột LeaveRequests.CreatedByUserId + FK + Index (chặn tự duyệt đơn nghỉ phép)
--      và backfill dữ liệu lịch sử từ Users.EmployeeId
--   3. Đồng bộ idempotent các cột/bảng còn thiếu từ Phiên 4/5 (nếu DB chạy từ InitialBaseline)
--   4. Khởi tạo idempotent vai trò BRANCH_MANAGER & DEPARTMENT_HEAD phục vụ DataScope
--   5. Ghi nhận lịch sử EF Migration (20261104175015_V6_00_Security_And_Architecture)
-- Lưu ý: Script hoàn toàn IDEMPOTENT - chạy an toàn nhiều lần trực tiếp trên SSMS.
-- ==========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=== [V6_00] Bắt đầu nâng cấp Bảo mật P0 & Kiến trúc Clean Architecture ===';
GO

-- ==========================================================================================
-- PHẦN 1: CỘT BẢO MẬT TRÊN BẢNG USERS (MustChangePassword)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Users', N'MustChangePassword') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Users]
            ADD [MustChangePassword] BIT NOT NULL
            CONSTRAINT [DF_Users_MustChangePassword] DEFAULT (0);
        PRINT N'[OK] Đã thêm cột Users.MustChangePassword (DEFAULT 0).';
    END
    ELSE
    BEGIN
        PRINT N'[SKIP] Cột Users.MustChangePassword đã tồn tại.';
    END
END
GO

-- ==========================================================================================
-- PHẦN 2: CỘT NGƯỜI TẠO ĐƠN TRÊN BẢNG LEAVEREQUESTS (CreatedByUserId - Chặn tự duyệt đơn)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.LeaveRequests', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.LeaveRequests', N'CreatedByUserId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[LeaveRequests]
            ADD [CreatedByUserId] INT NULL;
        PRINT N'[OK] Đã thêm cột LeaveRequests.CreatedByUserId.';
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'FK_LeaveRequests_Users_CreatedByUserId'
          AND parent_object_id = OBJECT_ID(N'dbo.LeaveRequests')
    )
    BEGIN
        ALTER TABLE [dbo].[LeaveRequests] WITH CHECK
            ADD CONSTRAINT [FK_LeaveRequests_Users_CreatedByUserId]
            FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users]([Id])
            ON DELETE NO ACTION;
        PRINT N'[OK] Đã tạo khóa ngoại FK_LeaveRequests_Users_CreatedByUserId.';
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_LeaveRequests_CreatedByUserId'
          AND object_id = OBJECT_ID(N'dbo.LeaveRequests')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_LeaveRequests_CreatedByUserId]
            ON [dbo].[LeaveRequests] ([CreatedByUserId] ASC);
        PRINT N'[OK] Đã tạo chỉ mục IX_LeaveRequests_CreatedByUserId.';
    END
END
GO

-- Backfill CreatedByUserId cho các đơn từ cũ dựa trên liên kết Users.EmployeeId
IF COL_LENGTH(N'dbo.LeaveRequests', N'CreatedByUserId') IS NOT NULL
   AND OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
        UPDATE lr
        SET lr.CreatedByUserId = u.Id
        FROM [dbo].[LeaveRequests] lr
        INNER JOIN [dbo].[Users] u ON u.EmployeeId = lr.EmployeeId
        WHERE lr.CreatedByUserId IS NULL;
    ';
    PRINT N'[OK] Đã đồng bộ CreatedByUserId cho các đơn từ hiện có.';
END
GO

-- ==========================================================================================
-- PHẦN 3: ĐỒNG BỘ IDEMPOTENT CÁC BẢNG/CỘT MỞ RỘNG (NẾU DB CHƯA CHẠY V4_01 / V5_01)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Customers] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Customers] PRIMARY KEY,
        [Code] NVARCHAR(50) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(20) NULL,
        [Email] NVARCHAR(150) NULL,
        [Address] NVARCHAR(500) NULL,
        [TaxCode] NVARCHAR(50) NULL,
        [CustomerType] NVARCHAR(50) NOT NULL DEFAULT N'Retail',
        [TotalDebt] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalRevenue] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Note] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [CreatedBy] NVARCHAR(MAX) NULL
    );
    CREATE UNIQUE INDEX [IX_Customers_Code] ON [dbo].[Customers]([Code]);
    CREATE UNIQUE INDEX [IX_Customers_Phone] ON [dbo].[Customers]([Phone]) WHERE [Phone] IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.Units', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Units] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Units] PRIMARY KEY,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [CreatedBy] NVARCHAR(MAX) NULL
    );
    CREATE UNIQUE INDEX [IX_Units_Code] ON [dbo].[Units]([Code]);
END
GO

IF OBJECT_ID(N'dbo.Warehouses', N'U') IS NULL AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    CREATE TABLE [dbo].[Warehouses] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Warehouses] PRIMARY KEY,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Address] NVARCHAR(500) NULL,
        [Phone] NVARCHAR(20) NULL,
        [ManagerId] INT NULL,
        [BranchId] INT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_Warehouses_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Warehouses_Employees_ManagerId] FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [dbo].[Warehouses]([Code]);
    CREATE INDEX [IX_Warehouses_BranchId] ON [dbo].[Warehouses]([BranchId]);
    CREATE INDEX [IX_Warehouses_ManagerId] ON [dbo].[Warehouses]([ManagerId]);
END
GO

IF OBJECT_ID(N'dbo.LeaveTypes', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LeaveTypes] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LeaveTypes] PRIMARY KEY,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [MaxDaysPerYear] INT NOT NULL DEFAULT 12,
        [IsPaid] BIT NOT NULL DEFAULT 1,
        [AllowHalfDay] BIT NOT NULL DEFAULT 1,
        [RequiresAttachment] BIT NOT NULL DEFAULT 0,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [CreatedBy] NVARCHAR(MAX) NULL
    );
    CREATE UNIQUE INDEX [IX_LeaveTypes_Code] ON [dbo].[LeaveTypes]([Code]);
END
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Products', N'UnitId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [UnitId] INT NULL;
    ALTER TABLE [dbo].[Products] ADD CONSTRAINT [FK_Products_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]);
    CREATE INDEX [IX_Products_UnitId] ON [dbo].[Products]([UnitId]);
END
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Orders', N'CustomerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders] ADD [CustomerId] INT NULL;
    ALTER TABLE [dbo].[Orders] ADD CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id]);
    CREATE INDEX [IX_Orders_CustomerId] ON [dbo].[Orders]([CustomerId]);
END
GO

IF OBJECT_ID(N'dbo.LeaveRequests', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.LeaveRequests', N'LeaveTypeId') IS NULL
BEGIN
    ALTER TABLE [dbo].[LeaveRequests] ADD [LeaveTypeId] INT NULL;
    ALTER TABLE [dbo].[LeaveRequests] ADD CONSTRAINT [FK_LeaveRequests_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [dbo].[LeaveTypes]([Id]);
    CREATE INDEX [IX_LeaveRequests_LeaveTypeId] ON [dbo].[LeaveRequests]([LeaveTypeId]);
END
GO

IF OBJECT_ID(N'dbo.Inventories', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Inventories', N'WarehouseId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Inventories] ADD [WarehouseId] INT NULL;
    ALTER TABLE [dbo].[Inventories] ADD CONSTRAINT [FK_Inventories_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses]([Id]);
    CREATE INDEX [IX_Inventories_WarehouseId] ON [dbo].[Inventories]([WarehouseId]);
END
GO

IF OBJECT_ID(N'dbo.DebtRecords', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.DebtRecords', N'CustomerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[DebtRecords] ADD [CustomerId] INT NULL;
    ALTER TABLE [dbo].[DebtRecords] ADD CONSTRAINT [FK_DebtRecords_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id]) ON DELETE NO ACTION;
    CREATE INDEX [IX_DebtRecords_CustomerId] ON [dbo].[DebtRecords]([CustomerId]);
END
GO

-- ==========================================================================================
-- PHẦN 4: VAI TRÒ PHẠM VI DỮ LIỆU (BRANCH_MANAGER & DEPARTMENT_HEAD)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Code] = N'BRANCH_MANAGER')
    BEGIN
        INSERT INTO [dbo].[Roles] ([Code], [Name], [Description], [IsSystem], [IsActive], [CreatedAt])
        VALUES (N'BRANCH_MANAGER', N'Quản lý Chi nhánh', N'Quản lý vận hành, nhân sự, bán hàng và kho trong phạm vi chi nhánh', 0, 1, GETUTCDATE());
        PRINT N'[OK] Đã thêm vai trò BRANCH_MANAGER.';
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Code] = N'DEPARTMENT_HEAD')
    BEGIN
        INSERT INTO [dbo].[Roles] ([Code], [Name], [Description], [IsSystem], [IsActive], [CreatedAt])
        VALUES (N'DEPARTMENT_HEAD', N'Trưởng phòng ban', N'Quản lý nhân sự, chấm công và duyệt đơn từ trong phạm vi phòng ban', 0, 1, GETUTCDATE());
        PRINT N'[OK] Đã thêm vai trò DEPARTMENT_HEAD.';
    END
END
GO

-- ==========================================================================================
-- PHẦN 5: GHI NHẬN LỊCH SỬ EF CORE MIGRATION (NẾU BẢNG ĐẦY ĐỦ)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.EmployeeRelatives', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Invoices', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'MustChangePassword') IS NOT NULL
   AND COL_LENGTH(N'dbo.LeaveRequests', N'CreatedByUserId') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[__EFMigrationsHistory]
        WHERE [MigrationId] = N'20261104175015_V6_00_Security_And_Architecture'
    )
    BEGIN
        INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20261104175015_V6_00_Security_And_Architecture', N'8.0.0');
        PRINT N'[OK] Đã ghi nhận 20261104175015_V6_00_Security_And_Architecture vào __EFMigrationsHistory.';
    END
END
GO

PRINT N'=== [V6_00] Hoàn tất nâng cấp cơ sở dữ liệu Phiên 1 ===';
GO
