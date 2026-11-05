-- ==========================================================================================
-- PHIÊN 2: HOÀN THIỆN CƠ SỞ DỮ LIỆU ENTERPRISE HRM TRÊN SSMS & EF CORE
-- Script: db/scripts/V6_01_EnterpriseHRM_Schema_And_Indexes.sql
-- Ngày: 2026-11-05
-- Tác giả: Enterprise SQL Server DBA & EF Core Architect
-- Mô tả:
--   1. Tạo các bảng Thực thể khép kín vòng đời Nhân sự (Hire-to-Retire):
--      - EmployeeLeaveBalances (Quỹ phép năm nhân viên theo năm)
--      - AttendanceLocks (Khóa kỳ chấm công theo tháng/chi nhánh)
--      - AttendanceAdjustmentRequests (Đơn giải trình chấm công / bổ sung giờ vào-ra)
--      - HrDecisions (Quyết định Nhân sự điện tử)
--      - CompanyAssets & EmployeeAssetAssignments (Quản lý & Cấp phát tài sản doanh nghiệp)
--      - OnboardingChecklists & OnboardingTasks (Quy trình hội nhập & bàn giao nghỉ việc)
--      - InterviewSchedules (Lịch phỏng vấn ứng viên tuyển dụng)
--   2. Bổ sung cột Slug cho JobPostings (chuẩn SEO tuyển dụng).
--   3. Thiết lập đầy đủ CHECK CONSTRAINTS bảo vệ toàn vẹn dữ liệu nghiệp vụ.
--   4. Thiết lập bộ Non-Clustered & Covering Indexes tối ưu hiệu năng truy vấn nóng.
--   5. Gắn Extended Properties (sp_addextendedproperty) mô tả tiếng Việt cho SSMS Database Diagram.
-- Lưu ý: Script hoàn toàn IDEMPOTENT - chạy an toàn nhiều lần trực tiếp trên SSMS.
-- ==========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=== [V6_01] Bắt đầu khởi tạo Schema, Constraints & Indexes cho Enterprise HRM ===';
GO

-- ==========================================================================================
-- PHẦN 1: BỔ SUNG CỘT CHO CÁC BẢNG HIỆN HỮU
-- ==========================================================================================

-- 1.1. Cột Slug cho JobPostings
IF OBJECT_ID(N'dbo.JobPostings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.JobPostings', N'Slug') IS NULL
    BEGIN
        ALTER TABLE [dbo].[JobPostings]
            ADD [Slug] NVARCHAR(350) NOT NULL CONSTRAINT [DF_JobPostings_Slug] DEFAULT ('');
        PRINT N'[OK] Đã thêm cột JobPostings.Slug.';
    END
    ELSE
    BEGIN
        PRINT N'[SKIP] Cột JobPostings.Slug đã tồn tại.';
    END
END
GO

-- 1.2. Cột Audit cho Employees (nếu chưa có)
IF OBJECT_ID(N'dbo.Employees', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Employees', N'IsDeleted') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Employees]
            ADD [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Employees_IsDeleted] DEFAULT (0);
        PRINT N'[OK] Đã thêm cột Employees.IsDeleted.';
    END
    IF COL_LENGTH(N'dbo.Employees', N'UpdatedAt') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Employees] ADD [UpdatedAt] DATETIME2 NULL;
        PRINT N'[OK] Đã thêm cột Employees.UpdatedAt.';
    END
    IF COL_LENGTH(N'dbo.Employees', N'UpdatedBy') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Employees] ADD [UpdatedBy] NVARCHAR(100) NULL;
        PRINT N'[OK] Đã thêm cột Employees.UpdatedBy.';
    END
    IF COL_LENGTH(N'dbo.Employees', N'CreatedBy') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Employees] ADD [CreatedBy] NVARCHAR(100) NULL;
        PRINT N'[OK] Đã thêm cột Employees.CreatedBy.';
    END
END
GO

-- ==========================================================================================
-- PHẦN 2: TẠO CÁC BẢNG ENTERPRISE HRM MỚI (IDEMPOTENT)
-- ==========================================================================================

