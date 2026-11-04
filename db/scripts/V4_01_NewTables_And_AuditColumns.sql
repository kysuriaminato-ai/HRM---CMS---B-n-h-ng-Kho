-- ==========================================================================
-- PHIÊN 4: THIẾT KẾ VÀ TRIỂN KHAI CƠ SỞ DỮ LIỆU
-- Script: V4_01_NewTables_And_AuditColumns.sql
-- Mô tả: Tạo 13 bảng mới + thêm cột audit (IsDeleted, UpdatedAt, UpdatedBy, CreatedBy)
--         + thêm FK mới (UnitId, CustomerId, LeaveTypeId)
-- Ngày: 2026-11-04
-- Lưu ý: Chạy trên SQL Server. Kiểm tra bảng đã tồn tại trước khi tạo.
-- ==========================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT N'=== PHIÊN 4 DB: Bắt đầu migration ===';

-- ==========================================================================
-- PHẦN 1: THÊM CỘT AUDIT CHO BẢNG HIỆN CÓ
-- Thêm IsDeleted, UpdatedAt, UpdatedBy, CreatedBy cho tất cả bảng nghiệp vụ
-- ==========================================================================

PRINT N'1. Thêm cột audit cho bảng hiện có...';

-- Macro: thêm cột nếu chưa tồn tại
-- Companies
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'IsDeleted')
    ALTER TABLE [Companies] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'UpdatedAt')
    ALTER TABLE [Companies] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'UpdatedBy')
    ALTER TABLE [Companies] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'CreatedBy')
    ALTER TABLE [Companies] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Branches
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'IsDeleted')
    ALTER TABLE [Branches] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'UpdatedAt')
    ALTER TABLE [Branches] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'UpdatedBy')
    ALTER TABLE [Branches] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Branches') AND name = 'CreatedBy')
    ALTER TABLE [Branches] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Departments
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Departments') AND name = 'IsDeleted')
    ALTER TABLE [Departments] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Departments') AND name = 'UpdatedAt')
    ALTER TABLE [Departments] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Departments') AND name = 'UpdatedBy')
    ALTER TABLE [Departments] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Departments') AND name = 'CreatedBy')
    ALTER TABLE [Departments] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Positions
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Positions') AND name = 'IsDeleted')
    ALTER TABLE [Positions] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Positions') AND name = 'UpdatedAt')
    ALTER TABLE [Positions] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Positions') AND name = 'UpdatedBy')
    ALTER TABLE [Positions] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Positions') AND name = 'CreatedBy')
    ALTER TABLE [Positions] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Employees
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'IsDeleted')
    ALTER TABLE [Employees] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'UpdatedAt')
    ALTER TABLE [Employees] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'UpdatedBy')
    ALTER TABLE [Employees] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'CreatedBy')
    ALTER TABLE [Employees] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Contracts
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'IsDeleted')
    ALTER TABLE [Contracts] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'UpdatedAt')
    ALTER TABLE [Contracts] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'UpdatedBy')
    ALTER TABLE [Contracts] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'CreatedBy')
    ALTER TABLE [Contracts] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Categories
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Categories') AND name = 'IsDeleted')
    ALTER TABLE [Categories] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Categories') AND name = 'UpdatedAt')
    ALTER TABLE [Categories] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Categories') AND name = 'UpdatedBy')
    ALTER TABLE [Categories] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Categories') AND name = 'CreatedBy')
    ALTER TABLE [Categories] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Products
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsDeleted')
    ALTER TABLE [Products] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'UpdatedAt')
    ALTER TABLE [Products] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'UpdatedBy')
    ALTER TABLE [Products] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CreatedBy')
    ALTER TABLE [Products] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Suppliers
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'IsDeleted')
    ALTER TABLE [Suppliers] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'UpdatedAt')
    ALTER TABLE [Suppliers] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'UpdatedBy')
    ALTER TABLE [Suppliers] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'CreatedBy')
    ALTER TABLE [Suppliers] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Contents
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contents') AND name = 'IsDeleted')
    ALTER TABLE [Contents] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contents') AND name = 'UpdatedBy')
    ALTER TABLE [Contents] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contents') AND name = 'CreatedBy')
    ALTER TABLE [Contents] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Menus
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Menus') AND name = 'IsDeleted')
    ALTER TABLE [Menus] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Menus') AND name = 'UpdatedAt')
    ALTER TABLE [Menus] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Menus') AND name = 'UpdatedBy')
    ALTER TABLE [Menus] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Menus') AND name = 'CreatedBy')
    ALTER TABLE [Menus] ADD [CreatedBy] NVARCHAR(100) NULL;

