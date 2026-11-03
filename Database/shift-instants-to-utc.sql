-- =====================================================================
-- Chuyển các cột thời điểm từ giờ Việt Nam (UTC+7) sang UTC: trừ 7 giờ.
-- Chỉ áp dụng cho cột thời điểm (xem AppDbContext.IsInstantProperty).
-- Không đổi cột ngày lịch (FromDate, WorkDate, DueDate, OrderDate...) và cột ...Utc.
--
-- CHẠY ĐÚNG MỘT LẦN, đồng thời với việc triển khai mã có VietnamTimeConverter.
-- Script có bảng nhật ký dbo.__DataMigrationLog để không trừ hai lần nếu chạy lại.
-- Sao lưu database trước khi chạy. Thử trên ERP_HRM_CMS_DEV trước.
-- =====================================================================
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.__DataMigrationLog') IS NULL
    CREATE TABLE dbo.__DataMigrationLog (
        Name nvarchar(200) NOT NULL PRIMARY KEY,
        AppliedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

IF EXISTS (SELECT 1 FROM dbo.__DataMigrationLog WHERE Name = N'shift-instants-to-utc-v1')
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'Đã chạy trước đó (shift-instants-to-utc-v1): bỏ qua.';
    RETURN;
END

UPDATE [Users] SET [LastLoginAt] = DATEADD(HOUR, -7, [LastLoginAt]) WHERE [LastLoginAt] IS NOT NULL;
UPDATE [Users] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Roles] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [UserRoles] SET [AssignedAt] = DATEADD(HOUR, -7, [AssignedAt]) WHERE [AssignedAt] IS NOT NULL;
UPDATE [UserPermissions] SET [AssignedAt] = DATEADD(HOUR, -7, [AssignedAt]) WHERE [AssignedAt] IS NOT NULL;
UPDATE [SiteLayouts] SET [UpdatedAt] = DATEADD(HOUR, -7, [UpdatedAt]) WHERE [UpdatedAt] IS NOT NULL;
UPDATE [Menus] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Contents] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Contents] SET [UpdatedAt] = DATEADD(HOUR, -7, [UpdatedAt]) WHERE [UpdatedAt] IS NOT NULL;
UPDATE [Banners] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [SiteSettings] SET [UpdatedAt] = DATEADD(HOUR, -7, [UpdatedAt]) WHERE [UpdatedAt] IS NOT NULL;
UPDATE [AuditLogs] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Notifications] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [NotificationReads] SET [ReadAt] = DATEADD(HOUR, -7, [ReadAt]) WHERE [ReadAt] IS NOT NULL;
UPDATE [LeaveRequests] SET [DepartmentHeadApprovedAt] = DATEADD(HOUR, -7, [DepartmentHeadApprovedAt]) WHERE [DepartmentHeadApprovedAt] IS NOT NULL;
UPDATE [LeaveRequests] SET [DirectorApprovedAt] = DATEADD(HOUR, -7, [DirectorApprovedAt]) WHERE [DirectorApprovedAt] IS NOT NULL;
UPDATE [LeaveRequests] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [PayrollPeriods] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [PayrollPeriods] SET [LockedAt] = DATEADD(HOUR, -7, [LockedAt]) WHERE [LockedAt] IS NOT NULL;
UPDATE [SalarySlips] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Companies] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Branches] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Departments] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Positions] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Employees] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Contracts] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [AttendanceDailies] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Categories] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Products] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Inventories] SET [UpdatedAt] = DATEADD(HOUR, -7, [UpdatedAt]) WHERE [UpdatedAt] IS NOT NULL;
UPDATE [InventoryTransactions] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [Orders] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [CartItems] SET [AddedAt] = DATEADD(HOUR, -7, [AddedAt]) WHERE [AddedAt] IS NOT NULL;
UPDATE [Suppliers] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [PurchaseOrders] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [PurchaseOrders] SET [ReceivedDate] = DATEADD(HOUR, -7, [ReceivedDate]) WHERE [ReceivedDate] IS NOT NULL;
UPDATE [DebtRecords] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;
UPDATE [DebtPayments] SET [PaymentDate] = DATEADD(HOUR, -7, [PaymentDate]) WHERE [PaymentDate] IS NOT NULL;
UPDATE [StocktakingSheets] SET [CheckDate] = DATEADD(HOUR, -7, [CheckDate]) WHERE [CheckDate] IS NOT NULL;
UPDATE [StocktakingSheets] SET [CreatedAt] = DATEADD(HOUR, -7, [CreatedAt]) WHERE [CreatedAt] IS NOT NULL;

INSERT INTO dbo.__DataMigrationLog (Name) VALUES (N'shift-instants-to-utc-v1');
COMMIT TRANSACTION;
PRINT N'Đã chuyển 40 cột thời điểm sang UTC.';
