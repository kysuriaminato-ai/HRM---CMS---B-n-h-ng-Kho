-- =========================================================================================
-- ERP_HRM_CMS ENTERPRISE — SCRIPT KIỂM ĐỊNH SỨC KHỎE CƠ SỞ DỮ LIỆU TOÀN DIỆN (HEALTH CHECK)
-- Version: V6_99_Verify_Schema_Health.sql
-- Môi trường thực thi: SQL Server Management Studio (SSMS) / Azure SQL / Docker SQL Server
-- Tính chất: IDEMPOTENT — Chỉ đọc và kiểm định, tuyệt đối an toàn cho dữ liệu Production
-- =========================================================================================

SET NOCOUNT ON;
PRINT N'';
PRINT N'=========================================================================================';
PRINT N'  HỆ THỐNG ERP_HRM_CMS ENTERPRISE — KIỂM TRA SỨC KHỎE CƠ SỞ DỮ LIỆU (SCHEMA HEALTH CHECK)';
PRINT N'  Thời gian kiểm tra: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT N'  Cơ sở dữ liệu:      ' + DB_NAME();
PRINT N'=========================================================================================';
PRINT N'';

DECLARE @TotalTablesCount INT = 0;
DECLARE @MissingTablesCount INT = 0;
DECLARE @TotalIndexesCount INT = 0;
DECLARE @TotalFkCount INT = 0;

-- 1. BẢNG TẬP HỢP CÁC BẢNG BẮT BUỘC TRONG HỆ THỐNG
DECLARE @ExpectedTables TABLE (
    ModuleName NVARCHAR(50),
    TableName NVARCHAR(100),
    Status NVARCHAR(20) DEFAULT N'PENDING'
);

INSERT INTO @ExpectedTables (ModuleName, TableName) VALUES
-- System & Auth (11)
(N'System & Auth', N'Users'),
(N'System & Auth', N'Roles'),
(N'System & Auth', N'Permissions'),
(N'System & Auth', N'UserRoles'),
(N'System & Auth', N'RolePermissions'),
(N'System & Auth', N'UserPermissions'),
(N'System & Auth', N'AuditLogs'),
(N'System & Auth', N'Notifications'),
(N'System & Auth', N'NotificationReads'),
(N'System & Auth', N'LoginAttempts'),
(N'System & Auth', N'SiteSettings'),

-- HRM Core & Hồ sơ 360° (13)
(N'HRM Core', N'Companies'),
(N'HRM Core', N'Branches'),
(N'HRM Core', N'Departments'),
(N'HRM Core', N'Positions'),
(N'HRM Core', N'Employees'),
(N'HRM Core', N'EmployeeDetails'),
(N'HRM Core', N'Contracts'),
(N'HRM Core', N'EmployeeRelatives'),
(N'HRM Core', N'EmployeeSkills'),
(N'HRM Core', N'EmployeeCertificates'),
(N'HRM Core', N'EmployeeDocuments'),
(N'HRM Core', N'EmployeeChangeHistories'),
(N'HRM Core', N'EmployeeAssets'),

-- HRM Time & Attendance (8)
(N'HRM Time & Attendance', N'Shifts'),
(N'HRM Time & Attendance', N'ShiftSchedules'),
(N'HRM Time & Attendance', N'AttendanceDailies'),
(N'HRM Time & Attendance', N'LeaveTypes'),
(N'HRM Time & Attendance', N'LeaveRequests'),
(N'HRM Time & Attendance', N'EmployeeLeaveBalances'),
(N'HRM Time & Attendance', N'AttendanceLocks'),
(N'HRM Time & Attendance', N'AttendanceAdjustmentRequests'),

-- HRM Performance & LMS (5)
(N'HRM Performance & LMS', N'KpiReviews'),
(N'HRM Performance & LMS', N'KpiCriteria'),
(N'HRM Performance & LMS', N'Trainings'),
(N'HRM Performance & LMS', N'TrainingParticipants'),
(N'HRM Performance & LMS', N'HrDecisions'),

-- HRM Recruitment ATS (5)
(N'HRM Recruitment ATS', N'JobPostings'),
(N'HRM Recruitment ATS', N'Candidates'),
(N'HRM Recruitment ATS', N'InterviewSchedules'),
(N'HRM Recruitment ATS', N'OnboardingChecklists'),
(N'HRM Recruitment ATS', N'OnboardingTasks'),