-- Banners
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Banners') AND name = 'IsDeleted')
    ALTER TABLE [Banners] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Banners') AND name = 'UpdatedAt')
    ALTER TABLE [Banners] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Banners') AND name = 'UpdatedBy')
    ALTER TABLE [Banners] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Banners') AND name = 'CreatedBy')
    ALTER TABLE [Banners] ADD [CreatedBy] NVARCHAR(100) NULL;

-- LeaveRequests
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LeaveRequests') AND name = 'IsDeleted')
    ALTER TABLE [LeaveRequests] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LeaveRequests') AND name = 'UpdatedAt')
    ALTER TABLE [LeaveRequests] ADD [UpdatedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LeaveRequests') AND name = 'UpdatedBy')
    ALTER TABLE [LeaveRequests] ADD [UpdatedBy] NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LeaveRequests') AND name = 'CreatedBy')
    ALTER TABLE [LeaveRequests] ADD [CreatedBy] NVARCHAR(100) NULL;

PRINT N'   → Xong cột audit cho bảng hiện có.';

-- ==========================================================================
-- PHẦN 2: THÊM CỘT FK MỚI CHO BẢNG HIỆN CÓ
-- ==========================================================================

PRINT N'2. Thêm FK mới cho bảng hiện có...';

-- Products.UnitId → Units (sẽ tạo bảng Units bên dưới)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'UnitId')
    ALTER TABLE [Products] ADD [UnitId] INT NULL;

-- Orders.CustomerId → Customers (sẽ tạo bảng Customers bên dưới)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CustomerId')
    ALTER TABLE [Orders] ADD [CustomerId] INT NULL;

-- DebtRecords.CustomerId → Customers
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DebtRecords') AND name = 'CustomerId')
    ALTER TABLE [DebtRecords] ADD [CustomerId] INT NULL;

-- LeaveRequests.LeaveTypeId → LeaveTypes (sẽ tạo bảng LeaveTypes bên dưới)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LeaveRequests') AND name = 'LeaveTypeId')
    ALTER TABLE [LeaveRequests] ADD [LeaveTypeId] INT NULL;

PRINT N'   → Xong FK mới.';

-- ==========================================================================
-- PHẦN 3: TẠO BẢNG MỚI — HRM
-- ==========================================================================

PRINT N'3. Tạo bảng HRM mới...';

-- LeaveTypes (Loại nghỉ phép)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LeaveTypes')
BEGIN
    CREATE TABLE [LeaveTypes] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [Code]                NVARCHAR(50)  NOT NULL,
        [Name]                NVARCHAR(150) NOT NULL,
        [MaxDaysPerYear]      INT NOT NULL DEFAULT 12,
        [IsPaid]              BIT NOT NULL DEFAULT 1,
        [AllowHalfDay]        BIT NOT NULL DEFAULT 0,
        [RequiresAttachment]  BIT NOT NULL DEFAULT 0,
        [Description]         NVARCHAR(500) NULL,
        [IsActive]            BIT NOT NULL DEFAULT 1,
        [CreatedAt]           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]           BIT NOT NULL DEFAULT 0,
        [UpdatedAt]           DATETIME2 NULL,
        [UpdatedBy]           NVARCHAR(100) NULL,
        [CreatedBy]           NVARCHAR(100) NULL,
        CONSTRAINT [PK_LeaveTypes] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_LeaveTypes_Code] ON [LeaveTypes]([Code]);
    PRINT N'   → Bảng LeaveTypes đã tạo.';
END

