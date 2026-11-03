USE master;
GO

-- 1. KHỞI TẠO CƠ SỞ DỮ LIỆU
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ERP_HRM_CMS_DB')
BEGIN
CREATE DATABASE ERP_HRM_CMS_DB
COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

USE ERP_HRM_CMS_DB;
GO

-- Tắt kiểm tra khóa ngoại tạm thời nếu cần dọn sạch bảng cũ
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all";
GO

-- PHẦN I: PHÂN HỆ CƠ CẤU TỔ CHỨC & NHÂN SỰ - CHẤM CÔNG (HRM)

-- 1. Bảng Doanh nghiệp / Tổng công ty
IF OBJECT_ID(N'dbo.Companies', N'U') IS NULL
CREATE TABLE dbo.Companies (
Id INT IDENTITY(1,1) PRIMARY KEY,
Code VARCHAR(50) NOT NULL UNIQUE,
Name NVARCHAR(250) NOT NULL,
TaxCode VARCHAR(50) NULL,
Phone VARCHAR(20) NULL,
Email VARCHAR(100) NULL,
Address NVARCHAR(300) NULL,
IsActive BIT NOT NULL DEFAULT(1),
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())
);
GO

-- 2. Bảng Chi nhánh (Branches)
IF OBJECT_ID(N'dbo.Branches', N'U') IS NULL
CREATE TABLE dbo.Branches (
Id INT IDENTITY(1,1) PRIMARY KEY,
CompanyId INT NOT NULL,
Code VARCHAR(50) NOT NULL UNIQUE,
Name NVARCHAR(200) NOT NULL,
Phone VARCHAR(20) NULL,
Address NVARCHAR(300) NULL,
IsActive BIT NOT NULL DEFAULT(1),
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_Branches_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(Id) ON DELETE CASCADE
);
GO

-- 3. Bảng Phòng ban (Departments)
IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
CREATE TABLE dbo.Departments (
Id INT IDENTITY(1,1) PRIMARY KEY,
BranchId INT NOT NULL,
Code VARCHAR(50) NOT NULL UNIQUE,
Name NVARCHAR(200) NOT NULL,
ParentId INT NULL,
IsActive BIT NOT NULL DEFAULT(1),
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_Departments_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id) ON DELETE CASCADE,
CONSTRAINT FK_Departments_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Departments(Id)
);
GO

-- 4. Bảng Chức danh / Vị trí công việc (Positions)
IF OBJECT_ID(N'dbo.Positions', N'U') IS NULL
CREATE TABLE dbo.Positions (
Id INT IDENTITY(1,1) PRIMARY KEY,
Code VARCHAR(50) NOT NULL UNIQUE,
Name NVARCHAR(150) NOT NULL,
BaseSalary DECIMAL(18,2) NOT NULL DEFAULT(0),
AllowanceRate DECIMAL(5,2) NOT NULL DEFAULT(0),
IsActive BIT NOT NULL DEFAULT(1),
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())
);
GO

-- 5. Bảng Hồ sơ Nhân viên (Employees)
IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
CREATE TABLE dbo.Employees (
Id INT IDENTITY(1,1) PRIMARY KEY,
DepartmentId INT NOT NULL,
PositionId INT NOT NULL,
EmployeeCode VARCHAR(50) NOT NULL UNIQUE,
FullName NVARCHAR(150) NOT NULL,
DateOfBirth DATE NULL,
Gender NVARCHAR(10) NULL,
Email VARCHAR(150) NOT NULL UNIQUE,
Phone VARCHAR(20) NULL,
IdentityCard VARCHAR(30) NULL,
AvatarUrl NVARCHAR(500) NULL,
Status NVARCHAR(50) NOT NULL DEFAULT(N'Active'), -- Active, OnLeave, Resigned
HireDate DATE NOT NULL DEFAULT(CAST(GETDATE() AS DATE)),
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id),
CONSTRAINT FK_Employees_Positions FOREIGN KEY (PositionId) REFERENCES dbo.Positions(Id)
);
GO

-- 6. Bảng Chi tiết Nhân viên mở rộng (EmployeeDetails - 1:1)
IF OBJECT_ID(N'dbo.EmployeeDetails', N'U') IS NULL
CREATE TABLE dbo.EmployeeDetails (
EmployeeId INT PRIMARY KEY,
EmergencyContactName NVARCHAR(150) NULL,
EmergencyContactPhone VARCHAR(20) NULL,
BankAccountNumber VARCHAR(50) NULL,
BankName NVARCHAR(150) NULL,
BankBranch NVARCHAR(150) NULL,
CurrentAddress NVARCHAR(300) NULL,
PermanentAddress NVARCHAR(300) NULL,
TaxIdentificationNumber VARCHAR(50) NULL,
SocialInsuranceNumber VARCHAR(50) NULL,
EducationLevel NVARCHAR(100) NULL,
CONSTRAINT FK_EmployeeDetails_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE
);
GO

-- 7. Bảng Hợp đồng Lao động (Contracts)
IF OBJECT_ID(N'dbo.Contracts', N'U') IS NULL
CREATE TABLE dbo.Contracts (
Id INT IDENTITY(1,1) PRIMARY KEY,
EmployeeId INT NOT NULL,
ContractNumber VARCHAR(100) NOT NULL UNIQUE,
ContractType NVARCHAR(100) NOT NULL, -- Thử việc, Xác định thời hạn 1 năm, Vô thời hạn
StartDate DATE NOT NULL,
EndDate DATE NULL,
BasicSalary DECIMAL(18,2) NOT NULL DEFAULT(0),
InsuranceSalary DECIMAL(18,2) NOT NULL DEFAULT(0),
Status NVARCHAR(50) NOT NULL DEFAULT(N'Active'), -- Active, Expired, Terminated
SignDate DATE NOT NULL,
Note NVARCHAR(500) NULL,
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_Contracts_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE
);
GO

-- 8. Bảng Ca làm việc (Shifts)
IF OBJECT_ID(N'dbo.Shifts', N'U') IS NULL
CREATE TABLE dbo.Shifts (
Id INT IDENTITY(1,1) PRIMARY KEY,
Code VARCHAR(50) NOT NULL UNIQUE,
Name NVARCHAR(100) NOT NULL,
StartTime TIME(0) NOT NULL,
EndTime TIME(0) NOT NULL,
BreakStartTime TIME(0) NULL,
BreakEndTime TIME(0) NULL,
WorkHours DECIMAL(4,2) NOT NULL DEFAULT(8.0),
IsActive BIT NOT NULL DEFAULT(1)
);
GO

