/*
  Tạo 3 bảng còn thiếu trong database cũ: EmployeeDependents, LoginAttempts, PayrollAllowanceTypes.
  Nguyên nhân: database được tạo trước khi có các bảng này. Migration InitialBaseline đã được đánh dấu
  là đã áp dụng trên DB cũ nên EF không tạo lại các bảng này.
  - Chỉ THÊM bảng và index còn thiếu. Không sửa, không xóa dữ liệu.
  - Idempotent: chạy lại an toàn; bảng/index đã có thì bỏ qua.
  - Chạy trong transaction: lỗi giữa chừng thì không để lại trạng thái dở dang.
  - Định nghĩa khớp với migration InitialBaseline. Sao lưu database trước khi chạy.
*/
USE [ERP_HRM_CMS_DB];
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- 1. PayrollAllowanceTypes
IF OBJECT_ID(N'dbo.PayrollAllowanceTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayrollAllowanceTypes (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PayrollAllowanceTypes PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        DefaultAmount DECIMAL(18,2) NOT NULL,
        IsTaxExempt BIT NOT NULL,
        TaxExemptLimit DECIMAL(18,2) NULL,
        SourceNote NVARCHAR(500) NULL,
        NeedsConfirmation BIT NOT NULL,
        IsActive BIT NOT NULL
    );
    PRINT N'Đã tạo bảng dbo.PayrollAllowanceTypes.';
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PayrollAllowanceTypes_Code' AND object_id = OBJECT_ID(N'dbo.PayrollAllowanceTypes'))
    CREATE UNIQUE INDEX IX_PayrollAllowanceTypes_Code ON dbo.PayrollAllowanceTypes (Code);

-- 2. LoginAttempts (legacy-schema-fixes.sql cũng tạo bảng này; IF OBJECT_ID bảo đảm không tạo trùng)
IF OBJECT_ID(N'dbo.LoginAttempts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoginAttempts (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoginAttempts PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        IpAddress NVARCHAR(45) NOT NULL,
        FailedCount INT NOT NULL,
        LockoutLevel INT NOT NULL,
        LockedUntilUtc DATETIME2 NULL,
        LastFailedUtc DATETIME2 NOT NULL
    );
    PRINT N'Đã tạo bảng dbo.LoginAttempts.';
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LoginAttempts_Username_IpAddress' AND object_id = OBJECT_ID(N'dbo.LoginAttempts'))
    CREATE UNIQUE INDEX IX_LoginAttempts_Username_IpAddress ON dbo.LoginAttempts (Username, IpAddress);

-- 3. EmployeeDependents
IF OBJECT_ID(N'dbo.EmployeeDependents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeDependents (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeDependents PRIMARY KEY,
        EmployeeId INT NOT NULL,
        FullName NVARCHAR(150) NOT NULL,
        Relationship NVARCHAR(50) NOT NULL,
        DateOfBirth DATETIME2 NULL,
        FromDate DATETIME2 NOT NULL,
        ToDate DATETIME2 NULL,
        Note NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_EmployeeDependents_Employees_EmployeeId FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees (Id) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng dbo.EmployeeDependents.';
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmployeeDependents_EmployeeId_FromDate' AND object_id = OBJECT_ID(N'dbo.EmployeeDependents'))
    CREATE INDEX IX_EmployeeDependents_EmployeeId_FromDate ON dbo.EmployeeDependents (EmployeeId, FromDate);

COMMIT TRANSACTION;
PRINT N'Hoàn tất kiểm tra 3 bảng.';