-- JobPostings (Tin tuyển dụng)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobPostings')
BEGIN
    CREATE TABLE [JobPostings] (
        [Id]              INT IDENTITY(1,1) NOT NULL,
        [Code]            NVARCHAR(50)  NOT NULL,
        [Title]           NVARCHAR(300) NOT NULL,
        [DepartmentId]    INT NOT NULL,
        [PositionId]      INT NOT NULL,
        [Vacancies]       INT NOT NULL DEFAULT 1,
        [Description]     NVARCHAR(MAX) NULL,
        [Requirements]    NVARCHAR(MAX) NULL,
        [SalaryRange]     NVARCHAR(100) NULL,
        [WorkLocation]    NVARCHAR(200) NULL,
        [Status]          NVARCHAR(50)  NOT NULL DEFAULT 'Draft',
        [OpenDate]        DATETIME2 NOT NULL,
        [CloseDate]       DATETIME2 NULL,
        [CreatedAt]       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]       BIT NOT NULL DEFAULT 0,
        [UpdatedAt]       DATETIME2 NULL,
        [UpdatedBy]       NVARCHAR(100) NULL,
        [CreatedBy]       NVARCHAR(100) NULL,
        CONSTRAINT [PK_JobPostings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobPostings_Departments] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobPostings_Positions] FOREIGN KEY ([PositionId]) REFERENCES [Positions]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_JobPostings_Code] ON [JobPostings]([Code]);
    PRINT N'   → Bảng JobPostings đã tạo.';
END

-- Candidates (Ứng viên)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Candidates')
BEGIN
    CREATE TABLE [Candidates] (
        [Id]              INT IDENTITY(1,1) NOT NULL,
        [Code]            NVARCHAR(50)  NOT NULL,
        [FullName]        NVARCHAR(150) NOT NULL,
        [Email]           NVARCHAR(150) NULL,
        [Phone]           NVARCHAR(20)  NULL,
        [ResumeUrl]       NVARCHAR(500) NULL,
        [CoverLetter]     NVARCHAR(500) NULL,
        [Status]          NVARCHAR(50)  NOT NULL DEFAULT 'Applied',
        [AppliedDate]     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Note]            NVARCHAR(500) NULL,
        [EvaluationScore] DECIMAL(5,2)  NULL,
        [JobPostingId]    INT NOT NULL,
        [CreatedAt]       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]       BIT NOT NULL DEFAULT 0,
        [UpdatedAt]       DATETIME2 NULL,
        [UpdatedBy]       NVARCHAR(100) NULL,
        [CreatedBy]       NVARCHAR(100) NULL,
        CONSTRAINT [PK_Candidates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Candidates_JobPostings] FOREIGN KEY ([JobPostingId]) REFERENCES [JobPostings]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_Candidates_Code] ON [Candidates]([Code]);
    PRINT N'   → Bảng Candidates đã tạo.';
END

-- Trainings (Đào tạo)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Trainings')
BEGIN
    CREATE TABLE [Trainings] (
        [Id]           INT IDENTITY(1,1) NOT NULL,
        [Code]         NVARCHAR(50)  NOT NULL,
        [Title]        NVARCHAR(300) NOT NULL,
        [TrainerName]  NVARCHAR(200) NULL,
        [Location]     NVARCHAR(300) NULL,
        [Description]  NVARCHAR(MAX) NULL,
        [StartDate]    DATETIME2 NOT NULL,
        [EndDate]      DATETIME2 NULL,
        [Budget]       DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Status]       NVARCHAR(50)  NOT NULL DEFAULT 'Planned',
        [CreatedAt]    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]    BIT NOT NULL DEFAULT 0,
        [UpdatedAt]    DATETIME2 NULL,
        [UpdatedBy]    NVARCHAR(100) NULL,
        [CreatedBy]    NVARCHAR(100) NULL,
        CONSTRAINT [PK_Trainings] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_Trainings_Code] ON [Trainings]([Code]);
    PRINT N'   → Bảng Trainings đã tạo.';
END

-- TrainingParticipants (Nhân viên tham gia đào tạo)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrainingParticipants')
BEGIN
    CREATE TABLE [TrainingParticipants] (
        [TrainingId]   INT NOT NULL,
        [EmployeeId]   INT NOT NULL,
        [Result]       NVARCHAR(50)  NULL,
        [Score]        DECIMAL(5,2)  NULL,
        [Comment]      NVARCHAR(500) NULL,
        CONSTRAINT [PK_TrainingParticipants] PRIMARY KEY ([TrainingId], [EmployeeId]),
        CONSTRAINT [FK_TrainingParticipants_Trainings] FOREIGN KEY ([TrainingId]) REFERENCES [Trainings]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TrainingParticipants_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees]([Id]) ON DELETE CASCADE
    );
    PRINT N'   → Bảng TrainingParticipants đã tạo.';
END