-- 9. Bảng Chấm công Tổng hợp Hàng ngày (AttendanceDailies)
IF OBJECT_ID(N'dbo.AttendanceDailies', N'U') IS NULL
CREATE TABLE dbo.AttendanceDailies (
Id BIGINT IDENTITY(1,1) PRIMARY KEY,
EmployeeId INT NOT NULL,
ShiftId INT NOT NULL,
WorkDate DATE NOT NULL,
FirstInTime TIME(0) NULL,
LastOutTime TIME(0) NULL,
LateMinutes INT NOT NULL DEFAULT(0),
EarlyMinutes INT NOT NULL DEFAULT(0),
WorkHourCount DECIMAL(4,2) NOT NULL DEFAULT(0),
OvertimeHours DECIMAL(4,2) NOT NULL DEFAULT(0),
AttendanceStatus NVARCHAR(50) NOT NULL DEFAULT(N'Present'), -- Present, Late, Early, Absent, Leave
Note NVARCHAR(250) NULL,
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT UQ_Attendance_Emp_Date UNIQUE (EmployeeId, WorkDate),
CONSTRAINT FK_Attendance_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id),
CONSTRAINT FK_Attendance_Shifts FOREIGN KEY (ShiftId) REFERENCES dbo.Shifts(Id)
);
GO

-- PHẦN II: PHÂN QUYỀN ĐỘNG ĐA CẤP (DYNAMIC RBAC + ACL MATRIX)

-- 10. Bảng Tài khoản người dùng (Users - nối 1-1 với Employees)
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
Id INT IDENTITY(1,1) PRIMARY KEY,
EmployeeId INT NULL UNIQUE,
Username VARCHAR(50) NOT NULL UNIQUE,
PasswordHash VARCHAR(256) NOT NULL, -- SHA-256 băm kèm salt hoặc Hex 64 bytes
PasswordSalt VARCHAR(100) NOT NULL,
IsActive BIT NOT NULL DEFAULT(1),
IsSuperAdmin BIT NOT NULL DEFAULT(0),
LastLoginAt DATETIME2 NULL,
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_Users_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE SET NULL
);
GO

-- 11. Bảng Nhóm quyền / Vai trò (Roles)
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
CREATE TABLE dbo.Roles (
Id INT IDENTITY(1,1) PRIMARY KEY,
Code VARCHAR(50) NOT NULL UNIQUE,
Name NVARCHAR(150) NOT NULL,
Description NVARCHAR(300) NULL,
IsSystem BIT NOT NULL DEFAULT(0),
IsActive BIT NOT NULL DEFAULT(1),
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())
);
GO

-- 12. Bảng Phân hệ chức năng (Modules)
IF OBJECT_ID(N'dbo.Modules', N'U') IS NULL
CREATE TABLE dbo.Modules (
Id INT IDENTITY(1,1) PRIMARY KEY,
Code VARCHAR(50) NOT NULL UNIQUE,
Name NVARCHAR(150) NOT NULL,
Icon VARCHAR(100) NULL,
OrderIndex INT NOT NULL DEFAULT(0),
IsActive BIT NOT NULL DEFAULT(1)
);
GO

-- 13. Bảng Danh mục Quyền chi tiết (Permissions)
IF OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
CREATE TABLE dbo.Permissions (
Id INT IDENTITY(1,1) PRIMARY KEY,
ModuleId INT NOT NULL,
Code VARCHAR(100) NOT NULL UNIQUE, -- Định dạng: module.action (VD: menu.view, menu.create, hrm.manage...)
Name NVARCHAR(200) NOT NULL,
Description NVARCHAR(300) NULL,
OrderIndex INT NOT NULL DEFAULT(0),
CONSTRAINT FK_Permissions_Modules FOREIGN KEY (ModuleId) REFERENCES dbo.Modules(Id) ON DELETE CASCADE
);
GO

-- 14. Bảng Gán Vai trò cho Người dùng (UserRoles)
IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
CREATE TABLE dbo.UserRoles (
UserId INT NOT NULL,
RoleId INT NOT NULL,
AssignedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
PRIMARY KEY (UserId, RoleId),
CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE
);
GO

-- 15. Bảng Phân quyền cho Vai trò (RolePermissions - RBAC)
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
CREATE TABLE dbo.RolePermissions (
RoleId INT NOT NULL,
PermissionId INT NOT NULL,
PRIMARY KEY (RoleId, PermissionId),
CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE,
CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(Id) ON DELETE CASCADE
);
GO

-- 16. Bảng Phân quyền đặc thù cho từng Cá nhân (UserPermissions - ACL Override Matrix)
-- IsGranted = 1: Cấp quyền vượt cấp dù Role không có; IsGranted = 0: Chặn quyền dù Role có
IF OBJECT_ID(N'dbo.UserPermissions', N'U') IS NULL
CREATE TABLE dbo.UserPermissions (
UserId INT NOT NULL,
PermissionId INT NOT NULL,
IsGranted BIT NOT NULL DEFAULT(1),
AssignedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
PRIMARY KEY (UserId, PermissionId),
CONSTRAINT FK_UserPermissions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
CONSTRAINT FK_UserPermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(Id) ON DELETE CASCADE
);
GO

-- PHẦN III: PHÂN HỆ QUẢN TRỊ GIAO DIỆN ĐỘNG (DYNAMIC CMS)

-- 17. Bảng Quản lý Bố cục trang động (SiteLayouts)
IF OBJECT_ID(N'dbo.SiteLayouts', N'U') IS NULL
CREATE TABLE dbo.SiteLayouts (
Id INT IDENTITY(1,1) PRIMARY KEY,
LayoutName NVARCHAR(100) NOT NULL, -- Default, LandingPage, Portal
BlockKey VARCHAR(50) NOT NULL,     -- Header, Slider, MainContent, Sidebar, Footer, CallToAction
BlockTitle NVARCHAR(150) NOT NULL,
OrderIndex INT NOT NULL DEFAULT(0),
IsActive BIT NOT NULL DEFAULT(1),
CustomCssClass VARCHAR(100) NULL,
UpdatedBy VARCHAR(50) NULL,
UpdatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())
);
GO

-- 18. Bảng Menu động đa cấp (Menus)
IF OBJECT_ID(N'dbo.Menus', N'U') IS NULL
CREATE TABLE dbo.Menus (
Id INT IDENTITY(1,1) PRIMARY KEY,
Title NVARCHAR(150) NOT NULL,
ParentId INT NULL,
LinkType VARCHAR(50) NOT NULL DEFAULT('FunctionPage'), -- 'Content', 'Category', 'External', 'FunctionPage'
ReferenceId INT NULL, -- Id của Content hoặc Id Category tương ứng nếu link dạng động
CustomUrl NVARCHAR(500) NULL,
Target VARCHAR(20) NOT NULL DEFAULT('_self'), -- _self, _blank
Position VARCHAR(50) NOT NULL DEFAULT('TopHeader'), -- TopHeader, MainNav, FooterCol1, FooterCol2, Sidebar
OrderIndex INT NOT NULL DEFAULT(0),
IsActive BIT NOT NULL DEFAULT(1),
IconClass VARCHAR(100) NULL,
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_Menus_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Menus(Id)
);
GO

-- 19. Bảng Bài viết & Nội dung tĩnh (Contents)
IF OBJECT_ID(N'dbo.Contents', N'U') IS NULL
CREATE TABLE dbo.Contents (
Id INT IDENTITY(1,1) PRIMARY KEY,
Title NVARCHAR(250) NOT NULL,
Slug VARCHAR(300) NOT NULL UNIQUE,
Summary NVARCHAR(1000) NULL,
BodyHtml NVARCHAR(MAX) NOT NULL,
ThumbnailUrl NVARCHAR(500) NULL,
Status NVARCHAR(50) NOT NULL DEFAULT(N'Published'), -- Draft, Published, Archived
ViewCount INT NOT NULL DEFAULT(0),
AuthorId INT NULL,
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
UpdatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_Contents_Author FOREIGN KEY (AuthorId) REFERENCES dbo.Users(Id) ON DELETE SET NULL
);
GO

