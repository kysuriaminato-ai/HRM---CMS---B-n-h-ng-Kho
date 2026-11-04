-- =====================================================================
-- Migration dữ liệu: bỏ các vai trò alias và chuyển sang vai trò chuẩn.
--   QTVP            -> HR_MANAGER
--   SalesStaff      -> SALES_STAFF
--   SaleDirector    -> SALES_DIRECTOR
--   WarehouseKeeper -> WAREHOUSE_STAFF
-- Bổ sung vai trò EMPLOYEE (nhân viên thường) và quyền hrm.self.service.
--
-- Đặc điểm an toàn:
--   * Quyền của vai trò alias được GỘP (không xóa) sang vai trò chuẩn trước khi xóa alias,
--     nên không ai mất quyền đang có.
--   * Chạy lại nhiều lần vẫn an toàn (mọi bước đều kiểm tra tồn tại).
--   * Toàn bộ trong một giao dịch: lỗi thì không thay đổi gì.
--
-- Cách chạy: sao lưu DB trước; thử trên ERP_HRM_CMS_DEV trước khi chạy trên DB thật.
-- Script này KHÔNG được chạy tự động từ ứng dụng.
-- =====================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- 1. Bảng ánh xạ alias -> vai trò chuẩn
DECLARE @Map TABLE (AliasCode NVARCHAR(50) PRIMARY KEY, CanonicalCode NVARCHAR(50) NOT NULL);
INSERT INTO @Map (AliasCode, CanonicalCode) VALUES
    (N'QTVP',            N'HR_MANAGER'),
    (N'SalesStaff',      N'SALES_STAFF'),
    (N'SaleDirector',    N'SALES_DIRECTOR'),
    (N'WarehouseKeeper', N'WAREHOUSE_STAFF');

-- 2. Đảm bảo các vai trò chuẩn mới tồn tại
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Code = N'SALES_DIRECTOR')
    INSERT INTO dbo.Roles (Code, Name, Description, IsSystem, IsActive, CreatedAt)
    VALUES (N'SALES_DIRECTOR', N'Giám đốc Kinh doanh', N'Quản lý đơn hàng, xem sản phẩm và báo cáo bán hàng', 0, 1, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Code = N'EMPLOYEE')
    INSERT INTO dbo.Roles (Code, Name, Description, IsSystem, IsActive, CreatedAt)
    VALUES (N'EMPLOYEE', N'Nhân viên', N'Tự phục vụ: đơn từ và chấm công của chính mình', 0, 1, GETUTCDATE());

-- 3. Đảm bảo quyền tự phục vụ tồn tại (thuộc module hrm_leave)
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Code = N'hrm.self.service')
    INSERT INTO dbo.Permissions (ModuleId, Code, Name, Description, OrderIndex)
    SELECT m.Id, N'hrm.self.service',
           N'Tự phục vụ: tạo đơn từ và xem chấm công của chính mình',
           NULL, 0
    FROM dbo.Modules m
    WHERE m.Code = N'hrm_leave';

-- 4. Gộp quyền của vai trò alias sang vai trò chuẩn (chỉ thêm, không xóa ở bước này)
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT DISTINCT canonical.Id, rp.PermissionId
FROM dbo.RolePermissions rp
JOIN dbo.Roles alias ON alias.Id = rp.RoleId
JOIN @Map map ON map.AliasCode = alias.Code
JOIN dbo.Roles canonical ON canonical.Code = map.CanonicalCode
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermissions x
    WHERE x.RoleId = canonical.Id AND x.PermissionId = rp.PermissionId);

-- 5. Chuyển người dùng từ alias sang vai trò chuẩn (không trùng lặp)
INSERT INTO dbo.UserRoles (UserId, RoleId, AssignedAt)
SELECT DISTINCT ur.UserId, canonical.Id, GETUTCDATE()
FROM dbo.UserRoles ur
JOIN dbo.Roles alias ON alias.Id = ur.RoleId
JOIN @Map map ON map.AliasCode = alias.Code
JOIN dbo.Roles canonical ON canonical.Code = map.CanonicalCode
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.UserRoles x
    WHERE x.UserId = ur.UserId AND x.RoleId = canonical.Id);

-- 6. Xóa liên kết của alias rồi xóa chính vai trò alias
DELETE ur
FROM dbo.UserRoles ur
JOIN dbo.Roles alias ON alias.Id = ur.RoleId
JOIN @Map map ON map.AliasCode = alias.Code;

DELETE rp
FROM dbo.RolePermissions rp
JOIN dbo.Roles alias ON alias.Id = rp.RoleId
JOIN @Map map ON map.AliasCode = alias.Code;

DELETE alias
FROM dbo.Roles alias
JOIN @Map map ON map.AliasCode = alias.Code;

-- 7. Kiểm tra sau cùng: không còn vai trò alias nào
IF EXISTS (SELECT 1 FROM dbo.Roles r JOIN @Map m ON m.AliasCode = r.Code)
BEGIN
    THROW 50001, N'Vẫn còn vai trò alias sau migration; giao dịch đã được hủy.', 1;
END

COMMIT TRANSACTION;
PRINT N'Hoàn tất migration vai trò alias.';