-- KpiReviews (Đánh giá KPI)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KpiReviews')
BEGIN
    CREATE TABLE [KpiReviews] (
        [Id]             INT IDENTITY(1,1) NOT NULL,
        [EmployeeId]     INT NOT NULL,
        [ReviewerId]     INT NOT NULL,
        [PeriodMonth]    INT NOT NULL,
        [PeriodYear]     INT NOT NULL,
        [TotalScore]     DECIMAL(5,2) NOT NULL DEFAULT 0,
        [Grade]          NVARCHAR(10) NULL,
        [OverallComment] NVARCHAR(500) NULL,
        [Status]         NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        [CreatedAt]      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]      BIT NOT NULL DEFAULT 0,
        [UpdatedAt]      DATETIME2 NULL,
        [UpdatedBy]      NVARCHAR(100) NULL,
        [CreatedBy]      NVARCHAR(100) NULL,
        CONSTRAINT [PK_KpiReviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiReviews_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KpiReviews_Users] FOREIGN KEY ([ReviewerId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_KpiReviews_Employee_Period] ON [KpiReviews]([EmployeeId], [PeriodMonth], [PeriodYear]);
    PRINT N'   → Bảng KpiReviews đã tạo.';
END

-- KpiCriteria (Tiêu chí KPI)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KpiCriteria')
BEGIN
    CREATE TABLE [KpiCriteria] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [KpiReviewId]   INT NOT NULL,
        [CriteriaName]  NVARCHAR(200) NOT NULL,
        [Weight]        DECIMAL(5,2) NOT NULL DEFAULT 0,
        [Score]         DECIMAL(5,2) NOT NULL DEFAULT 0,
        [Comment]       NVARCHAR(500) NULL,
        CONSTRAINT [PK_KpiCriteria] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiCriteria_KpiReviews] FOREIGN KEY ([KpiReviewId]) REFERENCES [KpiReviews]([Id]) ON DELETE CASCADE
    );
    PRINT N'   → Bảng KpiCriteria đã tạo.';
END

PRINT N'   → Xong bảng HRM mới.';

-- ==========================================================================
-- PHẦN 4: TẠO BẢNG MỚI — CMS
-- ==========================================================================

PRINT N'4. Tạo bảng CMS mới...';

-- Tags
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tags')
BEGIN
    CREATE TABLE [Tags] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [Name]       NVARCHAR(100) NOT NULL,
        [Slug]       NVARCHAR(150) NOT NULL,
        [UsageCount] INT NOT NULL DEFAULT 0,
        [IsActive]   BIT NOT NULL DEFAULT 1,
        [CreatedAt]  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]  BIT NOT NULL DEFAULT 0,
        [UpdatedAt]  DATETIME2 NULL,
        [UpdatedBy]  NVARCHAR(100) NULL,
        [CreatedBy]  NVARCHAR(100) NULL,
        CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_Tags_Name] ON [Tags]([Name]);
    CREATE UNIQUE INDEX [IX_Tags_Slug] ON [Tags]([Slug]);
    PRINT N'   → Bảng Tags đã tạo.';
END

-- ContentTags (many-to-many)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContentTags')
BEGIN
    CREATE TABLE [ContentTags] (
        [ContentId]  INT NOT NULL,
        [TagId]      INT NOT NULL,
        CONSTRAINT [PK_ContentTags] PRIMARY KEY ([ContentId], [TagId]),
        CONSTRAINT [FK_ContentTags_Contents] FOREIGN KEY ([ContentId]) REFERENCES [Contents]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ContentTags_Tags] FOREIGN KEY ([TagId]) REFERENCES [Tags]([Id]) ON DELETE CASCADE
    );
    PRINT N'   → Bảng ContentTags đã tạo.';
END

-- Media
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Media')
BEGIN
    CREATE TABLE [Media] (
        [Id]            BIGINT IDENTITY(1,1) NOT NULL,
        [FileName]      NVARCHAR(300) NOT NULL,
        [OriginalName]  NVARCHAR(300) NOT NULL,
        [MimeType]      NVARCHAR(100) NOT NULL,
        [FileSize]      BIGINT NOT NULL DEFAULT 0,
        [Url]           NVARCHAR(500) NOT NULL,
        [ThumbnailUrl]  NVARCHAR(500) NULL,
        [AltText]       NVARCHAR(300) NULL,
        [Folder]        NVARCHAR(100) NOT NULL DEFAULT 'general',
        [UploadedBy]    INT NULL,
        [CreatedAt]     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]     BIT NOT NULL DEFAULT 0,
        [UpdatedAt]     DATETIME2 NULL,
        [UpdatedBy]     NVARCHAR(100) NULL,
        [CreatedBy]     NVARCHAR(100) NULL,
        CONSTRAINT [PK_Media] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Media_Users] FOREIGN KEY ([UploadedBy]) REFERENCES [Users]([Id]) ON DELETE SET NULL
    );
    PRINT N'   → Bảng Media đã tạo.';