-- 20. Bảng Banner / Slider quảng bá (Banners)
IF OBJECT_ID(N'dbo.Banners', N'U') IS NULL
CREATE TABLE dbo.Banners (
Id INT IDENTITY(1,1) PRIMARY KEY,
Title NVARCHAR(200) NOT NULL,
SubTitle NVARCHAR(300) NULL,
ImageUrl NVARCHAR(500) NOT NULL,
LinkUrl NVARCHAR(500) NULL,
Position VARCHAR(50) NOT NULL DEFAULT('HomeSlider'), -- HomeSlider, SidebarTop, PopupPromo, FooterBanner
OrderIndex INT NOT NULL DEFAULT(0),
IsActive BIT NOT NULL DEFAULT(1),
StartDate DATETIME2 NULL,
EndDate DATETIME2 NULL,
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())
);
GO

-- 21. Bảng Cấu hình Chung website (SiteSettings)
IF OBJECT_ID(N'dbo.SiteSettings', N'U') IS NULL
CREATE TABLE dbo.SiteSettings (
Id INT IDENTITY(1,1) PRIMARY KEY,
SettingKey VARCHAR(100) NOT NULL UNIQUE,
SettingValue NVARCHAR(MAX) NULL,
SettingGroup VARCHAR(50) NOT NULL DEFAULT('General'), -- General, Header, Footer, Contact, Social
Description NVARCHAR(250) NULL,
UpdatedAt DATETIME2 NOT NULL DEFAULT(GETDATE())
);
GO

-- PHẦN IV: PHÂN HỆ QUẢN LÝ BÁN HÀNG & KHO VẬN (SALES & INVENTORY)

-- 22. Bảng Danh mục Sản phẩm (Categories)
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
Id INT IDENTITY(1,1) PRIMARY KEY,
Code VARCHAR(50) NOT NULL UNIQUE,
Name NVARCHAR(200) NOT NULL,
Slug VARCHAR(250) NOT NULL UNIQUE,
ParentId INT NULL,
ImageUrl NVARCHAR(500) NULL,
OrderIndex INT NOT NULL DEFAULT(0),
IsActive BIT NOT NULL DEFAULT(1),
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Categories(Id)
);
GO

-- 23. Bảng Hàng hóa / Sản phẩm (Products)
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products (
Id INT IDENTITY(1,1) PRIMARY KEY,
CategoryId INT NOT NULL,
SKU VARCHAR(50) NOT NULL UNIQUE,
Barcode VARCHAR(50) NULL,
Name NVARCHAR(250) NOT NULL,
Slug VARCHAR(300) NOT NULL UNIQUE,
Unit NVARCHAR(50) NOT NULL DEFAULT(N'Cái'),
CostPrice DECIMAL(18,2) NOT NULL DEFAULT(0),
SalePrice DECIMAL(18,2) NOT NULL DEFAULT(0),
Description NVARCHAR(MAX) NULL,
ImageUrl NVARCHAR(500) NULL,
IsActive BIT NOT NULL DEFAULT(1),
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id)
);
GO

-- 24. Bảng Quản lý Tồn kho thực tế (Inventories)
IF OBJECT_ID(N'dbo.Inventories', N'U') IS NULL
CREATE TABLE dbo.Inventories (
Id INT IDENTITY(1,1) PRIMARY KEY,
ProductId INT NOT NULL,
BranchId INT NOT NULL,
StockQuantity INT NOT NULL DEFAULT(0),
MinStockAlert INT NOT NULL DEFAULT(5),
MaxStockLimit INT NOT NULL DEFAULT(1000),
UpdatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT UQ_Inventory_Product_Branch UNIQUE (ProductId, BranchId),
CONSTRAINT FK_Inventories_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
CONSTRAINT FK_Inventories_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id) ON DELETE CASCADE
);
GO

-- 25. Bảng Lịch sử Phiếu Nhập / Xuất / Điều chỉnh Kho (InventoryTransactions)
IF OBJECT_ID(N'dbo.InventoryTransactions', N'U') IS NULL
CREATE TABLE dbo.InventoryTransactions (
Id BIGINT IDENTITY(1,1) PRIMARY KEY,
TransactionCode VARCHAR(50) NOT NULL UNIQUE,
TransactionType VARCHAR(20) NOT NULL, -- 'IN' (Nhập), 'OUT' (Xuất), 'ADJUST' (Điều chỉnh kiểm kê)
BranchId INT NOT NULL,
ProductId INT NOT NULL,
Quantity INT NOT NULL,
UnitPrice DECIMAL(18,2) NOT NULL DEFAULT(0),
TotalAmount AS (Quantity * UnitPrice),
Note NVARCHAR(500) NULL,
CreatedBy INT NOT NULL,
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_InvTrans_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
CONSTRAINT FK_InvTrans_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
CONSTRAINT FK_InvTrans_Users FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id)
);
GO

-- 26. Bảng Đơn đặt hàng & Bán hàng POS (Orders)
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
CREATE TABLE dbo.Orders (
Id BIGINT IDENTITY(1,1) PRIMARY KEY,
OrderCode VARCHAR(50) NOT NULL UNIQUE,
BranchId INT NOT NULL,
CustomerName NVARCHAR(150) NOT NULL,
CustomerPhone VARCHAR(20) NOT NULL,
CustomerAddress NVARCHAR(300) NULL,
TotalAmount DECIMAL(18,2) NOT NULL DEFAULT(0),
DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT(0),
FinalAmount DECIMAL(18,2) NOT NULL DEFAULT(0),
OrderStatus NVARCHAR(50) NOT NULL DEFAULT(N'Pending'), -- Pending, Confirmed, Shipping, Completed, Cancelled
PaymentStatus NVARCHAR(50) NOT NULL DEFAULT(N'Unpaid'), -- Unpaid, PartiallyPaid, Paid
PaymentMethod VARCHAR(50) NOT NULL DEFAULT('CASH'),    -- CASH, BANK_TRANSFER, POS_CARD
CreatedBy INT NULL, -- Nhân viên lập đơn (Null nếu khách đặt Online)
CreatedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
Note NVARCHAR(500) NULL,
CONSTRAINT FK_Orders_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
CONSTRAINT FK_Orders_Users FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id)
);
GO

-- 27. Bảng Chi tiết Đơn hàng (OrderDetails)
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NULL
CREATE TABLE dbo.OrderDetails (
Id BIGINT IDENTITY(1,1) PRIMARY KEY,
OrderId BIGINT NOT NULL,
ProductId INT NOT NULL,
Quantity INT NOT NULL,
UnitPrice DECIMAL(18,2) NOT NULL,
TotalPrice AS (Quantity * UnitPrice),
Note NVARCHAR(250) NULL,
CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
);
GO

