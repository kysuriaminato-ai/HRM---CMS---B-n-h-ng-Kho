-- =====================================================================
-- Script SQL cũ (idempotent) trích từ DbInitializer trước khi chuyển sang EF Core Migrations.
-- Chạy lại an toàn: mọi lệnh đều có kiểm tra tồn tại. Nội dung đã được đưa vào migration LegacySchemaFixes.
-- =====================================================================

-- Bảng nghiệp vụ và nhật ký (AuditLogs, Notifications, Suppliers, PO, Công nợ, Kiểm kê, Đơn từ, Lương)
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
            
GO

-- Bảng trạng thái đã đọc thông báo (NotificationReads)
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
            
GO

-- Bảng bộ đếm đăng nhập sai (LoginAttempts)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoginAttempts')
                BEGIN
                    CREATE TABLE [LoginAttempts] (
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Username] nvarchar(100) NOT NULL,
                        [IpAddress] nvarchar(45) NOT NULL,
                        [FailedCount] int NOT NULL DEFAULT 0,
                        [LockoutLevel] int NOT NULL DEFAULT 0,
                        [LockedUntilUtc] datetime2 NULL,
                        [LastFailedUtc] datetime2 NOT NULL DEFAULT GETUTCDATE()
                    );
                    CREATE UNIQUE INDEX [IX_LoginAttempts_Username_IpAddress] ON [LoginAttempts] ([Username], [IpAddress]);
                END;
            
GO

-- Nới cột mã hóa (Employees.IdentityCard, EmployeeDetails.BankAccountNumber lên nvarchar(512))
                IF COL_LENGTH('Employees', 'IdentityCard') IS NOT NULL AND COL_LENGTH('Employees', 'IdentityCard') < 1024
                    ALTER TABLE [Employees] ALTER COLUMN [IdentityCard] nvarchar(512) NULL;
                IF COL_LENGTH('EmployeeDetails', 'BankAccountNumber') IS NOT NULL AND COL_LENGTH('EmployeeDetails', 'BankAccountNumber') < 1024
                    ALTER TABLE [EmployeeDetails] ALTER COLUMN [BankAccountNumber] nvarchar(512) NULL;
            
GO