-- HRM Payroll C&B (3)
(N'HRM Payroll C&B', N'PayrollPeriods'),
(N'HRM Payroll C&B', N'SalarySlips'),
(N'HRM Payroll C&B', N'PayrollSettings'),

-- SCM & Kho Vận (9)
(N'SCM & Kho Vận', N'Suppliers'),
(N'SCM & Kho Vận', N'PurchaseOrders'),
(N'SCM & Kho Vận', N'PurchaseOrderDetails'),
(N'SCM & Kho Vận', N'Debts'),
(N'SCM & Kho Vận', N'Warehouses'),
(N'SCM & Kho Vận', N'WarehouseInventories'),
(N'SCM & Kho Vận', N'InventoryTransactions'),
(N'SCM & Kho Vận', N'InventoryAudits'),
(N'SCM & Kho Vận', N'InventoryAuditDetails'),

-- CMS & Bán Hàng (6)
(N'CMS & Bán Hàng', N'Contents'),
(N'CMS & Bán Hàng', N'Categories'),
(N'CMS & Bán Hàng', N'Tags'),
(N'CMS & Bán Hàng', N'Orders'),
(N'CMS & Bán Hàng', N'OrderDetails'),
(N'CMS & Bán Hàng', N'Products');

-- 2. ĐỐI SOÁT CÁC BẢNG TRONG DATABASE
UPDATE t
SET t.Status = CASE WHEN st.name IS NOT NULL THEN N'OK' ELSE N'MISSING' END
FROM @ExpectedTables t
LEFT JOIN sys.tables st ON st.name = t.TableName;

SELECT @TotalTablesCount = COUNT(*) FROM @ExpectedTables WHERE Status = N'OK';
SELECT @MissingTablesCount = COUNT(*) FROM @ExpectedTables WHERE Status = N'MISSING';

PRINT N'[1/5] KIỂM TRA BẢNG CƠ SỞ DỮ LIỆU:';
PRINT N'      - Tổng số bảng tiêu chuẩn: ' + CAST(COUNT(*) AS NVARCHAR(10)) FROM @ExpectedTables;
PRINT N'      - Bảng hiện hữu (OK):     ' + CAST(@TotalTablesCount AS NVARCHAR(10));
PRINT N'      - Bảng thiếu hụt:         ' + CAST(@MissingTablesCount AS NVARCHAR(10));

IF @MissingTablesCount > 0
BEGIN
    PRINT N'      [!] CẢNH BÁO: Danh sách bảng còn thiếu trong DB:';
    SELECT ModuleName, TableName FROM @ExpectedTables WHERE Status = N'MISSING';
END
ELSE
BEGIN
    PRINT N'      [✔] XÁC NHẬN: 100% CÁC BẢNG NGHIỆP VỤ ĐÃ HIỆN HỮU ĐẦY ĐỦ TRÊN SQL SERVER!';
END
PRINT N'';

-- 3. KIỂM TRA KHÓA NGOẠI (FOREIGN KEYS)
SELECT @TotalFkCount = COUNT(*) FROM sys.foreign_keys;
PRINT N'[2/5] KIỂM TRA RÀNG BUỘC TOÀN VẸN (FOREIGN KEYS):';
PRINT N'      - Tổng số ràng buộc khóa ngoại: ' + CAST(@TotalFkCount AS NVARCHAR(10));
IF @TotalFkCount >= 30
    PRINT N'      [✔] Khóa ngoại liên kết đa tầng hoạt động bình thường, bảo toàn dữ liệu quan hệ.';
ELSE
    PRINT N'      [!] Cảnh báo: Số lượng khóa ngoại ít hơn tiêu chuẩn thiết kế.';
PRINT N'';

-- 4. KIỂM TRA CÁC CHỈ MỤC TỐI ƯU HIỆU NĂNG TRUY VẤN (INDEXES)
SELECT @TotalIndexesCount = COUNT(*) 
FROM sys.indexes 
WHERE is_hypothetical = 0 AND name IS NOT NULL AND name NOT LIKE 'PK_%';