-- 28. Bảng Giỏ hàng tạm thời (CartItems)
IF OBJECT_ID(N'dbo.CartItems', N'U') IS NULL
CREATE TABLE dbo.CartItems (
Id BIGINT IDENTITY(1,1) PRIMARY KEY,
SessionId VARCHAR(100) NULL, -- Dành cho khách vãng lai (Guest)
UserId INT NULL,             -- Dành cho khách đã đăng nhập
ProductId INT NOT NULL,
Quantity INT NOT NULL DEFAULT(1),
AddedAt DATETIME2 NOT NULL DEFAULT(GETDATE()),
CONSTRAINT FK_CartItems_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE
);
GO

-- Bật lại kiểm tra ràng buộc toàn vẹn
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all";
GO

-- PHẦN V: DỮ LIỆU MẪU (SEED DATA ĐẦY ĐỦ CHO TẤT CẢ PHÂN HỆ)

-- 1. Seed Doanh nghiệp & Chi nhánh
IF NOT EXISTS (SELECT 1 FROM dbo.Companies)
BEGIN
INSERT INTO dbo.Companies (Code, Name, TaxCode, Phone, Email, Address, IsActive)
VALUES ('CP001', N'TẬP ĐOÀN CÔNG NGHỆ VÀ THƯƠNG MẠI TIÊN PHONG', '0109998888', '02438889999', 'contact@tienphong.vn', N'Tòa nhà Landmark, 72 Cầu Giấy, Hà Nội', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Branches)
BEGIN
DECLARE @CompId INT = (SELECT TOP 1 Id FROM dbo.Companies WHERE Code = 'CP001');
INSERT INTO dbo.Branches (CompanyId, Code, Name, Phone, Address, IsActive)
VALUES
(@CompId, 'HN-CN01', N'Chi nhánh Hà Nội - Trụ sở chính', '02438889999', N'72 Cầu Giấy, Cầu Giấy, Hà Nội', 1),
(@CompId, 'HCM-CN02', N'Chi nhánh TP. Hồ Chí Minh', '02838889999', N'123 Nguyễn Thị Minh Khai, Quận 1, TP. HCM', 1);
END;

-- 2. Seed Phòng ban & Chức vụ
IF NOT EXISTS (SELECT 1 FROM dbo.Departments)
BEGIN
DECLARE @BranchHnId INT = (SELECT TOP 1 Id FROM dbo.Branches WHERE Code = 'HN-CN01');
INSERT INTO dbo.Departments (BranchId, Code, Name, IsActive)
VALUES
(@BranchHnId, 'BOD', N'Ban Tổng Giám Đốc', 1),
(@BranchHnId, 'HRM', N'Phòng Hành chính - Nhân sự & Quản trị VP', 1),
(@BranchHnId, 'IT_CMS', N'Phòng Công nghệ & Quản trị Nội dung CMS', 1),
(@BranchHnId, 'SALES', N'Khối Bán hàng & Phát triển Thị trường', 1),
(@BranchHnId, 'WAREHOUSE', N'Bộ phận Kho vận & Logistics', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Positions)
BEGIN
INSERT INTO dbo.Positions (Code, Name, BaseSalary, AllowanceRate, IsActive)
VALUES
('CEO', N'Tổng Giám Đốc Điều Hành', 50000000, 2.0, 1),
('ADMIN_CMS', N'Chuyên viên Quản trị Nội dung & Giao diện', 18000000, 1.2, 1),
('QTVP_LEAD', N'Trưởng phòng Quản trị Văn phòng & HR', 22000000, 1.3, 1),
('SALES_DIR', N'Giám đốc Kinh doanh (Sale Director)', 35000000, 1.8, 1),
('SALES_EXEC', N'Nhân viên Kinh doanh / Bán hàng POS', 12000000, 1.1, 1),
('WH_KEEPER', N'Thủ kho Trưởng', 14000000, 1.15, 1);
END;

-- 3. Seed Ca làm việc (Shifts)
IF NOT EXISTS (SELECT 1 FROM dbo.Shifts)
BEGIN
INSERT INTO dbo.Shifts (Code, Name, StartTime, EndTime, BreakStartTime, BreakEndTime, WorkHours, IsActive)
VALUES
('HC_STANDARD', N'Ca Hành Chính Chuẩn (8h-17h30)', '08:00:00', '17:30:00', '12:00:00', '13:30:00', 8.0, 1),
('CA_SANG', N'Ca Bán Hàng Sáng (8h-16h)', '08:00:00', '16:00:00', '12:00:00', '13:00:00', 7.0, 1),
('CA_CHIEU', N'Ca Bán Hàng Chiều (15h-22h)', '15:00:00', '22:00:00', '18:00:00', '18:30:00', 6.5, 1);
END;

-- 4. Seed Nhân viên (Employees)
IF NOT EXISTS (SELECT 1 FROM dbo.Employees)
BEGIN
DECLARE @DepBod INT = (SELECT Id FROM dbo.Departments WHERE Code = 'BOD');
DECLARE @DepHr INT = (SELECT Id FROM dbo.Departments WHERE Code = 'HRM');
DECLARE @DepIt INT = (SELECT Id FROM dbo.Departments WHERE Code = 'IT_CMS');
DECLARE @DepSale INT = (SELECT Id FROM dbo.Departments WHERE Code = 'SALES');
DECLARE @DepWh INT = (SELECT Id FROM dbo.Departments WHERE Code = 'WAREHOUSE');

DECLARE @PosCeo INT = (SELECT Id FROM dbo.Positions WHERE Code = 'CEO');
DECLARE @PosCms INT = (SELECT Id FROM dbo.Positions WHERE Code = 'ADMIN_CMS');
DECLARE @PosQtvp INT = (SELECT Id FROM dbo.Positions WHERE Code = 'QTVP_LEAD');
DECLARE @PosSaleDir INT = (SELECT Id FROM dbo.Positions WHERE Code = 'SALES_DIR');
DECLARE @PosSaleExec INT = (SELECT Id FROM dbo.Positions WHERE Code = 'SALES_EXEC');
DECLARE @PosWh INT = (SELECT Id FROM dbo.Positions WHERE Code = 'WH_KEEPER');

INSERT INTO dbo.Employees (DepartmentId, PositionId, EmployeeCode, FullName, DateOfBirth, Gender, Email, Phone, IdentityCard, Status, HireDate)
VALUES 
(@DepBod, @PosCeo, 'EMP001', N'Nguyễn Văn An (SuperAdmin)', '1985-05-15', N'Nam', 'admin@tienphong.vn', '0901111222', '001085000001', 'Active', '2020-01-01'),
(@DepIt, @PosCms, 'EMP002', N'Trần Thị Bích (Admin Menu)', '1992-08-20', N'Nữ', 'menuadmin@tienphong.vn', '0902222333', '001092000002', 'Active', '2021-03-01'),
(@DepIt, @PosCms, 'EMP003', N'Lê Hoàng Cường (Admin Content)', '1995-11-10', N'Nam', 'contentadmin@tienphong.vn', '0903333444', '001095000003', 'Active', '2021-05-15'),
(@DepHr, @PosQtvp, 'EMP004', N'Phạm Thu Dung (Quản trị VP - HR)', '1990-02-18', N'Nữ', 'qtvp@tienphong.vn', '0904444555', '001090000004', 'Active', '2020-06-01'),
(@DepSale, @PosSaleDir, 'EMP005', N'Vũ Quốc Đạt (Giám đốc Sale)', '1988-09-25', N'Nam', 'saledirector@tienphong.vn', '0905555666', '001088000005', 'Active', '2020-09-01'),
(@DepSale, @PosSaleExec, 'EMP006', N'Đỗ Minh Giang (NV Bán hàng)', '1998-04-12', N'Nam', 'salestaff@tienphong.vn', '0906666777', '001098000006', 'Active', '2022-02-01'),
(@DepWh, @PosWh, 'EMP007', N'Hoàng Trọng Hải (Thủ kho)', '1989-12-05', N'Nam', 'warehouse@tienphong.vn', '0907777888', '001089000007', 'Active', '2020-11-15');