-- 2.1. Quỹ phép năm nhân viên: EmployeeLeaveBalances
IF OBJECT_ID(N'dbo.EmployeeLeaveBalances', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmployeeLeaveBalances] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EmployeeLeaveBalances] PRIMARY KEY,
        [EmployeeId] INT NOT NULL,
        [Year] INT NOT NULL,
        [LeaveTypeId] INT NOT NULL,
        [StandardDays] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_EmployeeLeaveBalances_StandardDays] DEFAULT (12.00),
        [SeniorityBonusDays] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_EmployeeLeaveBalances_SeniorityBonusDays] DEFAULT (0.00),
        [CarriedOverDays] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_EmployeeLeaveBalances_CarriedOverDays] DEFAULT (0.00),
        [UsedDays] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_EmployeeLeaveBalances_UsedDays] DEFAULT (0.00),
        [RemainingDays] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_EmployeeLeaveBalances_RemainingDays] DEFAULT (12.00),
        [ExpiredCarryOverDate] DATETIME2 NULL,
        [Note] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_EmployeeLeaveBalances_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_EmployeeLeaveBalances_IsDeleted] DEFAULT (0),
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_EmployeeLeaveBalances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeLeaveBalances_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [dbo].[LeaveTypes]([Id]) ON DELETE NO ACTION
    );
    PRINT N'[OK] Đã tạo bảng EmployeeLeaveBalances.';
END
GO

-- 2.2. Khóa kỳ chấm công: AttendanceLocks
IF OBJECT_ID(N'dbo.AttendanceLocks', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AttendanceLocks] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AttendanceLocks] PRIMARY KEY,
        [Month] INT NOT NULL,
        [Year] INT NOT NULL,
        [BranchId] INT NULL,
        [DepartmentId] INT NULL,
        [IsLocked] BIT NOT NULL CONSTRAINT [DF_AttendanceLocks_IsLocked] DEFAULT (1),
        [LockedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AttendanceLocks_LockedAt] DEFAULT (SYSUTCDATETIME()),
        [LockedById] INT NOT NULL,
        [Note] NVARCHAR(500) NULL,
        [UnlockedAt] DATETIME2 NULL,
        [UnlockedById] INT NULL,
        [UnlockReason] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AttendanceLocks_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_AttendanceLocks_IsDeleted] DEFAULT (0),
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_AttendanceLocks_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceLocks_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceLocks_Users_LockedById] FOREIGN KEY ([LockedById]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceLocks_Users_UnlockedById] FOREIGN KEY ([UnlockedById]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    PRINT N'[OK] Đã tạo bảng AttendanceLocks.';
END
GO

-- 2.3. Đơn giải trình chấm công: AttendanceAdjustmentRequests
IF OBJECT_ID(N'dbo.AttendanceAdjustmentRequests', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AttendanceAdjustmentRequests] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AttendanceAdjustmentRequests] PRIMARY KEY,
        [RequestCode] NVARCHAR(50) NOT NULL,
        [EmployeeId] INT NOT NULL,
        [WorkDate] DATETIME2 NOT NULL,
        [ShiftId] INT NULL,
        [AdjustmentType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_AttendanceAdjustmentRequests_Type] DEFAULT ('MissedCheckIn'),
        [ActualInTime] TIME NULL,
        [ActualOutTime] TIME NULL,
        [Reason] NVARCHAR(500) NOT NULL,
        [AttachmentUrl] NVARCHAR(500) NULL,
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_AttendanceAdjustmentRequests_Status] DEFAULT ('Pending'),
        [ApprovedById] INT NULL,
        [ApprovedAt] DATETIME2 NULL,
        [ApprovalNote] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AttendanceAdjustmentRequests_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_AttendanceAdjustmentRequests_IsDeleted] DEFAULT (0),
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_AttendanceAdjustmentRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceAdjustmentRequests_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [dbo].[Shifts]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceAdjustmentRequests_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    PRINT N'[OK] Đã tạo bảng AttendanceAdjustmentRequests.';
END
GO