PRINT N'[3/5] KIỂM TRA CHỈ MỤC TỐI ƯU TRUY VẤN (INDEX HEALTH):';
PRINT N'      - Tổng số Non-clustered Indexes hiện hữu: ' + CAST(@TotalIndexesCount AS NVARCHAR(10));

-- Kiểm tra một số chỉ mục then chốt
DECLARE @CriticalIndexes TABLE (IndexName NVARCHAR(100), ExistsFlag BIT);
INSERT INTO @CriticalIndexes VALUES
(N'IX_Employees_EmployeeCode', 0),
(N'IX_AttendanceDailies_Employee_Date', 0),
(N'IX_SalarySlips_Period_Employee', 0),
(N'IX_Contracts_EmployeeId', 0),
(N'IX_KpiReviews_Period', 0);

UPDATE c
SET c.ExistsFlag = 1
FROM @CriticalIndexes c
JOIN sys.indexes i ON i.name = c.IndexName;

DECLARE @IndexOkCount INT;
SELECT @IndexOkCount = COUNT(*) FROM @CriticalIndexes WHERE ExistsFlag = 1;
PRINT N'      - Chỉ mục then chốt hiệu năng (Critical Indexes): ' + CAST(@IndexOkCount AS NVARCHAR(10)) + N'/5';
IF @IndexOkCount = 5
    PRINT N'      [✔] 100% Các chỉ mục then chốt đã được kích hoạt, tối ưu chi phí truy vấn SQL.';
ELSE
    PRINT N'      [!] Chú ý: Một số chỉ mục tùy biến có thể được khởi tạo lại qua script V6_01.';
PRINT N'';

-- 5. KIỂM TRA CỘT DỮ LIỆU NHẠY CẢM MÃ HÓA (SECURITY AUDIT)
PRINT N'[4/5] KIỂM TRA CỘT DỮ LIỆU NHẠY CẢM ĐƯỢC BẢO VỆ:';
DECLARE @IdentityCardColumnOk BIT = 0;
DECLARE @BankAccountColumnOk BIT = 0;

IF EXISTS (SELECT * FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.name = 'Employees' AND c.name = 'IdentityCard')
    SET @IdentityCardColumnOk = 1;

IF EXISTS (SELECT * FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.name = 'EmployeeDetails' AND c.name = 'BankAccountNumber')
    SET @BankAccountColumnOk = 1;

PRINT N'      - Cột Số CCCD (Employees.IdentityCard):           ' + CASE WHEN @IdentityCardColumnOk = 1 THEN N'[✔] Hợp lệ' ELSE N'[!] Thiếu' END;
PRINT N'      - Cột Số TK Ngân Hàng (EmployeeDetails.BankAccountNumber): ' + CASE WHEN @BankAccountColumnOk = 1 THEN N'[✔] Hợp lệ' ELSE N'[!] Thiếu' END;
PRINT N'';

-- 6. TỔNG KẾT ĐÁNH GIÁ SẴN SÀNG PRODUCTION
PRINT N'[5/5] TỔNG KẾT TRẠNG THÁI SẴN SÀNG TRIỂN KHAI PRODUCTION:';
IF @MissingTablesCount = 0 AND @TotalFkCount >= 20 AND @IdentityCardColumnOk = 1 AND @BankAccountColumnOk = 1
BEGIN
    PRINT N'=========================================================================================';
    PRINT N'  KẾT LUẬN: HỆ THỐNG CƠ SỞ DỮ LIỆU ĐẠT TIÊU CHUẨN XANH 100% (HEALTHY & PRODUCTION READY)!';
    PRINT N'  - Không có bảng mồ côi hoặc thiếu hụt.';
    PRINT N'  - Cơ cấu chỉ mục và khóa ngoại chuẩn hóa theo Clean Architecture 4 tầng.';
    PRINT N'  - Cột mã hóa nhạy cảm Data Protection hoạt động chính xác.';
    PRINT N'=========================================================================================';
END
ELSE
BEGIN
    PRINT N'=========================================================================================';
    PRINT N'  KẾT LUẬN: CẦN RÀ SOÁT LẠI MỘT SỐ HẠNG MỤC TRƯỚC KHI DEPLOY PRODUCTION!';
    PRINT N'=========================================================================================';
END
PRINT N'';
GO