END;

-- 5. Seed Nhóm quyền (Roles)
IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
INSERT INTO dbo.Roles (Code, Name, Description, IsSystem, IsActive)
VALUES
('SuperAdmin', N'Quản trị Hệ thống Tối cao', N'Toàn quyền quản trị tất cả các phân hệ và gán quyền đặc quyền', 1, 1),
('AdminMenu', N'Quản trị Menu & Điều hướng', N'Chỉ có quyền tạo, sửa, xóa, duyệt Menu và Điều hướng CMS', 0, 1),
('AdminContent', N'Quản trị Nội dung & Banner', N'Chỉ có quyền soạn thảo bài viết, tin tức, hình ảnh và banner trang chủ', 0, 1),
('HR_MANAGER', N'Quản trị Văn phòng & Nhân sự', N'Quản lý nhân viên, hợp đồng lao động, ca kíp và bảng chấm công', 0, 1),
('SALES_DIRECTOR', N'Giám đốc Kinh doanh', N'Xem báo cáo doanh số, duyệt đơn hàng và phê duyệt chính sách giá', 0, 1),
('SALES_STAFF', N'Nhân viên Bán hàng', N'Tạo đơn hàng POS, tra cứu thông tin sản phẩm và giỏ hàng', 0, 1),
('WAREHOUSE_STAFF', N'Thủ kho', N'Quản lý phiếu xuất/nhập/điều chỉnh kho và cảnh báo tồn tối thiểu', 0, 1);
END;

-- 6. Seed Modules & Permissions
IF NOT EXISTS (SELECT 1 FROM dbo.Modules)
BEGIN
INSERT INTO dbo.Modules (Code, Name, Icon, OrderIndex, IsActive)
VALUES
('system', N'Cấu hình Hệ thống', 'bi bi-gear-fill', 1, 1),
('auth', N'Quản trị Người dùng & Phân quyền', 'bi bi-shield-lock-fill', 2, 1),
('cms_menu', N'Quản trị Menu Trang web', 'bi bi-list-nested', 3, 1),
('cms_content', N'Quản trị Nội dung & Banner', 'bi bi-file-earmark-richtext', 4, 1),
('hrm_emp', N'Quản lý Nhân sự & Hợp đồng', 'bi bi-person-badge', 5, 1),
('hrm_att', N'Quản lý Ca & Chấm công', 'bi bi-clock-history', 6, 1),
('product', N'Quản lý Hàng hóa & Danh mục', 'bi bi-box-seam', 7, 1),
('order', N'Quản lý Đơn hàng & POS', 'bi bi-receipt-cutoff', 8, 1),
('inventory', N'Quản trị Kho & Điều chuyển', 'bi bi-boxes', 9, 1),
('report', N'Báo cáo & Thống kê Tổng quan', 'bi bi-graph-up-arrow', 10, 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions)
BEGIN
DECLARE @ModSys INT = (SELECT Id FROM dbo.Modules WHERE Code = 'system');
DECLARE @ModAuth INT = (SELECT Id FROM dbo.Modules WHERE Code = 'auth');
DECLARE @ModMenu INT = (SELECT Id FROM dbo.Modules WHERE Code = 'cms_menu');
DECLARE @ModCms INT = (SELECT Id FROM dbo.Modules WHERE Code = 'cms_content');
DECLARE @ModEmp INT = (SELECT Id FROM dbo.Modules WHERE Code = 'hrm_emp');
DECLARE @ModAtt INT = (SELECT Id FROM dbo.Modules WHERE Code = 'hrm_att');
DECLARE @ModProd INT = (SELECT Id FROM dbo.Modules WHERE Code = 'product');
DECLARE @ModOrder INT = (SELECT Id FROM dbo.Modules WHERE Code = 'order');
DECLARE @ModInv INT = (SELECT Id FROM dbo.Modules WHERE Code = 'inventory');
DECLARE @ModRep INT = (SELECT Id FROM dbo.Modules WHERE Code = 'report');

INSERT INTO dbo.Permissions (ModuleId, Code, Name, Description, OrderIndex)
VALUES 
-- Auth & System
(@ModSys, 'system.setting.view', N'Xem cấu hình hệ thống', N'Truy cập trang cấu hình giao diện & website', 1),
(@ModSys, 'system.setting.update', N'Sửa cấu hình hệ thống', N'Cập nhật Header, Footer, Logo, Hotline', 2),
(@ModAuth, 'auth.matrix.view', N'Xem ma trận phân quyền', N'Xem bảng phân quyền RBAC và ACL cá nhân', 3),
(@ModAuth, 'auth.matrix.update', N'Cập nhật ma trận phân quyền', N'Tích chọn cấp/thu hồi quyền người dùng', 4),

-- CMS Menu
(@ModMenu, 'menu.view', N'Xem danh sách menu', N'Xem toàn bộ cây điều hướng menu', 10),
(@ModMenu, 'menu.create', N'Thêm mới menu', N'Tạo liên kết menu cấp mới', 11),
(@ModMenu, 'menu.edit', N'Chỉnh sửa menu', N'Cập nhật liên kết, vị trí và thứ tự hiển thị', 12),
(@ModMenu, 'menu.delete', N'Xóa menu', N'Xóa mục menu khỏi hệ thống', 13),

-- CMS Content & Banner
(@ModCms, 'content.view', N'Xem danh sách bài viết', N'Xem bài viết và tin tức', 20),
(@ModCms, 'content.create', N'Tạo bài viết mới', N'Soạn thảo bài viết HTML qua CKEditor/TinyMCE', 21),
(@ModCms, 'content.edit', N'Sửa bài viết', N'Cập nhật nội dung bài viết', 22),
(@ModCms, 'content.delete', N'Xóa bài viết', N'Xóa bài viết', 23),
(@ModCms, 'banner.manage', N'Quản lý Slider & Banner', N'Thêm, sửa, xóa các banner quảng bá', 24),
(@ModCms, 'layout.manage', N'Quản lý Bố cục trang (SiteLayouts)', N'Bật/tắt/sắp xếp các khối Layout', 25),