-- 2.4. Quyết định Nhân sự điện tử: HrDecisions
IF OBJECT_ID(N'dbo.HrDecisions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[HrDecisions] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_HrDecisions] PRIMARY KEY,
        [DecisionNumber] NVARCHAR(50) NOT NULL,
        [EmployeeId] INT NOT NULL,
        [DecisionType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_HrDecisions_DecisionType] DEFAULT ('Appointment'),
        [Title] NVARCHAR(300) NOT NULL,
        [EffectiveDate] DATETIME2 NOT NULL,
        [SignDate] DATETIME2 NOT NULL CONSTRAINT [DF_HrDecisions_SignDate] DEFAULT (SYSUTCDATETIME()),
        [OldDepartmentId] INT NULL,
        [NewDepartmentId] INT NULL,
        [OldPositionId] INT NULL,
        [NewPositionId] INT NULL,
        [OldSalary] DECIMAL(18,2) NULL,
        [NewSalary] DECIMAL(18,2) NULL,
        [Amount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_HrDecisions_Amount] DEFAULT (0),
        [Reason] NVARCHAR(1000) NULL,
        [AttachmentUrl] NVARCHAR(500) NULL,
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_HrDecisions_Status] DEFAULT ('Issued'),
        [ApprovedById] INT NULL,
        [ApprovedAt] DATETIME2 NULL,
        [SignerName] NVARCHAR(150) NULL,
        [SignerTitle] NVARCHAR(150) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_HrDecisions_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_HrDecisions_IsDeleted] DEFAULT (0),
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_HrDecisions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HrDecisions_Departments_OldDepartmentId] FOREIGN KEY ([OldDepartmentId]) REFERENCES [dbo].[Departments]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HrDecisions_Departments_NewDepartmentId] FOREIGN KEY ([NewDepartmentId]) REFERENCES [dbo].[Departments]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HrDecisions_Positions_OldPositionId] FOREIGN KEY ([OldPositionId]) REFERENCES [dbo].[Positions]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HrDecisions_Positions_NewPositionId] FOREIGN KEY ([NewPositionId]) REFERENCES [dbo].[Positions]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HrDecisions_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    PRINT N'[OK] Đã tạo bảng HrDecisions.';
END
GO

-- 2.5. Danh mục Tài sản doanh nghiệp: CompanyAssets
IF OBJECT_ID(N'dbo.CompanyAssets', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CompanyAssets] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CompanyAssets] PRIMARY KEY,
        [AssetCode] NVARCHAR(50) NOT NULL,
        [AssetName] NVARCHAR(200) NOT NULL,
        [AssetType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_CompanyAssets_AssetType] DEFAULT ('Laptop'),
        [SerialNumber] NVARCHAR(100) NULL,
        [Specification] NVARCHAR(1000) NULL,
        [PurchaseDate] DATETIME2 NULL,
        [PurchasePrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_CompanyAssets_PurchasePrice] DEFAULT (0),
        [WarrantyExpiryDate] DATETIME2 NULL,
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_CompanyAssets_Status] DEFAULT ('Available'),
        [BranchId] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_CompanyAssets_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_CompanyAssets_IsDeleted] DEFAULT (0),
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_CompanyAssets_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION
    );
    PRINT N'[OK] Đã tạo bảng CompanyAssets.';
END
GO