END

-- SeoMeta
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SeoMeta')
BEGIN
    CREATE TABLE [SeoMeta] (
        [Id]              INT IDENTITY(1,1) NOT NULL,
        [EntityType]      NVARCHAR(50)  NOT NULL,
        [EntityId]        INT NOT NULL,
        [MetaTitle]       NVARCHAR(200) NULL,
        [MetaDescription] NVARCHAR(500) NULL,
        [MetaKeywords]    NVARCHAR(500) NULL,
        [CanonicalUrl]    NVARCHAR(500) NULL,
        [OgImage]         NVARCHAR(500) NULL,
        [StructuredData]  NVARCHAR(MAX) NULL,
        [CreatedAt]       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]       BIT NOT NULL DEFAULT 0,
        [UpdatedAt]       DATETIME2 NULL,
        [UpdatedBy]       NVARCHAR(100) NULL,
        [CreatedBy]       NVARCHAR(100) NULL,
        CONSTRAINT [PK_SeoMeta] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_SeoMeta_EntityType_EntityId] ON [SeoMeta]([EntityType], [EntityId]);
    PRINT N'   → Bảng SeoMeta đã tạo.';
END

PRINT N'   → Xong bảng CMS mới.';

-- ==========================================================================
-- PHẦN 5: TẠO BẢNG MỚI — KHO & BÁN HÀNG
-- ==========================================================================

PRINT N'5. Tạo bảng SCM mới...';

-- Units (Đơn vị tính)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Units')
BEGIN
    CREATE TABLE [Units] (
        [Id]          INT IDENTITY(1,1) NOT NULL,
        [Code]        NVARCHAR(50)  NOT NULL,
        [Name]        NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [IsActive]    BIT NOT NULL DEFAULT 1,
        [CreatedAt]   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]   BIT NOT NULL DEFAULT 0,
        [UpdatedAt]   DATETIME2 NULL,
        [UpdatedBy]   NVARCHAR(100) NULL,
        [CreatedBy]   NVARCHAR(100) NULL,
        CONSTRAINT [PK_Units] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_Units_Code] ON [Units]([Code]);
    PRINT N'   → Bảng Units đã tạo.';
END

-- Customers (Khách hàng)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Customers')
BEGIN
    CREATE TABLE [Customers] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [Code]          NVARCHAR(50)  NOT NULL,
        [FullName]      NVARCHAR(200) NOT NULL,
        [Phone]         NVARCHAR(20)  NULL,
        [Email]         NVARCHAR(150) NULL,
        [Address]       NVARCHAR(500) NULL,
        [TaxCode]       NVARCHAR(50)  NULL,
        [CustomerType]  NVARCHAR(50)  NOT NULL DEFAULT 'Individual',
        [TotalDebt]     DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalRevenue]  DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Note]          NVARCHAR(500) NULL,
        [IsActive]      BIT NOT NULL DEFAULT 1,
        [CreatedAt]     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]     BIT NOT NULL DEFAULT 0,
        [UpdatedAt]     DATETIME2 NULL,
        [UpdatedBy]     NVARCHAR(100) NULL,
        [CreatedBy]     NVARCHAR(100) NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_Customers_Code] ON [Customers]([Code]);
    CREATE UNIQUE INDEX [IX_Customers_Phone] ON [Customers]([Phone]) WHERE [Phone] IS NOT NULL;
    PRINT N'   → Bảng Customers đã tạo.';
END