-- HRM
(@ModEmp, 'hrm.employee.view', N'Xem danh sách nhân viên', N'Xem hồ sơ nhân sự công ty', 30),
(@ModEmp, 'hrm.employee.manage', N'Quản lý hồ sơ nhân viên', N'Thêm, sửa, chuyển phòng ban, cập nhật hợp đồng', 31),
(@ModAtt, 'hrm.attendance.view', N'Xem bảng chấm công', N'Xem giờ vào/ra và dữ liệu điểm danh', 32),
(@ModAtt, 'hrm.attendance.manage', N'Quản lý ca và điều chỉnh chấm công', N'Tạo ca kíp, tính công và bù công', 33),

-- Product & Sales
(@ModProd, 'product.view', N'Xem danh mục & sản phẩm', N'Xem giá bán, giá vốn và quy cách sản phẩm', 40),
(@ModProd, 'product.manage', N'Quản lý sản phẩm', N'Thêm, sửa giá bán, tải ảnh sản phẩm', 41),
(@ModOrder, 'order.pos', N'Bán hàng tại quầy POS', N'Lập hóa đơn và tính tiền trực tiếp', 42),
(@ModOrder, 'order.view', N'Xem danh sách đơn hàng', N'Tra cứu đơn hàng toàn chi nhánh', 43),
(@ModOrder, 'order.manage', N'Quản trị & Duyệt đơn hàng', N'Thay đổi trạng thái, hủy đơn, xác nhận thanh toán', 44),

-- Warehouse & Report
(@ModInv, 'inventory.view', N'Xem tồn kho thực tế', N'Xem số lượng tồn theo chi nhánh và ngưỡng cảnh báo', 50),
(@ModInv, 'inventory.transaction', N'Lập phiếu xuất/nhập kho', N'Tạo phiếu nhập từ NCC hoặc xuất điều chuyển', 51),
(@ModRep, 'report.sales', N'Báo cáo doanh số bán hàng', N'Xem biểu đồ doanh thu theo ngày/tháng', 60),
(@ModRep, 'report.hrm', N'Báo cáo biến động nhân sự', N'Xem báo cáo tỷ lệ đi trễ, nghỉ phép', 61);


END;

-- 7. Seed Mật khẩu người dùng & Tài khoản mẫu
-- Mật khẩu gốc cho toàn bộ tài khoản mẫu là: 123456
-- Chuỗi Hash SHA-256 (Hex) của '123456' là: BA3253876AED6BC22D4A6FF53D8406C6AD864195ED144AB5C87621B6C233B548
-- Trường PasswordSalt để cố định 'DEFAULT_SYSTEM_SALT_2026' để hệ thống ASP.NET Core kiểm tra đồng nhất
IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
DECLARE @Sha256_123456 VARCHAR(256) = 'BA3253876AED6BC22D4A6FF53D8406C6AD864195ED144AB5C87621B6C233B548';
DECLARE @Salt VARCHAR(100) = 'SYS_ERP_SALT_V1';

DECLARE @E1 INT = (SELECT Id FROM dbo.Employees WHERE EmployeeCode = 'EMP001');
DECLARE @E2 INT = (SELECT Id FROM dbo.Employees WHERE EmployeeCode = 'EMP002');
DECLARE @E3 INT = (SELECT Id FROM dbo.Employees WHERE EmployeeCode = 'EMP003');
DECLARE @E4 INT = (SELECT Id FROM dbo.Employees WHERE EmployeeCode = 'EMP004');
DECLARE @E5 INT = (SELECT Id FROM dbo.Employees WHERE EmployeeCode = 'EMP005');
DECLARE @E6 INT = (SELECT Id FROM dbo.Employees WHERE EmployeeCode = 'EMP006');
DECLARE @E7 INT = (SELECT Id FROM dbo.Employees WHERE EmployeeCode = 'EMP007');

INSERT INTO dbo.Users (EmployeeId, Username, PasswordHash, PasswordSalt, IsActive, IsSuperAdmin)
VALUES 
(@E1, 'superadmin', @Sha256_123456, @Salt, 1, 1),
(@E2, 'adminmenu', @Sha256_123456, @Salt, 1, 0),
(@E3, 'admincontent', @Sha256_123456, @Salt, 1, 0),
(@E4, 'qtvp', @Sha256_123456, @Salt, 1, 0),
(@E5, 'saledirector', @Sha256_123456, @Salt, 1, 0),
(@E6, 'salestaff', @Sha256_123456, @Salt, 1, 0),
(@E7, 'warehouse', @Sha256_123456, @Salt, 1, 0);


END;

-- 8. Gán Vai trò cho Users (UserRoles)
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles)
BEGIN
DECLARE @U_SuperAdmin INT = (SELECT Id FROM dbo.Users WHERE Username = 'superadmin');
DECLARE @U_AdminMenu INT = (SELECT Id FROM dbo.Users WHERE Username = 'adminmenu');
DECLARE @U_AdminContent INT = (SELECT Id FROM dbo.Users WHERE Username = 'admincontent');
DECLARE @U_Qtvp INT = (SELECT Id FROM dbo.Users WHERE Username = 'qtvp');
DECLARE @U_SaleDir INT = (SELECT Id FROM dbo.Users WHERE Username = 'saledirector');
DECLARE @U_SaleStaff INT = (SELECT Id FROM dbo.Users WHERE Username = 'salestaff');
DECLARE @U_Wh INT = (SELECT Id FROM dbo.Users WHERE Username = 'warehouse');

DECLARE @R_SuperAdmin INT = (SELECT Id FROM dbo.Roles WHERE Code = 'SuperAdmin');
DECLARE @R_AdminMenu INT = (SELECT Id FROM dbo.Roles WHERE Code = 'AdminMenu');
DECLARE @R_AdminContent INT = (SELECT Id FROM dbo.Roles WHERE Code = 'AdminContent');
DECLARE @R_Qtvp INT = (SELECT Id FROM dbo.Roles WHERE Code = 'HR_MANAGER');
DECLARE @R_SaleDir INT = (SELECT Id FROM dbo.Roles WHERE Code = 'SALES_DIRECTOR');
DECLARE @R_SaleStaff INT = (SELECT Id FROM dbo.Roles WHERE Code = 'SALES_STAFF');
DECLARE @R_Wh INT = (SELECT Id FROM dbo.Roles WHERE Code = 'WAREHOUSE_STAFF');

INSERT INTO dbo.UserRoles (UserId, RoleId)
VALUES 
(@U_SuperAdmin, @R_SuperAdmin),
(@U_AdminMenu, @R_AdminMenu),
(@U_AdminContent, @R_AdminContent),
(@U_Qtvp, @R_Qtvp),
(@U_SaleDir, @R_SaleDir),
(@U_SaleStaff, @R_SaleStaff),
(@U_Wh, @R_Wh);


END;

-- 9. Gán Quyền cho Role (RolePermissions - RBAC)
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions)
BEGIN
DECLARE @RId_Super INT = (SELECT Id FROM dbo.Roles WHERE Code = 'SuperAdmin');
DECLARE @RId_Menu INT = (SELECT Id FROM dbo.Roles WHERE Code = 'AdminMenu');
DECLARE @RId_Content INT = (SELECT Id FROM dbo.Roles WHERE Code = 'AdminContent');
DECLARE @RId_Qtvp INT = (SELECT Id FROM dbo.Roles WHERE Code = 'HR_MANAGER');
DECLARE @RId_SaleDir INT = (SELECT Id FROM dbo.Roles WHERE Code = 'SALES_DIRECTOR');
DECLARE @RId_SaleStaff INT = (SELECT Id FROM dbo.Roles WHERE Code = 'SALES_STAFF');
DECLARE @RId_Wh INT = (SELECT Id FROM dbo.Roles WHERE Code = 'WAREHOUSE_STAFF');