-- 2.6. Cấp phát tài sản nhân viên: EmployeeAssetAssignments
IF OBJECT_ID(N'dbo.EmployeeAssetAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmployeeAssetAssignments] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EmployeeAssetAssignments] PRIMARY KEY,
        [AssetId] INT NOT NULL,
        [EmployeeId] INT NOT NULL,
        [AssignedDate] DATETIME2 NOT NULL CONSTRAINT [DF_EmployeeAssetAssignments_AssignedDate] DEFAULT (SYSUTCDATETIME()),
        [ConditionOnAssign] NVARCHAR(300) NULL,
        [HandoverDocumentUrl] NVARCHAR(500) NULL,
        [ReturnedDate] DATETIME2 NULL,
        [ConditionOnReturn] NVARCHAR(300) NULL,
        [ReturnHandoverDocumentUrl] NVARCHAR(500) NULL,
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_EmployeeAssetAssignments_Status] DEFAULT ('Active'),
        [AssignedById] INT NULL,
        [ReturnedById] INT NULL,
        [Note] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_EmployeeAssetAssignments_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_EmployeeAssetAssignments_IsDeleted] DEFAULT (0),
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_EmployeeAssetAssignments_CompanyAssets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [dbo].[CompanyAssets]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeAssetAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeAssetAssignments_Users_AssignedById] FOREIGN KEY ([AssignedById]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeAssetAssignments_Users_ReturnedById] FOREIGN KEY ([ReturnedById]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    PRINT N'[OK] Đã tạo bảng EmployeeAssetAssignments.';
END
GO

-- 2.7. Quy trình Onboarding/Offboarding: OnboardingChecklists & OnboardingTasks
IF OBJECT_ID(N'dbo.OnboardingChecklists', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OnboardingChecklists] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OnboardingChecklists] PRIMARY KEY,
        [EmployeeId] INT NOT NULL,
        [Type] NVARCHAR(50) NOT NULL CONSTRAINT [DF_OnboardingChecklists_Type] DEFAULT ('Onboarding'),
        [Title] NVARCHAR(200) NOT NULL,
        [StartDate] DATETIME2 NOT NULL CONSTRAINT [DF_OnboardingChecklists_StartDate] DEFAULT (SYSUTCDATETIME()),
        [TargetCompletionDate] DATETIME2 NULL,
        [CompletedDate] DATETIME2 NULL,
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_OnboardingChecklists_Status] DEFAULT ('InProgress'),
        [AssignedHrId] INT NULL,
        [Note] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_OnboardingChecklists_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_OnboardingChecklists_IsDeleted] DEFAULT (0),
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_OnboardingChecklists_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OnboardingChecklists_Users_AssignedHrId] FOREIGN KEY ([AssignedHrId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    PRINT N'[OK] Đã tạo bảng OnboardingChecklists.';
END
GO

IF OBJECT_ID(N'dbo.OnboardingTasks', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OnboardingTasks] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OnboardingTasks] PRIMARY KEY,
        [ChecklistId] INT NOT NULL,
        [TaskName] NVARCHAR(250) NOT NULL,
        [DepartmentRole] NVARCHAR(50) NOT NULL CONSTRAINT [DF_OnboardingTasks_Role] DEFAULT ('HR'),
        [AssignedToUserId] INT NULL,
        [DueDate] DATETIME2 NULL,
        [CompletedDate] DATETIME2 NULL,
        [IsCompleted] BIT NOT NULL CONSTRAINT [DF_OnboardingTasks_IsCompleted] DEFAULT (0),
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_OnboardingTasks_Status] DEFAULT ('Pending'),
        [Note] NVARCHAR(500) NULL,
        [OrderIndex] INT NOT NULL CONSTRAINT [DF_OnboardingTasks_OrderIndex] DEFAULT (0),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_OnboardingTasks_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_OnboardingTasks_IsDeleted] DEFAULT (0),
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_OnboardingTasks_OnboardingChecklists_ChecklistId] FOREIGN KEY ([ChecklistId]) REFERENCES [dbo].[OnboardingChecklists]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OnboardingTasks_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    PRINT N'[OK] Đã tạo bảng OnboardingTasks.';
END
GO