-- Warehouses (Kho hàng)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Warehouses')
BEGIN
    CREATE TABLE [Warehouses] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [Code]       NVARCHAR(50)  NOT NULL,
        [Name]       NVARCHAR(200) NOT NULL,
        [Address]    NVARCHAR(500) NULL,
        [Phone]      NVARCHAR(20)  NULL,
        [ManagerId]  INT NULL,
        [BranchId]   INT NOT NULL,
        [IsActive]   BIT NOT NULL DEFAULT 1,
        [CreatedAt]  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]  BIT NOT NULL DEFAULT 0,
        [UpdatedAt]  DATETIME2 NULL,
        [UpdatedBy]  NVARCHAR(100) NULL,
        [CreatedBy]  NVARCHAR(100) NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouses_Branches] FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Warehouses_Employees] FOREIGN KEY ([ManagerId]) REFERENCES [Employees]([Id]) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [Warehouses]([Code]);
    PRINT N'   → Bảng Warehouses đã tạo.';
END

-- Invoices (Hóa đơn)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Invoices')
BEGIN
    CREATE TABLE [Invoices] (
        [Id]               BIGINT IDENTITY(1,1) NOT NULL,
        [InvoiceCode]      NVARCHAR(50) NOT NULL,
        [InvoiceType]      NVARCHAR(20) NOT NULL DEFAULT 'SALES',
        [TotalAmount]      DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TaxAmount]        DECIMAL(18,2) NOT NULL DEFAULT 0,
        [GrandTotal]       DECIMAL(18,2) NOT NULL DEFAULT 0,
        [InvoiceDate]      DATETIME2 NOT NULL,
        [Note]             NVARCHAR(500) NULL,
        [Status]           NVARCHAR(50)  NOT NULL DEFAULT 'Draft',
        [OrderId]          BIGINT NULL,
        [PurchaseOrderId]  BIGINT NULL,
        [CustomerId]       INT NULL,
        [SupplierId]       INT NULL,
        [CreatedBy]        INT NULL,
        [CreatedAt]        DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]        BIT NOT NULL DEFAULT 0,
        [UpdatedAt]        DATETIME2 NULL,
        [UpdatedBy]        NVARCHAR(100) NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Orders] FOREIGN KEY ([OrderId]) REFERENCES [Orders]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_PurchaseOrders] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Suppliers] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Users] FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceCode] ON [Invoices]([InvoiceCode]);
    PRINT N'   → Bảng Invoices đã tạo.';
END

-- Payments (Thanh toán)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Payments')
BEGIN
    CREATE TABLE [Payments] (
        [Id]             BIGINT IDENTITY(1,1) NOT NULL,
        [PaymentCode]    NVARCHAR(50)  NOT NULL,
        [Amount]         DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PaymentMethod]  NVARCHAR(50)  NOT NULL DEFAULT 'Cash',
        [PaymentDate]    DATETIME2 NOT NULL,
        [ReferenceType]  NVARCHAR(20)  NOT NULL DEFAULT 'ORDER',
        [ReferenceId]    BIGINT NOT NULL,
        [Note]           NVARCHAR(500) NULL,
        [CreatedBy]      INT NULL,
        [CreatedAt]      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsDeleted]      BIT NOT NULL DEFAULT 0,
        [UpdatedAt]      DATETIME2 NULL,
        [UpdatedBy]      NVARCHAR(100) NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Users] FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_Payments_PaymentCode] ON [Payments]([PaymentCode]);
    PRINT N'   → Bảng Payments đã tạo.';
END

PRINT N'   → Xong bảng SCM mới.';

-- ==========================================================================
-- PHẦN 6: THÊM FK CONSTRAINTS CHO CỘT MỚI TRÊN BẢNG CŨ
-- (sau khi bảng đích đã tạo)
-- ==========================================================================

PRINT N'6. Thêm FK constraints...';

-- Products.UnitId → Units
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Products_Units')
    ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Units]
        FOREIGN KEY ([UnitId]) REFERENCES [Units]([Id]) ON DELETE SET NULL;

-- Orders.CustomerId → Customers
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_Customers')
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Customers]
        FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]) ON DELETE SET NULL;

-- DebtRecords.CustomerId → Customers
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DebtRecords_Customers')
    ALTER TABLE [DebtRecords] ADD CONSTRAINT [FK_DebtRecords_Customers]
        FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]) ON DELETE NO ACTION;

-- LeaveRequests.LeaveTypeId → LeaveTypes
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LeaveRequests_LeaveTypes')
    ALTER TABLE [LeaveRequests] ADD CONSTRAINT [FK_LeaveRequests_LeaveTypes]
        FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes]([Id]) ON DELETE SET NULL;

PRINT N'   → Xong FK constraints.';

PRINT N'=== PHIÊN 4 DB: Migration hoàn tất ===';

COMMIT TRANSACTION;
GO