-- SuperAdmin có toàn bộ quyền
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @RId_Super, Id FROM dbo.Permissions;

-- AdminMenu chỉ có cụm menu
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @RId_Menu, Id FROM dbo.Permissions WHERE Code LIKE 'menu.%';

-- AdminContent chỉ có cụm content & banner & layout
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @RId_Content, Id FROM dbo.Permissions WHERE Code LIKE 'content.%' OR Code LIKE 'banner.%' OR Code LIKE 'layout.%';

-- HR_MANAGER (vai trò cũ QTVP) quản trị nhân sự & ca kíp chấm công
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @RId_Qtvp, Id FROM dbo.Permissions WHERE Code LIKE 'hrm.%' OR Code = 'report.hrm';

-- SALES_DIRECTOR (vai trò cũ SaleDirector) quản trị đơn hàng, xem sản phẩm, báo cáo bán hàng
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @RId_SaleDir, Id FROM dbo.Permissions WHERE Code LIKE 'order.%' OR Code = 'product.view' OR Code = 'report.sales';

-- SALES_STAFF (vai trò cũ SalesStaff) có POS và xem sản phẩm, tạo đơn
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @RId_SaleStaff, Id FROM dbo.Permissions WHERE Code IN ('order.pos', 'order.view', 'product.view');

-- WAREHOUSE_STAFF (vai trò cũ WarehouseKeeper) quản lý kho và xuất nhập tồn
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @RId_Wh, Id FROM dbo.Permissions WHERE Code LIKE 'inventory.%' OR Code = 'product.view';


END;

-- 10. Seed Phân quyền Cá nhân ngoại lệ (UserPermissions - ACL Override Matrix)
-- Ví dụ: Bổ sung riêng cho tài khoản qtvp quyền xem Báo cáo doanh số (IsGranted = 1)
IF NOT EXISTS (SELECT 1 FROM dbo.UserPermissions)
BEGIN
DECLARE @UserQtvpId INT = (SELECT Id FROM dbo.Users WHERE Username = 'qtvp');
DECLARE @PermSaleReportId INT = (SELECT Id FROM dbo.Permissions WHERE Code = 'report.sales');

IF (@UserQtvpId IS NOT NULL AND @PermSaleReportId IS NOT NULL)
BEGIN
    INSERT INTO dbo.UserPermissions (UserId, PermissionId, IsGranted)
    VALUES (@UserQtvpId, @PermSaleReportId, 1);
END;


END;

-- 11. Seed CMS Dynamic: SiteLayouts & SiteSettings
IF NOT EXISTS (SELECT 1 FROM dbo.SiteLayouts)
BEGIN
INSERT INTO dbo.SiteLayouts (LayoutName, BlockKey, BlockTitle, OrderIndex, IsActive, CustomCssClass)
VALUES
('Default', 'HeaderTop', N'Thanh thông tin đầu trang (Hotline, Email)', 1, 1, 'bg-dark text-white'),
('Default', 'NavbarMain', N'Thanh Menu Điều hướng Chính', 2, 1, 'navbar-light bg-white shadow-sm'),
('Default', 'HeroSlider', N'Khối Trình chiếu Banner (Slider Carousel)', 3, 1, 'py-2'),
('Default', 'FeatureSection', N'Khối Nổi bật (Tính năng / Cam kết)', 4, 1, 'py-4 bg-light'),
('Default', 'ProductHighlights', N'Khối Sản phẩm Tiêu biểu', 5, 1, 'py-5'),
('Default', 'LatestNews', N'Khối Tin tức & Bài viết Mới nhất', 6, 1, 'py-5 bg-light'),
('Default', 'FooterMain', N'Chân trang & Bản quyền Doanh nghiệp', 7, 1, 'bg-black text-white pt-5 pb-3');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SiteSettings)
BEGIN
INSERT INTO dbo.SiteSettings (SettingKey, SettingValue, SettingGroup, Description)
VALUES
('Site_Name', N'TIEN PHONG ENTERPRISE ERP', 'General', N'Tên hiển thị hệ thống'),
('Site_Logo', '/images/logo.png', 'Header', N'Đường dẫn logo chính thức'),
('Site_Hotline', '1900 6868', 'Contact', N'Hotline hỗ trợ khách hàng'),
('Site_Email', 'support@tienphong.vn', 'Contact', N'Hòm thư tiếp nhận yêu cầu'),
('Site_Address', N'Tầng 12, Tòa nhà Landmark, 72 Cầu Giấy, Hà Nội', 'Footer', N'Địa chỉ trụ sở công ty'),
('Site_Copyright', N'© 2026 Tien Phong Corp. Tất cả các quyền được bảo lưu.', 'Footer', N'Dòng chữ bản quyền chân trang');
END;

-- 12. Seed CMS Dynamic: Menus
IF NOT EXISTS (SELECT 1 FROM dbo.Menus)
BEGIN
-- Menus Cấp 1
INSERT INTO dbo.Menus (Title, ParentId, LinkType, ReferenceId, CustomUrl, Target, Position, OrderIndex, IsActive, IconClass)
VALUES
(N'Trang chủ', NULL, 'FunctionPage', NULL, '/', '_self', 'MainNav', 1, 1, 'bi bi-house-door'),
(N'Giới thiệu', NULL, 'Content', 1, '/bai-viet/gioi-thieu-doanh-nghiep', '_self', 'MainNav', 2, 1, 'bi bi-info-circle'),
(N'Sản phẩm & Giải pháp', NULL, 'FunctionPage', NULL, '/san-pham', '_self', 'MainNav', 3, 1, 'bi bi-grid'),
(N'Tin tức & Sự kiện', NULL, 'FunctionPage', NULL, '/tin-tuc', '_self', 'MainNav', 4, 1, 'bi bi-newspaper'),
(N'Liên hệ', NULL, 'FunctionPage', NULL, '/lien-he', '_self', 'MainNav', 5, 1, 'bi bi-telephone');

-- Menus Con (Cấp 2) trực thuộc Sản phẩm & Giải pháp
DECLARE @MenuParentProd INT = (SELECT Id FROM dbo.Menus WHERE Title = N'Sản phẩm & Giải pháp');
IF (@MenuParentProd IS NOT NULL)
BEGIN
    INSERT INTO dbo.Menus (Title, ParentId, LinkType, ReferenceId, CustomUrl, Target, Position, OrderIndex, IsActive)
    VALUES 
    (N'Thiết bị Tin học & Văn phòng', @MenuParentProd, 'Category', 1, '/danh-muc/thiet-bi-tin-hoc', '_self', 'MainNav', 1, 1),
    (N'Giải pháp Máy chủ & Lưu trữ', @MenuParentProd, 'Category', 2, '/danh-muc/giai-phap-may-chu', '_self', 'MainNav', 2, 1);