-- 2.8. Lịch phỏng vấn ứng viên: InterviewSchedules
IF OBJECT_ID(N'dbo.InterviewSchedules', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InterviewSchedules] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_InterviewSchedules] PRIMARY KEY,
        [CandidateId] INT NOT NULL,
        [RoundNumber] INT NOT NULL CONSTRAINT [DF_InterviewSchedules_RoundNumber] DEFAULT (1),
        [Title] NVARCHAR(200) NOT NULL,
        [ScheduledTime] DATETIME2 NOT NULL,
        [DurationMinutes] INT NOT NULL CONSTRAINT [DF_InterviewSchedules_DurationMinutes] DEFAULT (45),
        [InterviewType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_InterviewSchedules_InterviewType] DEFAULT ('Offline'),
        [LocationOrMeetingLink] NVARCHAR(500) NULL,
        [InterviewerEmployeeId] INT NULL,
        [InterviewerUserId] INT NULL,
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_InterviewSchedules_Status] DEFAULT ('Scheduled'),
        [Score] DECIMAL(5,2) NULL,
        [Feedback] NVARCHAR(MAX) NULL,
        [Result] NVARCHAR(50) NOT NULL CONSTRAINT [DF_InterviewSchedules_Result] DEFAULT ('Pending'),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_InterviewSchedules_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_InterviewSchedules_IsDeleted] DEFAULT (0),
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_InterviewSchedules_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [dbo].[Candidates]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InterviewSchedules_Employees_InterviewerEmployeeId] FOREIGN KEY ([InterviewerEmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InterviewSchedules_Users_InterviewerUserId] FOREIGN KEY ([InterviewerUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    PRINT N'[OK] Đã tạo bảng InterviewSchedules.';
END
GO

-- ==========================================================================================
-- PHẦN 3: THIẾT LẬP CHECK CONSTRAINTS BẢO VỆ TOÀN VẸN NGHIỆP VỤ
-- ==========================================================================================

-- 3.1. Đơn nghỉ phép: ToDate >= FromDate và TotalDays > 0
IF OBJECT_ID(N'dbo.CK_LeaveRequests_DateRange', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[LeaveRequests] WITH CHECK
        ADD CONSTRAINT [CK_LeaveRequests_DateRange] CHECK ([ToDate] >= [FromDate]);
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_LeaveRequests_DateRange.';
END

IF OBJECT_ID(N'dbo.CK_LeaveRequests_TotalDays', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[LeaveRequests] WITH CHECK
        ADD CONSTRAINT [CK_LeaveRequests_TotalDays] CHECK ([TotalDays] > 0);
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_LeaveRequests_TotalDays.';
END

-- 3.2. Quỹ phép năm: Năm hợp lệ, ngày phép không âm
IF OBJECT_ID(N'dbo.CK_EmployeeLeaveBalances_Year', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[EmployeeLeaveBalances] WITH CHECK
        ADD CONSTRAINT [CK_EmployeeLeaveBalances_Year] CHECK ([Year] >= 2000 AND [Year] <= 2100);
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_EmployeeLeaveBalances_Year.';
END

IF OBJECT_ID(N'dbo.CK_EmployeeLeaveBalances_Days', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[EmployeeLeaveBalances] WITH CHECK
        ADD CONSTRAINT [CK_EmployeeLeaveBalances_Days] CHECK (
            [StandardDays] >= 0 AND
            [SeniorityBonusDays] >= 0 AND
            [CarriedOverDays] >= 0 AND
            [UsedDays] >= 0 AND
            [RemainingDays] >= 0
        );
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_EmployeeLeaveBalances_Days.';
END

-- 3.3. Khóa kỳ chấm công: Tháng từ 1 đến 12
IF OBJECT_ID(N'dbo.CK_AttendanceLocks_Month', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[AttendanceLocks] WITH CHECK
        ADD CONSTRAINT [CK_AttendanceLocks_Month] CHECK ([Month] >= 1 AND [Month] <= 12);
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_AttendanceLocks_Month.';
END

-- 3.4. Giờ giải trình chấm công: ActualOutTime >= ActualInTime
IF OBJECT_ID(N'dbo.CK_AttendanceAdjustmentRequests_Times', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[AttendanceAdjustmentRequests] WITH CHECK
        ADD CONSTRAINT [CK_AttendanceAdjustmentRequests_Times] CHECK (
            [ActualOutTime] IS NULL OR [ActualInTime] IS NULL OR [ActualOutTime] >= [ActualInTime]
        );
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_AttendanceAdjustmentRequests_Times.';
END

-- 3.5. Quyết định nhân sự: Tiền thưởng/phạt không âm
IF OBJECT_ID(N'dbo.CK_HrDecisions_Amount', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[HrDecisions] WITH CHECK
        ADD CONSTRAINT [CK_HrDecisions_Amount] CHECK ([Amount] >= 0);
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_HrDecisions_Amount.';
END

-- 3.6. Tài sản doanh nghiệp: Giá mua không âm & Ngày trả >= Ngày giao
IF OBJECT_ID(N'dbo.CK_CompanyAssets_Price', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[CompanyAssets] WITH CHECK
        ADD CONSTRAINT [CK_CompanyAssets_Price] CHECK ([PurchasePrice] >= 0);
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_CompanyAssets_Price.';
END

IF OBJECT_ID(N'dbo.CK_EmployeeAssetAssignments_Dates', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[EmployeeAssetAssignments] WITH CHECK
        ADD CONSTRAINT [CK_EmployeeAssetAssignments_Dates] CHECK (
            [ReturnedDate] IS NULL OR [ReturnedDate] >= [AssignedDate]
        );
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_EmployeeAssetAssignments_Dates.';
END

-- 3.7. Onboarding Checklist: Ngày mục tiêu >= Ngày bắt đầu
IF OBJECT_ID(N'dbo.CK_OnboardingChecklists_Dates', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[OnboardingChecklists] WITH CHECK
        ADD CONSTRAINT [CK_OnboardingChecklists_Dates] CHECK (
            [TargetCompletionDate] IS NULL OR [TargetCompletionDate] >= [StartDate]
        );
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_OnboardingChecklists_Dates.';
END

-- 3.8. Thang điểm đánh giá tuyển dụng & KPI (0 - 100)
IF OBJECT_ID(N'dbo.CK_InterviewSchedules_Score', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[InterviewSchedules] WITH CHECK
        ADD CONSTRAINT [CK_InterviewSchedules_Score] CHECK (
            [Score] IS NULL OR ([Score] >= 0 AND [Score] <= 100)
        );
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_InterviewSchedules_Score.';
END

IF OBJECT_ID(N'dbo.CK_Candidates_EvaluationScore', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[Candidates] WITH CHECK
        ADD CONSTRAINT [CK_Candidates_EvaluationScore] CHECK (
            [EvaluationScore] IS NULL OR ([EvaluationScore] >= 0 AND [EvaluationScore] <= 100)
        );
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_Candidates_EvaluationScore.';
END

IF OBJECT_ID(N'dbo.CK_KpiReviews_TotalScore', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[KpiReviews] WITH CHECK
        ADD CONSTRAINT [CK_KpiReviews_TotalScore] CHECK (
            [TotalScore] >= 0 AND [TotalScore] <= 100
        );
    PRINT N'[OK] Đã tạo CHECK CONSTRAINT CK_KpiReviews_TotalScore.';
END
GO

-- ==========================================================================================
-- PHẦN 4: THIẾT LẬP BỘ NON-CLUSTERED & COVERING INDEXES TỐI ƯU TRUY VẤN NÓNG
-- ==========================================================================================

-- 4.1. Chấm công hàng ngày: (EmployeeId, WorkDate)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceDailies_EmployeeId_WorkDate' AND object_id = OBJECT_ID(N'dbo.AttendanceDailies'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_AttendanceDailies_EmployeeId_WorkDate]
        ON [dbo].[AttendanceDailies] ([EmployeeId] ASC, [WorkDate] ASC);
    PRINT N'[OK] Đã tạo chỉ mục duy nhất IX_AttendanceDailies_EmployeeId_WorkDate.';
END

-- 4.2. Đơn từ nghỉ phép: (Status, FromDate, ToDate)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LeaveRequests_Status_FromDate_ToDate' AND object_id = OBJECT_ID(N'dbo.LeaveRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LeaveRequests_Status_FromDate_ToDate]
        ON [dbo].[LeaveRequests] ([Status] ASC, [FromDate] ASC, [ToDate] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_LeaveRequests_Status_FromDate_ToDate.';
END

-- 4.3. Nhân viên: (DepartmentId, Status, IsDeleted)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employees_DepartmentId_Status_IsDeleted' AND object_id = OBJECT_ID(N'dbo.Employees'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Employees_DepartmentId_Status_IsDeleted]
        ON [dbo].[Employees] ([DepartmentId] ASC, [Status] ASC, [IsDeleted] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_Employees_DepartmentId_Status_IsDeleted.';
END

-- 4.4. Ứng viên: (JobPostingId, Status)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Candidates_JobPostingId_Status' AND object_id = OBJECT_ID(N'dbo.Candidates'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Candidates_JobPostingId_Status]
        ON [dbo].[Candidates] ([JobPostingId] ASC, [Status] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_Candidates_JobPostingId_Status.';
END

-- 4.5. Tin tuyển dụng: Slug duy nhất & Bài viết CMS: Slug
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobPostings_Slug' AND object_id = OBJECT_ID(N'dbo.JobPostings'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_JobPostings_Slug]
        ON [dbo].[JobPostings] ([Slug] ASC);
    PRINT N'[OK] Đã tạo chỉ mục duy nhất IX_JobPostings_Slug.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Contents_Slug' AND object_id = OBJECT_ID(N'dbo.Contents'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Contents_Slug]
        ON [dbo].[Contents] ([Slug] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_Contents_Slug.';
END

-- 4.6. Quỹ phép năm: (EmployeeId, Year, LeaveTypeId) duy nhất
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmployeeLeaveBalances_EmployeeId_Year_LeaveTypeId' AND object_id = OBJECT_ID(N'dbo.EmployeeLeaveBalances'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_EmployeeLeaveBalances_EmployeeId_Year_LeaveTypeId]
        ON [dbo].[EmployeeLeaveBalances] ([EmployeeId] ASC, [Year] ASC, [LeaveTypeId] ASC);
    PRINT N'[OK] Đã tạo chỉ mục duy nhất IX_EmployeeLeaveBalances_EmployeeId_Year_LeaveTypeId.';
END

-- 4.7. Khóa chấm công: (Year, Month, BranchId, DepartmentId)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceLocks_Year_Month_BranchId_DepartmentId' AND object_id = OBJECT_ID(N'dbo.AttendanceLocks'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AttendanceLocks_Year_Month_BranchId_DepartmentId]
        ON [dbo].[AttendanceLocks] ([Year] ASC, [Month] ASC, [BranchId] ASC, [DepartmentId] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_AttendanceLocks_Year_Month_BranchId_DepartmentId.';
END

-- 4.8. Đơn giải trình chấm công: RequestCode duy nhất, (EmployeeId, WorkDate), (Status, WorkDate)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceAdjustmentRequests_RequestCode' AND object_id = OBJECT_ID(N'dbo.AttendanceAdjustmentRequests'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_AttendanceAdjustmentRequests_RequestCode]
        ON [dbo].[AttendanceAdjustmentRequests] ([RequestCode] ASC);
    PRINT N'[OK] Đã tạo chỉ mục duy nhất IX_AttendanceAdjustmentRequests_RequestCode.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceAdjustmentRequests_EmployeeId_WorkDate' AND object_id = OBJECT_ID(N'dbo.AttendanceAdjustmentRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AttendanceAdjustmentRequests_EmployeeId_WorkDate]
        ON [dbo].[AttendanceAdjustmentRequests] ([EmployeeId] ASC, [WorkDate] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_AttendanceAdjustmentRequests_EmployeeId_WorkDate.';
END

-- 4.9. Quyết định nhân sự: DecisionNumber duy nhất, (EmployeeId, EffectiveDate), (DecisionType, EffectiveDate)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HrDecisions_DecisionNumber' AND object_id = OBJECT_ID(N'dbo.HrDecisions'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_HrDecisions_DecisionNumber]
        ON [dbo].[HrDecisions] ([DecisionNumber] ASC);
    PRINT N'[OK] Đã tạo chỉ mục duy nhất IX_HrDecisions_DecisionNumber.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HrDecisions_EmployeeId_EffectiveDate' AND object_id = OBJECT_ID(N'dbo.HrDecisions'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HrDecisions_EmployeeId_EffectiveDate]
        ON [dbo].[HrDecisions] ([EmployeeId] ASC, [EffectiveDate] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_HrDecisions_EmployeeId_EffectiveDate.';
END

-- 4.10. Tài sản doanh nghiệp: AssetCode duy nhất, cấp phát (EmployeeId, Status), (AssetId, Status)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyAssets_AssetCode' AND object_id = OBJECT_ID(N'dbo.CompanyAssets'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_CompanyAssets_AssetCode]
        ON [dbo].[CompanyAssets] ([AssetCode] ASC);
    PRINT N'[OK] Đã tạo chỉ mục duy nhất IX_CompanyAssets_AssetCode.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmployeeAssetAssignments_EmployeeId_Status' AND object_id = OBJECT_ID(N'dbo.EmployeeAssetAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmployeeAssetAssignments_EmployeeId_Status]
        ON [dbo].[EmployeeAssetAssignments] ([EmployeeId] ASC, [Status] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_EmployeeAssetAssignments_EmployeeId_Status.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmployeeAssetAssignments_AssetId_Status' AND object_id = OBJECT_ID(N'dbo.EmployeeAssetAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmployeeAssetAssignments_AssetId_Status]
        ON [dbo].[EmployeeAssetAssignments] ([AssetId] ASC, [Status] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_EmployeeAssetAssignments_AssetId_Status.';
END

-- 4.11. Onboarding: (EmployeeId, Type), Tasks: (ChecklistId, OrderIndex), (DepartmentRole, IsCompleted)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OnboardingChecklists_EmployeeId_Type' AND object_id = OBJECT_ID(N'dbo.OnboardingChecklists'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OnboardingChecklists_EmployeeId_Type]
        ON [dbo].[OnboardingChecklists] ([EmployeeId] ASC, [Type] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_OnboardingChecklists_EmployeeId_Type.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OnboardingTasks_ChecklistId_OrderIndex' AND object_id = OBJECT_ID(N'dbo.OnboardingTasks'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OnboardingTasks_ChecklistId_OrderIndex]
        ON [dbo].[OnboardingTasks] ([ChecklistId] ASC, [OrderIndex] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_OnboardingTasks_ChecklistId_OrderIndex.';
END

-- 4.12. Lịch phỏng vấn: (CandidateId, RoundNumber), (ScheduledTime, Status)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InterviewSchedules_CandidateId_RoundNumber' AND object_id = OBJECT_ID(N'dbo.InterviewSchedules'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_InterviewSchedules_CandidateId_RoundNumber]
        ON [dbo].[InterviewSchedules] ([CandidateId] ASC, [RoundNumber] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_InterviewSchedules_CandidateId_RoundNumber.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InterviewSchedules_ScheduledTime_Status' AND object_id = OBJECT_ID(N'dbo.InterviewSchedules'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_InterviewSchedules_ScheduledTime_Status]
        ON [dbo].[InterviewSchedules] ([ScheduledTime] ASC, [Status] ASC);
    PRINT N'[OK] Đã tạo chỉ mục IX_InterviewSchedules_ScheduledTime_Status.';
END
GO

-- ==========================================================================================
-- PHẦN 5: THÊM EXTENDED PROPERTIES MÔ TẢ TIẾNG VIỆT CHO SSMS DATABASE DIAGRAM
-- ==========================================================================================
CREATE OR ALTER PROCEDURE dbo.sp_SetTableDescription
    @TableName NVARCHAR(128),
    @Description NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM fn_listextendedproperty(
            N'MS_Description',
            N'SCHEMA', N'dbo',
            N'TABLE', @TableName,
            NULL, NULL)
    )
    BEGIN
        EXEC sys.sp_updateextendedproperty
            @name = N'MS_Description',
            @value = @Description,
            @level0type = N'SCHEMA', @level0name = N'dbo',
            @level1type = N'TABLE',  @level1name = @TableName;
    END
    ELSE
    BEGIN
        EXEC sys.sp_addextendedproperty
            @name = N'MS_Description',
            @value = @Description,
            @level0type = N'SCHEMA', @level0name = N'dbo',
            @level1type = N'TABLE',  @level1name = @TableName;
    END
END
GO

EXEC dbo.sp_SetTableDescription N'EmployeeLeaveBalances', N'Quỹ phép năm nhân viên (Standard 12 ngày + Thâm niên + Tồn năm trước - Đã dùng)';
EXEC dbo.sp_SetTableDescription N'AttendanceLocks', N'Khóa kỳ công theo tháng/chi nhánh trước khi chốt bảng lương';
EXEC dbo.sp_SetTableDescription N'AttendanceAdjustmentRequests', N'Đơn giải trình chấm công / bổ sung giờ vào-ra kèm minh chứng';
EXEC dbo.sp_SetTableDescription N'HrDecisions', N'Quyết định Nhân sự điện tử: Bổ nhiệm, Điều chuyển, Tăng lương, Khen thưởng, Kỷ luật, Thôi việc';
EXEC dbo.sp_SetTableDescription N'CompanyAssets', N'Danh mục tài sản doanh nghiệp: Máy tính, Màn hình, Thẻ từ, Đồng phục, Xe cộ';
EXEC dbo.sp_SetTableDescription N'EmployeeAssetAssignments', N'Biên bản bàn giao và thu hồi tài sản cấp phát cho nhân sự';
EXEC dbo.sp_SetTableDescription N'OnboardingChecklists', N'Quy trình hội nhập nhân viên mới và bàn giao thôi việc Offboarding';
EXEC dbo.sp_SetTableDescription N'OnboardingTasks', N'Chi tiết đầu việc quy trình Onboarding/Offboarding theo vai trò HR, IT, Quản lý, Nhân viên';
EXEC dbo.sp_SetTableDescription N'InterviewSchedules', N'Lịch phỏng vấn ứng viên theo từng vòng, người phỏng vấn, điểm số và kết quả';

DROP PROCEDURE dbo.sp_SetTableDescription;
GO

PRINT N'=== [V6_01] Hoàn tất 100% Schema, Constraints, Indexes & Extended Properties Enterprise HRM ===';
GO

