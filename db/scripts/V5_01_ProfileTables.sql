-- =============================================
-- PHIÊN 5: Tạo bảng hồ sơ mở rộng nhân viên
-- Ngày: 2026-11-04
-- Mô tả: Thêm 5 bảng hỗ trợ module Nhân sự nâng cao
--   - EmployeeRelatives: Người thân / phụ thuộc
--   - EmployeeSkills: Kỹ năng
--   - EmployeeCertificates: Chứng chỉ / bằng cấp
--   - EmployeeDocuments: Tài liệu đính kèm
--   - EmployeeChangeHistories: Lịch sử thay đổi
-- =============================================

-- 1. Bảng Người thân / Phụ thuộc
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EmployeeRelatives')
BEGIN
    CREATE TABLE [dbo].[EmployeeRelatives] (
        [Id]            INT             IDENTITY(1,1) NOT NULL,
        [EmployeeId]    INT             NOT NULL,
        [FullName]      NVARCHAR(200)   NOT NULL,
        [Relationship]  NVARCHAR(50)    NULL,
        [Phone]         NVARCHAR(20)    NULL,
        [Address]       NVARCHAR(500)   NULL,
        [IsDependant]   BIT             NOT NULL DEFAULT 0,
        [DateOfBirth]   DATETIME2       NULL,
        [Note]          NVARCHAR(500)   NULL,
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_EmployeeRelatives] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_EmployeeRelatives_Employees] FOREIGN KEY ([EmployeeId])
            REFERENCES [dbo].[Employees]([Id]) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng EmployeeRelatives.';
END
GO

-- 2. Bảng Kỹ năng
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EmployeeSkills')
BEGIN
    CREATE TABLE [dbo].[EmployeeSkills] (
        [Id]            INT             IDENTITY(1,1) NOT NULL,
        [EmployeeId]    INT             NOT NULL,
        [SkillName]     NVARCHAR(200)   NOT NULL,
        [Level]         NVARCHAR(50)    NOT NULL DEFAULT 'Beginner',
        [Note]          NVARCHAR(500)   NULL,
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_EmployeeSkills] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_EmployeeSkills_Employees] FOREIGN KEY ([EmployeeId])
            REFERENCES [dbo].[Employees]([Id]) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng EmployeeSkills.';
END
GO

-- 3. Bảng Chứng chỉ / Bằng cấp
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EmployeeCertificates')
BEGIN
    CREATE TABLE [dbo].[EmployeeCertificates] (
        [Id]                INT             IDENTITY(1,1) NOT NULL,
        [EmployeeId]        INT             NOT NULL,
        [CertificateName]   NVARCHAR(300)   NOT NULL,
        [IssuedBy]          NVARCHAR(300)   NULL,
        [IssuedDate]        DATETIME2       NULL,
        [ExpiryDate]        DATETIME2       NULL,
        [FileUrl]           NVARCHAR(500)   NULL,
        [Note]              NVARCHAR(500)   NULL,
        [CreatedAt]         DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_EmployeeCertificates] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_EmployeeCertificates_Employees] FOREIGN KEY ([EmployeeId])
            REFERENCES [dbo].[Employees]([Id]) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng EmployeeCertificates.';
END
GO

-- 4. Bảng Tài liệu đính kèm
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EmployeeDocuments')
BEGIN
    CREATE TABLE [dbo].[EmployeeDocuments] (
        [Id]            INT             IDENTITY(1,1) NOT NULL,
        [EmployeeId]    INT             NOT NULL,
        [DocumentName]  NVARCHAR(300)   NOT NULL,
        [DocumentType]  NVARCHAR(100)   NULL,
        [FileUrl]       NVARCHAR(500)   NOT NULL,
        [FileSize]      BIGINT          NOT NULL DEFAULT 0,
        [ContentType]   NVARCHAR(100)   NULL,
        [Note]          NVARCHAR(500)   NULL,
        [UploadedBy]    INT             NULL,
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_EmployeeDocuments] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_EmployeeDocuments_Employees] FOREIGN KEY ([EmployeeId])
            REFERENCES [dbo].[Employees]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeDocuments_Users] FOREIGN KEY ([UploadedBy])
            REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    PRINT N'Đã tạo bảng EmployeeDocuments.';
END
GO

-- 5. Bảng Lịch sử thay đổi (high-volume: dùng BIGINT cho Id)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EmployeeChangeHistories')
BEGIN
    CREATE TABLE [dbo].[EmployeeChangeHistories] (
        [Id]            BIGINT          IDENTITY(1,1) NOT NULL,
        [EmployeeId]    INT             NOT NULL,
        [ChangeType]    NVARCHAR(50)    NOT NULL,  -- Department, Position, Salary, Status
        [OldValue]      NVARCHAR(500)   NULL,
        [NewValue]      NVARCHAR(500)   NULL,
        [EffectiveDate] DATETIME2       NOT NULL,
        [Reason]        NVARCHAR(500)   NULL,
        [ChangedBy]     INT             NULL,
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_EmployeeChangeHistories] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_EmployeeChangeHistories_Employees] FOREIGN KEY ([EmployeeId])
            REFERENCES [dbo].[Employees]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeChangeHistories_Users] FOREIGN KEY ([ChangedBy])
            REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );

    -- Index tối ưu truy vấn lịch sử theo nhân viên + loại thay đổi
    CREATE NONCLUSTERED INDEX [IX_ChangeHistories_Employee_Type_Date]
        ON [dbo].[EmployeeChangeHistories] ([EmployeeId], [ChangeType], [EffectiveDate] DESC);

    PRINT N'Đã tạo bảng EmployeeChangeHistories.';
END
GO

-- 6. Thêm cột ParentId vào Departments (nếu chưa có) cho cấu trúc cây đệ quy
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Departments' AND COLUMN_NAME = 'ParentId'
)
BEGIN
    ALTER TABLE [dbo].[Departments]
        ADD [ParentId] INT NULL;

    ALTER TABLE [dbo].[Departments]
        ADD CONSTRAINT [FK_Departments_ParentDepartment]
        FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Departments]([Id])
        ON DELETE NO ACTION;

    PRINT N'Đã thêm cột ParentId vào bảng Departments.';
END
GO

PRINT N'=== PHIÊN 5: Hoàn tất tạo bảng hồ sơ mở rộng ===';
GO