END;


END;

-- 13. Seed CMS Dynamic: Contents & Banners
IF NOT EXISTS (SELECT 1 FROM dbo.Contents)
BEGIN
DECLARE @AuthorId INT = (SELECT TOP 1 Id FROM dbo.Users WHERE Username = 'admincontent');
INSERT INTO dbo.Contents (Title, Slug, Summary, BodyHtml, ThumbnailUrl, Status, AuthorId)
VALUES
(N'Giới thiệu Doanh nghiệp Tiên Phong', 'gioi-thieu-doanh-nghiep',
N'Lịch sử hình thành và phát triển của Tập đoàn Công nghệ Tiên Phong từ 2015 đến nay.',
N'Về Chúng TôiTập đoàn Công nghệ và Thương mại Tiên Phong tự hào là đơn vị tiên phong trong việc số hóa quy trình quản trị doanh nghiệp toàn diện với hệ sinh thái ERP thông minh.',
'/images/news-1.svg', 'Published', @AuthorId),
(N'Thông báo Lịch nghỉ Lễ và Kế hoạch Vận hành', 'thong-bao-lich-nghi-le',
N'Kế hoạch trực vận hành phân hệ Bán hàng & Chăm sóc khách hàng trong các ngày nghỉ lễ trọng điểm.',
N'Ban Quản trị xin thông báo lịch vận hành và đường dây nóng hỗ trợ khách hàng luôn mở 24/7 trong suốt kỳ nghỉ...',
'/images/news-2.svg', 'Published', @AuthorId);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Banners)
BEGIN
INSERT INTO dbo.Banners (Title, SubTitle, ImageUrl, LinkUrl, Position, OrderIndex, IsActive)
VALUES
(N'Hệ thống ERP Toàn diện 2026', N'Đột phá năng suất và chuẩn hóa quy trình điều hành', '/images/banner-1.svg', '/san-pham', 'HomeSlider', 1, 1),
(N'Ưu đãi Thiết bị Công nghệ Quý 1', N'Chiết khấu lên tới 25% cho khách hàng doanh nghiệp', '/images/banner-2.svg', '/san-pham', 'HomeSlider', 2, 1);
END;

-- 14. Seed Bán hàng & Kho: Categories, Products, Inventories
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
INSERT INTO dbo.Categories (Code, Name, Slug, OrderIndex, IsActive)
VALUES
('CAT-TBVP', N'Thiết bị Tin học & Văn phòng', 'thiet-bi-tin-hoc', 1, 1),
('CAT-SRV', N'Giải pháp Máy chủ & Lưu trữ', 'giai-phap-may-chu', 2, 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
DECLARE @CatVp INT = (SELECT Id FROM dbo.Categories WHERE Code = 'CAT-TBVP');
DECLARE @CatSrv INT = (SELECT Id FROM dbo.Categories WHERE Code = 'CAT-SRV');

INSERT INTO dbo.Products (CategoryId, SKU, Barcode, Name, Slug, Unit, CostPrice, SalePrice, Description, ImageUrl, IsActive)
VALUES 
(@CatVp, 'DELL-LAT-5440', '893800010001', N'Laptop Dell Latitude 5440 Core i7-1365U', 'laptop-dell-latitude-5440', N'Chiếc', 19500000, 23900000, N'Laptop doanh nghiệp chuẩn độ bền quân đội, RAM 16GB, SSD 512GB NVMe.', '/uploads/products/dell-5440.jpg', 1),
(@CatVp, 'LOGI-MX-M3S', '893800010002', N'Chuột Không dây Logitech MX Master 3S', 'chuot-logitech-mx-master-3s', N'Con', 1800000, 2450000, N'Chuột chuyên dụng cho chuyên viên lập trình và thiết kế cao cấp.', '/uploads/products/mx-master3s.jpg', 1),
(@CatSrv, 'SRV-DL380-G10', '893800010003', N'Máy chủ HP ProLiant DL380 Gen10 Plus', 'may-chu-hp-proliant-dl380-gen10', N'Bộ', 65000000, 82000000, N'Máy chủ doanh nghiệp 2U Dual Intel Xeon Silver, RAM 64GB ECC.', '/uploads/products/hp-dl380.jpg', 1);


END;

IF NOT EXISTS (SELECT 1 FROM dbo.Inventories)
BEGIN
DECLARE @HnBranchId INT = (SELECT TOP 1 Id FROM dbo.Branches WHERE Code = 'HN-CN01');
DECLARE @HcmBranchId INT = (SELECT TOP 1 Id FROM dbo.Branches WHERE Code = 'HCM-CN02');

DECLARE @P1 INT = (SELECT Id FROM dbo.Products WHERE SKU = 'DELL-LAT-5440');
DECLARE @P2 INT = (SELECT Id FROM dbo.Products WHERE SKU = 'LOGI-MX-M3S');
DECLARE @P3 INT = (SELECT Id FROM dbo.Products WHERE SKU = 'SRV-DL380-G10');

INSERT INTO dbo.Inventories (ProductId, BranchId, StockQuantity, MinStockAlert, MaxStockLimit)
VALUES 
(@P1, @HnBranchId, 25, 5, 100),
(@P2, @HnBranchId, 60, 10, 200),
(@P3, @HnBranchId, 5, 2, 20),
(@P1, @HcmBranchId, 15, 5, 50),
(@P2, @HcmBranchId, 30, 5, 100),
(@P3, @HcmBranchId, 2, 1, 10);


END;
GO

-----------------------------------------------------------------------------------
-- PHẦN VI: TẠO CHỈ MỤC (INDEXES) NÂNG CAO HIỆU NĂNG TRUY VẤN
-----------------------------------------------------------------------------------
DROP INDEX IF EXISTS IX_Users_Username ON dbo.Users;
CREATE UNIQUE NONCLUSTERED INDEX IX_Users_Username ON dbo.Users(Username);

DROP INDEX IF EXISTS IX_Employees_Department_Status ON dbo.Employees;
CREATE NONCLUSTERED INDEX IX_Employees_Department_Status ON dbo.Employees(DepartmentId, Status);

DROP INDEX IF EXISTS IX_Attendance_Emp_Date ON dbo.AttendanceDailies;
CREATE NONCLUSTERED INDEX IX_Attendance_Emp_Date ON dbo.AttendanceDailies(EmployeeId, WorkDate);

DROP INDEX IF EXISTS IX_Products_Slug_IsActive ON dbo.Products;
CREATE NONCLUSTERED INDEX IX_Products_Slug_IsActive ON dbo.Products(Slug, IsActive);

DROP INDEX IF EXISTS IX_Menus_Position_Order ON dbo.Menus;
CREATE NONCLUSTERED INDEX IX_Menus_Position_Order ON dbo.Menus(Position, IsActive, OrderIndex);

DROP INDEX IF EXISTS IX_Inventories_Branch_Stock ON dbo.Inventories;
CREATE NONCLUSTERED INDEX IX_Inventories_Branch_Stock ON dbo.Inventories(BranchId, StockQuantity);
GO