-- ==========================================================================
-- PHIÊN 4: SEED DATA (IDEMPOTENT)
-- Script: V4_02_SeedData.sql
-- Mô tả: Dữ liệu mẫu — Roles, Departments, Positions, 50 Employees,
--         100 Products, Warehouses, Customers, LeaveTypes, Units
-- Ngày: 2026-11-04
-- Lưu ý: Mọi INSERT đều kiểm tra tồn tại trước → chạy lại an toàn.
-- ==========================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT N'=== SEED DATA: Bắt đầu ===';

-- ==========================================================================
-- 1. COMPANY & BRANCHES
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Companies] WHERE Code = 'CORP01')
BEGIN
    SET IDENTITY_INSERT [Companies] ON;
    INSERT INTO [Companies] (Id, Code, Name, TaxCode, Phone, Email, Address, IsActive, CreatedAt)
    VALUES (1, 'CORP01', N'Công ty TNHH ERP Việt Nam', '0123456789', '0901234567', 'contact@erpvn.com', N'123 Nguyễn Huệ, Quận 1, TP.HCM', 1, GETUTCDATE());
    SET IDENTITY_INSERT [Companies] OFF;
    PRINT N'   → Company CORP01 đã tạo.';
END

IF NOT EXISTS (SELECT 1 FROM [Branches] WHERE Code = 'HCM01')
BEGIN
    SET IDENTITY_INSERT [Branches] ON;
    INSERT INTO [Branches] (Id, CompanyId, Code, Name, Phone, Address, IsActive, CreatedAt)
    VALUES
        (1, 1, 'HCM01', N'Chi nhánh TP.HCM',   '0901234567', N'123 Nguyễn Huệ, Quận 1, TP.HCM', 1, GETUTCDATE()),
        (2, 1, 'HN01',  N'Chi nhánh Hà Nội',    '0241234567', N'45 Trần Hưng Đạo, Hoàn Kiếm, Hà Nội', 1, GETUTCDATE()),
        (3, 1, 'DN01',  N'Chi nhánh Đà Nẵng',   '0361234567', N'78 Bạch Đằng, Hải Châu, Đà Nẵng', 1, GETUTCDATE());
    SET IDENTITY_INSERT [Branches] OFF;
    PRINT N'   → 3 Branches đã tạo.';
END

-- ==========================================================================
-- 2. ROLES
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE Code = 'ADMIN')
BEGIN
    SET IDENTITY_INSERT [Roles] ON;
    INSERT INTO [Roles] (Id, Code, Name, Description, IsSystem, IsActive, CreatedAt) VALUES
        (1, 'ADMIN',       N'Quản trị hệ thống',      N'Toàn quyền quản trị', 1, 1, GETUTCDATE()),
        (2, 'DIRECTOR',    N'Giám đốc',               N'Duyệt đơn, xem báo cáo toàn hệ thống', 1, 1, GETUTCDATE()),
        (3, 'HR_MANAGER',  N'Trưởng phòng Nhân sự',   N'Quản lý nhân sự, lương, đơn từ', 0, 1, GETUTCDATE()),
        (4, 'ACCOUNTANT',  N'Kế toán',                N'Quản lý công nợ, hóa đơn, thanh toán', 0, 1, GETUTCDATE()),
        (5, 'SALES',       N'Nhân viên kinh doanh',   N'Bán hàng, quản lý đơn hàng', 0, 1, GETUTCDATE()),
        (6, 'WAREHOUSE',   N'Thủ kho',                N'Nhập xuất kho, kiểm kê', 0, 1, GETUTCDATE()),
        (7, 'CMS_EDITOR',  N'Biên tập viên',          N'Quản lý nội dung CMS', 0, 1, GETUTCDATE()),
        (8, 'EMPLOYEE',    N'Nhân viên',              N'Vai trò mặc định cho nhân viên', 0, 1, GETUTCDATE());
    SET IDENTITY_INSERT [Roles] OFF;
    PRINT N'   → 8 Roles đã tạo.';
END

-- ==========================================================================
-- 3. DEPARTMENTS
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Departments] WHERE Code = 'BOD')
BEGIN
    SET IDENTITY_INSERT [Departments] ON;
    INSERT INTO [Departments] (Id, BranchId, Code, Name, IsActive, CreatedAt) VALUES
        (1,  1, 'BOD',    N'Ban Giám đốc',              1, GETUTCDATE()),
        (2,  1, 'HR',     N'Phòng Nhân sự',             1, GETUTCDATE()),
        (3,  1, 'ACCT',   N'Phòng Kế toán',             1, GETUTCDATE()),
        (4,  1, 'SALES',  N'Phòng Kinh doanh',          1, GETUTCDATE()),
        (5,  1, 'WHSE',   N'Phòng Kho vận',             1, GETUTCDATE()),
        (6,  1, 'IT',     N'Phòng Công nghệ thông tin', 1, GETUTCDATE()),
        (7,  1, 'MKT',    N'Phòng Marketing',           1, GETUTCDATE()),
        (8,  2, 'HR-HN',  N'Phòng Nhân sự Hà Nội',     1, GETUTCDATE()),
        (9,  2, 'SALES-HN', N'Phòng KD Hà Nội',        1, GETUTCDATE()),
        (10, 3, 'SALES-DN', N'Phòng KD Đà Nẵng',       1, GETUTCDATE());
    SET IDENTITY_INSERT [Departments] OFF;
    PRINT N'   → 10 Departments đã tạo.';
END

-- ==========================================================================
-- 4. POSITIONS
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Positions] WHERE Code = 'GD')
BEGIN
    SET IDENTITY_INSERT [Positions] ON;
    INSERT INTO [Positions] (Id, Code, Name, IsActive, CreatedAt) VALUES
        (1,  'GD',     N'Giám đốc',              1, GETUTCDATE()),
        (2,  'PGD',    N'Phó Giám đốc',          1, GETUTCDATE()),
        (3,  'TP',     N'Trưởng phòng',          1, GETUTCDATE()),
        (4,  'PP',     N'Phó phòng',             1, GETUTCDATE()),
        (5,  'NV',     N'Nhân viên',             1, GETUTCDATE()),
        (6,  'TTS',    N'Thực tập sinh',         1, GETUTCDATE()),
        (7,  'TK',     N'Thủ kho',              1, GETUTCDATE()),
        (8,  'KT',     N'Kế toán viên',         1, GETUTCDATE()),
        (9,  'LT',     N'Lập trình viên',       1, GETUTCDATE()),
        (10, 'NVKD',   N'Nhân viên kinh doanh', 1, GETUTCDATE());
    SET IDENTITY_INSERT [Positions] OFF;
    PRINT N'   → 10 Positions đã tạo.';
END

-- ==========================================================================
-- 5. EMPLOYEES (50 nhân viên)
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Employees] WHERE EmployeeCode = 'NV001')
BEGIN
    SET IDENTITY_INSERT [Employees] ON;
    -- Cột: Id, DepartmentId, PositionId, EmployeeCode, FullName, DateOfBirth, Gender, Email, Phone, Status, HireDate, CreatedAt
    INSERT INTO [Employees] (Id, DepartmentId, PositionId, EmployeeCode, FullName, DateOfBirth, Gender, Email, Phone, Status, HireDate, CreatedAt) VALUES
        -- Ban Giám đốc (2)
        (1,  1, 1, 'NV001', N'Nguyễn Văn An',       '1975-03-15', N'Nam', 'an.nguyen@erpvn.com',       '0901000001', 'Active', '2020-01-01', GETUTCDATE()),
        (2,  1, 2, 'NV002', N'Trần Thị Bích',       '1978-07-22', N'Nữ',  'bich.tran@erpvn.com',       '0901000002', 'Active', '2020-01-01', GETUTCDATE()),
        -- Phòng Nhân sự (5)
        (3,  2, 3, 'NV003', N'Lê Minh Cường',       '1985-11-10', N'Nam', 'cuong.le@erpvn.com',        '0901000003', 'Active', '2020-03-01', GETUTCDATE()),
        (4,  2, 5, 'NV004', N'Phạm Thị Dung',       '1990-04-05', N'Nữ',  'dung.pham@erpvn.com',       '0901000004', 'Active', '2021-06-15', GETUTCDATE()),
        (5,  2, 5, 'NV005', N'Hoàng Văn Em',        '1992-08-20', N'Nam', 'em.hoang@erpvn.com',        '0901000005', 'Active', '2022-01-10', GETUTCDATE()),
        (6,  2, 5, 'NV006', N'Ngô Thị Phương',      '1993-01-12', N'Nữ',  'phuong.ngo@erpvn.com',      '0901000006', 'Active', '2022-04-01', GETUTCDATE()),
        (7,  2, 4, 'NV007', N'Vũ Đình Gia',         '1991-06-30', N'Nam', 'gia.vu@erpvn.com',          '0901000007', 'Active', '2021-08-01', GETUTCDATE()),
        -- Phòng Kế toán (5)
        (8,  3, 3, 'NV008', N'Đặng Thị Hạnh',       '1987-09-18', N'Nữ',  'hanh.dang@erpvn.com',       '0901000008', 'Active', '2020-05-01', GETUTCDATE()),
        (9,  3, 8, 'NV009', N'Bùi Văn Ích',         '1993-02-25', N'Nam', 'ich.bui@erpvn.com',         '0901000009', 'Active', '2021-09-15', GETUTCDATE()),
        (10, 3, 8, 'NV010', N'Cao Thị Kim',         '1994-12-03', N'Nữ',  'kim.cao@erpvn.com',         '0901000010', 'Active', '2022-02-01', GETUTCDATE()),
        (11, 3, 8, 'NV011', N'Dương Văn Long',      '1989-05-14', N'Nam', 'long.duong@erpvn.com',      '0901000011', 'Active', '2023-01-10', GETUTCDATE()),
        (12, 3, 5, 'NV012', N'Mai Thị Ngọc',        '1995-07-28', N'Nữ',  'ngoc.mai@erpvn.com',        '0901000012', 'Active', '2023-06-01', GETUTCDATE()),
        -- Phòng Kinh doanh HCM (8)
        (13, 4, 3,  'NV013', N'Trịnh Văn Phúc',      '1986-10-07', N'Nam', 'phuc.trinh@erpvn.com',      '0901000013', 'Active', '2020-06-01', GETUTCDATE()),
        (14, 4, 10, 'NV014', N'Lý Thị Quỳnh',        '1991-03-19', N'Nữ',  'quynh.ly@erpvn.com',        '0901000014', 'Active', '2021-02-15', GETUTCDATE()),
        (15, 4, 10, 'NV015', N'Hồ Văn Rạng',         '1993-09-11', N'Nam', 'rang.ho@erpvn.com',         '0901000015', 'Active', '2021-07-01', GETUTCDATE()),
        (16, 4, 10, 'NV016', N'Nguyễn Thị Sương',    '1994-01-30', N'Nữ',  'suong.nguyen@erpvn.com',    '0901000016', 'Active', '2022-03-01', GETUTCDATE()),
        (17, 4, 10, 'NV017', N'Phan Văn Tài',        '1990-06-15', N'Nam', 'tai.phan@erpvn.com',        '0901000017', 'Active', '2022-08-15', GETUTCDATE()),
        (18, 4, 10, 'NV018', N'Đỗ Thị Uyên',         '1992-11-08', N'Nữ',  'uyen.do@erpvn.com',         '0901000018', 'Active', '2023-01-05', GETUTCDATE()),
        (19, 4, 4,  'NV019', N'Lê Văn Vinh',         '1988-04-22', N'Nam', 'vinh.le@erpvn.com',         '0901000019', 'Active', '2021-04-01', GETUTCDATE()),
        (20, 4, 10, 'NV020', N'Trần Thị Xuân',       '1996-12-25', N'Nữ',  'xuan.tran@erpvn.com',       '0901000020', 'Active', '2024-01-15', GETUTCDATE()),
        -- Phòng Kho vận (5)
        (21, 5, 3, 'NV021', N'Nguyễn Hữu Yên',      '1984-02-14', N'Nam', 'yen.nguyen@erpvn.com',      '0901000021', 'Active', '2020-07-01', GETUTCDATE()),
        (22, 5, 7, 'NV022', N'Võ Văn Anh',          '1991-08-05', N'Nam', 'anh.vo@erpvn.com',          '0901000022', 'Active', '2021-11-01', GETUTCDATE()),
        (23, 5, 7, 'NV023', N'Trương Thị Bé',       '1993-05-17', N'Nữ',  'be.truong@erpvn.com',       '0901000023', 'Active', '2022-05-15', GETUTCDATE()),
        (24, 5, 5, 'NV024', N'Huỳnh Văn Châu',      '1990-10-09', N'Nam', 'chau.huynh@erpvn.com',      '0901000024', 'Active', '2023-03-01', GETUTCDATE()),
        (25, 5, 5, 'NV025', N'Lâm Thị Diễm',        '1995-03-28', N'Nữ',  'diem.lam@erpvn.com',        '0901000025', 'Active', '2024-02-01', GETUTCDATE()),
        -- Phòng IT (5)
        (26, 6, 3, 'NV026', N'Đoàn Minh Đức',       '1988-07-20', N'Nam', 'duc.doan@erpvn.com',        '0901000026', 'Active', '2020-08-01', GETUTCDATE()),
        (27, 6, 9, 'NV027', N'Tô Văn Giang',        '1992-01-15', N'Nam', 'giang.to@erpvn.com',        '0901000027', 'Active', '2021-05-15', GETUTCDATE()),
        (28, 6, 9, 'NV028', N'Châu Thị Hà',         '1994-09-03', N'Nữ',  'ha.chau@erpvn.com',         '0901000028', 'Active', '2022-06-01', GETUTCDATE()),
        (29, 6, 9, 'NV029', N'Lương Văn Hải',       '1993-04-12', N'Nam', 'hai.luong@erpvn.com',       '0901000029', 'Active', '2023-02-15', GETUTCDATE()),
        (30, 6, 6, 'NV030', N'Nguyễn Thị Lan',      '1997-11-27', N'Nữ',  'lan.nguyen2@erpvn.com',     '0901000030', 'Active', '2024-06-01', GETUTCDATE()),
        -- Phòng Marketing (5)
        (31, 7, 3, 'NV031', N'Trần Quốc Minh',      '1987-06-08', N'Nam', 'minh.tran@erpvn.com',       '0901000031', 'Active', '2020-09-01', GETUTCDATE()),
        (32, 7, 5, 'NV032', N'Phạm Thị Nga',        '1993-02-14', N'Nữ',  'nga.pham@erpvn.com',        '0901000032', 'Active', '2022-01-15', GETUTCDATE()),
        (33, 7, 5, 'NV033', N'Lê Hoàng Oanh',       '1995-08-19', N'Nữ',  'oanh.le@erpvn.com',         '0901000033', 'Active', '2022-09-01', GETUTCDATE()),
        (34, 7, 5, 'NV034', N'Hà Văn Phong',        '1991-12-01', N'Nam', 'phong.ha@erpvn.com',        '0901000034', 'Active', '2023-04-15', GETUTCDATE()),
        (35, 7, 6, 'NV035', N'Kiều Thị Quế',        '1996-05-23', N'Nữ',  'que.kieu@erpvn.com',        '0901000035', 'Active', '2024-03-01', GETUTCDATE()),
        -- Chi nhánh Hà Nội (10)
        (36, 8,  3,  'NV036', N'Vương Đình Sơn',      '1986-01-18', N'Nam', 'son.vuong@erpvn.com',       '0241000036', 'Active', '2021-01-01', GETUTCDATE()),
        (37, 8,  5,  'NV037', N'Đinh Thị Thanh',      '1992-07-04', N'Nữ',  'thanh.dinh@erpvn.com',      '0241000037', 'Active', '2021-06-01', GETUTCDATE()),
        (38, 9,  3,  'NV038', N'Bùi Xuân Ước',        '1990-03-22', N'Nam', 'uoc.bui@erpvn.com',         '0241000038', 'Active', '2021-03-01', GETUTCDATE()),
        (39, 9,  10, 'NV039', N'Trần Văn Viện',       '1993-10-16', N'Nam', 'vien.tran@erpvn.com',       '0241000039', 'Active', '2022-01-15', GETUTCDATE()),
        (40, 9,  10, 'NV040', N'Lê Thị Yến',          '1994-05-09', N'Nữ',  'yen.le@erpvn.com',          '0241000040', 'Active', '2022-07-01', GETUTCDATE()),
        (41, 9,  10, 'NV041', N'Nguyễn Thành Đạt',    '1991-08-13', N'Nam', 'dat.nguyen@erpvn.com',      '0241000041', 'Active', '2023-02-01', GETUTCDATE()),
        (42, 9,  10, 'NV042', N'Phạm Hồng Hải',       '1989-12-30', N'Nam', 'hai.pham@erpvn.com',        '0241000042', 'Active', '2023-05-15', GETUTCDATE()),
        (43, 8,  5,  'NV043', N'Vũ Thị Hương',        '1995-02-06', N'Nữ',  'huong.vu@erpvn.com',        '0241000043', 'Active', '2023-08-01', GETUTCDATE()),
        (44, 9,  10, 'NV044', N'Đặng Quốc Khánh',     '1992-06-21', N'Nam', 'khanh.dang@erpvn.com',      '0241000044', 'Active', '2024-01-10', GETUTCDATE()),
        (45, 9,  6,  'NV045', N'Hoàng Thị Liên',      '1996-09-14', N'Nữ',  'lien.hoang@erpvn.com',      '0241000045', 'Active', '2024-07-01', GETUTCDATE()),
        -- Chi nhánh Đà Nẵng (5)
        (46, 10, 3,  'NV046', N'Trần Đức Mạnh',       '1988-04-17', N'Nam', 'manh.tran@erpvn.com',       '0361000046', 'Active', '2022-01-01', GETUTCDATE()),
        (47, 10, 10, 'NV047', N'Lê Thị Như',          '1993-11-25', N'Nữ',  'nhu.le@erpvn.com',          '0361000047', 'Active', '2022-06-15', GETUTCDATE()),
        (48, 10, 10, 'NV048', N'Phan Minh Quân',      '1994-07-03', N'Nam', 'quan.phan@erpvn.com',       '0361000048', 'Active', '2023-01-01', GETUTCDATE()),
        (49, 10, 10, 'NV049', N'Ngô Thị Tâm',         '1995-10-12', N'Nữ',  'tam.ngo@erpvn.com',         '0361000049', 'Active', '2023-09-01', GETUTCDATE()),
        (50, 10, 6,  'NV050', N'Huỳnh Văn Thắng',     '1997-03-08', N'Nam', 'thang.huynh@erpvn.com',     '0361000050', 'Active', '2024-05-01', GETUTCDATE());
    SET IDENTITY_INSERT [Employees] OFF;
    PRINT N'   → 50 Employees đã tạo.';
END

-- ==========================================================================
-- 6. UNITS (Đơn vị tính)
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Units] WHERE Code = 'CAI')
BEGIN
    SET IDENTITY_INSERT [Units] ON;
    INSERT INTO [Units] (Id, Code, Name, IsActive, CreatedAt) VALUES
        (1, 'CAI',   N'Cái',       1, GETUTCDATE()),
        (2, 'CHIEC', N'Chiếc',     1, GETUTCDATE()),
        (3, 'BO',    N'Bộ',        1, GETUTCDATE()),
        (4, 'HOP',   N'Hộp',       1, GETUTCDATE()),
        (5, 'THUNG', N'Thùng',     1, GETUTCDATE()),
        (6, 'KG',    N'Kg',        1, GETUTCDATE()),
        (7, 'MET',   N'Mét',       1, GETUTCDATE()),
        (8, 'LON',   N'Lon',       1, GETUTCDATE()),
        (9, 'GOI',   N'Gói',       1, GETUTCDATE()),
        (10,'CUON',  N'Cuộn',      1, GETUTCDATE());
    SET IDENTITY_INSERT [Units] OFF;
    PRINT N'   → 10 Units đã tạo.';
END

-- ==========================================================================
-- 7. LEAVE TYPES (Loại nghỉ phép)
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [LeaveTypes] WHERE Code = 'ANNUAL')
BEGIN
    SET IDENTITY_INSERT [LeaveTypes] ON;
    INSERT INTO [LeaveTypes] (Id, Code, Name, MaxDaysPerYear, IsPaid, AllowHalfDay, RequiresAttachment, IsActive, CreatedAt) VALUES
        (1, 'ANNUAL',   N'Nghỉ phép năm',         12, 1, 1, 0, 1, GETUTCDATE()),
        (2, 'SICK',     N'Nghỉ ốm',               30, 1, 0, 1, 1, GETUTCDATE()),
        (3, 'MATERNITY',N'Nghỉ thai sản',         180, 1, 0, 1, 1, GETUTCDATE()),
        (4, 'WEDDING',  N'Nghỉ cưới',              3, 1, 0, 0, 1, GETUTCDATE()),
        (5, 'FUNERAL',  N'Nghỉ tang',              3, 1, 0, 0, 1, GETUTCDATE()),
        (6, 'UNPAID',   N'Nghỉ không lương',      365, 0, 1, 0, 1, GETUTCDATE()),
        (7, 'COMP',     N'Nghỉ bù',               30, 1, 1, 0, 1, GETUTCDATE());
    SET IDENTITY_INSERT [LeaveTypes] OFF;
    PRINT N'   → 7 LeaveTypes đã tạo.';
END

-- ==========================================================================
-- 8. CATEGORIES (Danh mục sản phẩm)
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Categories] WHERE Code = 'DT')
BEGIN
    SET IDENTITY_INSERT [Categories] ON;
    INSERT INTO [Categories] (Id, Code, Name, Slug, IsActive, CreatedAt) VALUES
        (1,  'DT',    N'Điện thoại',       'dien-thoai',        1, GETUTCDATE()),
        (2,  'LT',    N'Laptop',           'laptop',            1, GETUTCDATE()),
        (3,  'TB',    N'Tablet',           'tablet',            1, GETUTCDATE()),
        (4,  'PK',    N'Phụ kiện',         'phu-kien',          1, GETUTCDATE()),
        (5,  'MH',    N'Màn hình',         'man-hinh',          1, GETUTCDATE()),
        (6,  'MT',    N'Máy tính để bàn',  'may-tinh-de-ban',   1, GETUTCDATE()),
        (7,  'MI',    N'Máy in',           'may-in',            1, GETUTCDATE()),
        (8,  'TN',    N'Thiết bị mạng',    'thiet-bi-mang',     1, GETUTCDATE()),
        (9,  'AM',    N'Âm thanh',         'am-thanh',          1, GETUTCDATE()),
        (10, 'LK',    N'Linh kiện',        'linh-kien',         1, GETUTCDATE());
    SET IDENTITY_INSERT [Categories] OFF;
    PRINT N'   → 10 Categories đã tạo.';
END

-- ==========================================================================
-- 9. PRODUCTS (100 sản phẩm)
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Products] WHERE SKU = 'DT-IP16-128')
BEGIN
    SET IDENTITY_INSERT [Products] ON;
    INSERT INTO [Products] (Id, CategoryId, UnitId, SKU, Name, Slug, Unit, CostPrice, SalePrice, IsActive, CreatedAt) VALUES
        -- Điện thoại (20)
        (1,  1, 1, 'DT-IP16-128',   N'iPhone 16 128GB',           'iphone-16-128gb',           N'Chiếc', 22000000, 25990000, 1, GETUTCDATE()),
        (2,  1, 1, 'DT-IP16-256',   N'iPhone 16 256GB',           'iphone-16-256gb',           N'Chiếc', 24000000, 28990000, 1, GETUTCDATE()),
        (3,  1, 1, 'DT-IP16P-256',  N'iPhone 16 Pro 256GB',       'iphone-16-pro-256gb',       N'Chiếc', 28000000, 32990000, 1, GETUTCDATE()),
        (4,  1, 1, 'DT-IP16PM-512', N'iPhone 16 Pro Max 512GB',   'iphone-16-pro-max-512gb',   N'Chiếc', 35000000, 39990000, 1, GETUTCDATE()),
        (5,  1, 1, 'DT-SS25U-256',  N'Samsung Galaxy S25 Ultra',  'samsung-galaxy-s25-ultra',  N'Chiếc', 27000000, 31990000, 1, GETUTCDATE()),
        (6,  1, 1, 'DT-SS25-128',   N'Samsung Galaxy S25 128GB',  'samsung-galaxy-s25-128gb',  N'Chiếc', 18000000, 21990000, 1, GETUTCDATE()),
        (7,  1, 1, 'DT-SS-A55',     N'Samsung Galaxy A55',        'samsung-galaxy-a55',        N'Chiếc', 8000000,  9990000,  1, GETUTCDATE()),
        (8,  1, 1, 'DT-SS-A35',     N'Samsung Galaxy A35',        'samsung-galaxy-a35',        N'Chiếc', 6500000,  7990000,  1, GETUTCDATE()),
        (9,  1, 1, 'DT-XM14U',      N'Xiaomi 14 Ultra',           'xiaomi-14-ultra',           N'Chiếc', 17000000, 19990000, 1, GETUTCDATE()),
        (10, 1, 1, 'DT-XM-RN13',    N'Xiaomi Redmi Note 13',      'xiaomi-redmi-note-13',      N'Chiếc', 4200000,  4990000,  1, GETUTCDATE()),
        (11, 1, 1, 'DT-OP-R3',      N'OPPO Reno 11',              'oppo-reno-11',              N'Chiếc', 7500000,  8990000,  1, GETUTCDATE()),
        (12, 1, 1, 'DT-VV-V30',     N'Vivo V30',                  'vivo-v30',                  N'Chiếc', 7000000,  8490000,  1, GETUTCDATE()),
        (13, 1, 1, 'DT-IP15-128',   N'iPhone 15 128GB',           'iphone-15-128gb',           N'Chiếc', 17000000, 19990000, 1, GETUTCDATE()),
        (14, 1, 1, 'DT-IP15P-256',  N'iPhone 15 Pro 256GB',       'iphone-15-pro-256gb',       N'Chiếc', 24000000, 27990000, 1, GETUTCDATE()),
        (15, 1, 1, 'DT-GG-P9P',     N'Google Pixel 9 Pro',        'google-pixel-9-pro',        N'Chiếc', 20000000, 23990000, 1, GETUTCDATE()),
        (16, 1, 1, 'DT-HW-P70',     N'Huawei Pura 70 Pro',        'huawei-pura-70-pro',        N'Chiếc', 18000000, 21990000, 1, GETUTCDATE()),
        (17, 1, 1, 'DT-NK-G42',     N'Nokia G42',                 'nokia-g42',                 N'Chiếc', 3500000,  4290000,  1, GETUTCDATE()),
        (18, 1, 1, 'DT-RE-11T',     N'Realme 11 Pro+',            'realme-11-pro-plus',        N'Chiếc', 7000000,  8290000,  1, GETUTCDATE()),
        (19, 1, 1, 'DT-SS-ZF6',     N'Samsung Galaxy Z Fold 6',   'samsung-galaxy-z-fold-6',   N'Chiếc', 38000000, 43990000, 1, GETUTCDATE()),
        (20, 1, 1, 'DT-SS-ZFL6',    N'Samsung Galaxy Z Flip 6',   'samsung-galaxy-z-flip-6',   N'Chiếc', 22000000, 25990000, 1, GETUTCDATE()),
        -- Laptop (20)
        (21, 2, 1, 'LT-MB-AIR-M3',  N'MacBook Air M3 13"',        'macbook-air-m3-13',         N'Chiếc', 25000000, 28990000, 1, GETUTCDATE()),
        (22, 2, 1, 'LT-MB-PRO-M3P', N'MacBook Pro M3 Pro 14"',    'macbook-pro-m3-pro-14',     N'Chiếc', 42000000, 48990000, 1, GETUTCDATE()),
        (23, 2, 1, 'LT-DL-XPS15',   N'Dell XPS 15 i7',            'dell-xps-15-i7',            N'Chiếc', 32000000, 37990000, 1, GETUTCDATE()),
        (24, 2, 1, 'LT-DL-LAT5540', N'Dell Latitude 5540',        'dell-latitude-5540',        N'Chiếc', 22000000, 25990000, 1, GETUTCDATE()),
        (25, 2, 1, 'LT-HP-ENV16',   N'HP Envy 16 i7',             'hp-envy-16-i7',             N'Chiếc', 28000000, 32990000, 1, GETUTCDATE()),
        (26, 2, 1, 'LT-HP-PROB450', N'HP ProBook 450 G10',        'hp-probook-450-g10',        N'Chiếc', 15000000, 17990000, 1, GETUTCDATE()),
        (27, 2, 1, 'LT-LN-TP-X1C',  N'Lenovo ThinkPad X1 Carbon', 'lenovo-thinkpad-x1-carbon', N'Chiếc', 35000000, 39990000, 1, GETUTCDATE()),
        (28, 2, 1, 'LT-LN-YOGA9',   N'Lenovo Yoga 9i 14"',        'lenovo-yoga-9i-14',         N'Chiếc', 30000000, 34990000, 1, GETUTCDATE()),
        (29, 2, 1, 'LT-AS-ZB14',    N'ASUS Zenbook 14 OLED',      'asus-zenbook-14-oled',      N'Chiếc', 22000000, 25990000, 1, GETUTCDATE()),
        (30, 2, 1, 'LT-AS-ROG16',   N'ASUS ROG Strix G16',        'asus-rog-strix-g16',        N'Chiếc', 32000000, 37990000, 1, GETUTCDATE()),
        (31, 2, 1, 'LT-AC-SP5-14',  N'Acer Swift 5 14"',          'acer-swift-5-14',           N'Chiếc', 18000000, 21990000, 1, GETUTCDATE()),
        (32, 2, 1, 'LT-AC-NITRO5',  N'Acer Nitro 5 Gaming',       'acer-nitro-5-gaming',       N'Chiếc', 20000000, 23990000, 1, GETUTCDATE()),
        (33, 2, 1, 'LT-MS-SLP5',    N'Microsoft Surface Laptop 5','microsoft-surface-laptop-5', N'Chiếc', 26000000, 29990000, 1, GETUTCDATE()),
        (34, 2, 1, 'LT-MS-SPR9',    N'Microsoft Surface Pro 9',   'microsoft-surface-pro-9',   N'Chiếc', 24000000, 27990000, 1, GETUTCDATE()),
        (35, 2, 1, 'LT-LG-GRAM17',  N'LG Gram 17 2024',           'lg-gram-17-2024',           N'Chiếc', 28000000, 32990000, 1, GETUTCDATE()),
        (36, 2, 1, 'LT-HP-OMEN16',  N'HP OMEN 16 Gaming',         'hp-omen-16-gaming',         N'Chiếc', 28000000, 32990000, 1, GETUTCDATE()),
        (37, 2, 1, 'LT-DL-INS3520', N'Dell Inspiron 15 3520',     'dell-inspiron-15-3520',     N'Chiếc', 12000000, 14990000, 1, GETUTCDATE()),
        (38, 2, 1, 'LT-LN-IP5-15',  N'Lenovo IdeaPad 5 15"',      'lenovo-ideapad-5-15',       N'Chiếc', 14000000, 16990000, 1, GETUTCDATE()),
        (39, 2, 1, 'LT-AS-VVB-15',  N'ASUS VivoBook 15',          'asus-vivobook-15',          N'Chiếc', 11000000, 13490000, 1, GETUTCDATE()),
        (40, 2, 1, 'LT-MB-AIR-M2',  N'MacBook Air M2 15"',        'macbook-air-m2-15',         N'Chiếc', 26000000, 29990000, 1, GETUTCDATE()),
        -- Tablet (10)
        (41, 3, 1, 'TB-IP-AIR-M2',  N'iPad Air M2 11"',           'ipad-air-m2-11',            N'Chiếc', 14000000, 16490000, 1, GETUTCDATE()),
        (42, 3, 1, 'TB-IP-PRO-M4',  N'iPad Pro M4 11"',           'ipad-pro-m4-11',            N'Chiếc', 24000000, 27990000, 1, GETUTCDATE()),
        (43, 3, 1, 'TB-IP-10TH',    N'iPad 10th Gen',             'ipad-10th-gen',             N'Chiếc', 8000000,  9990000,  1, GETUTCDATE()),
        (44, 3, 1, 'TB-SS-S9FE',    N'Samsung Galaxy Tab S9 FE',  'samsung-galaxy-tab-s9-fe',  N'Chiếc', 8500000,  9990000,  1, GETUTCDATE()),
        (45, 3, 1, 'TB-SS-S9U',     N'Samsung Galaxy Tab S9 Ultra','samsung-galaxy-tab-s9-ultra',N'Chiếc', 22000000, 25990000, 1, GETUTCDATE()),
        (46, 3, 1, 'TB-XM-PAD6',    N'Xiaomi Pad 6',              'xiaomi-pad-6',              N'Chiếc', 6500000,  7990000,  1, GETUTCDATE()),
        (47, 3, 1, 'TB-IP-MINI-A17','iPad Mini A17 Pro',          'ipad-mini-a17-pro',         N'Chiếc', 12000000, 14990000, 1, GETUTCDATE()),
        (48, 3, 1, 'TB-LN-TAB-P12', N'Lenovo Tab P12 Pro',        'lenovo-tab-p12-pro',        N'Chiếc', 9000000,  10990000, 1, GETUTCDATE()),
        (49, 3, 1, 'TB-HW-MPAD-11', N'Huawei MatePad 11.5"',      'huawei-matepad-11-5',       N'Chiếc', 7000000,  8490000,  1, GETUTCDATE()),
        (50, 3, 1, 'TB-MS-SGO3',    N'Microsoft Surface Go 3',    'microsoft-surface-go-3',    N'Chiếc', 8000000,  9490000,  1, GETUTCDATE()),
        -- Phụ kiện (15)
        (51, 4, 1, 'PK-AP-20W',     N'Sạc Apple 20W USB-C',       'sac-apple-20w-usb-c',       N'Cái',   400000,   490000,   1, GETUTCDATE()),
        (52, 4, 1, 'PK-AP-C2C',     N'Cáp USB-C to USB-C Apple',  'cap-usb-c-apple',           N'Sợi',   300000,   390000,   1, GETUTCDATE()),
        (53, 4, 1, 'PK-AP-AIRTAG',  N'Apple AirTag 4 Pack',       'apple-airtag-4-pack',       N'Hộp',   2500000,  2990000,  1, GETUTCDATE()),
        (54, 4, 1, 'PK-AP-PENCIL2', N'Apple Pencil 2',            'apple-pencil-2',            N'Cây',   2800000,  3290000,  1, GETUTCDATE()),
        (55, 4, 1, 'PK-ANK-PB10K',  N'Anker PowerBank 10000mAh',  'anker-powerbank-10000',     N'Cái',   500000,   650000,   1, GETUTCDATE()),
        (56, 4, 1, 'PK-ANK-PB20K',  N'Anker PowerBank 20000mAh',  'anker-powerbank-20000',     N'Cái',   800000,   990000,   1, GETUTCDATE()),
        (57, 4, 1, 'PK-LOG-MX-MS3', N'Logitech MX Master 3S',     'logitech-mx-master-3s',     N'Cái',   2000000,  2490000,  1, GETUTCDATE()),
        (58, 4, 1, 'PK-LOG-K580',   N'Logitech K580 Keyboard',    'logitech-k580',             N'Cái',   700000,   890000,   1, GETUTCDATE()),
        (59, 4, 1, 'PK-DGR-SC-12P', N'Dán cường lực iPhone 16 PM','dan-cuong-luc-ip16pm',      N'Miếng', 150000,   250000,   1, GETUTCDATE()),
        (60, 4, 1, 'PK-OP-CAS-IP16',N'Ốp lưng iPhone 16',         'op-lung-iphone-16',         N'Cái',   200000,   350000,   1, GETUTCDATE()),
        (61, 4, 1, 'PK-SS-SPEN',    N'Samsung S Pen Pro',          'samsung-s-pen-pro',         N'Cây',   1200000,  1490000,  1, GETUTCDATE()),
        (62, 4, 1, 'PK-JBL-CLIP4',  N'JBL Clip 4 Speaker',        'jbl-clip-4-speaker',        N'Cái',   900000,   1190000,  1, GETUTCDATE()),
        (63, 4, 1, 'PK-UGREEN-HUB', N'UGREEN 7-in-1 USB-C Hub',   'ugreen-7in1-usb-c-hub',     N'Cái',   600000,   790000,   1, GETUTCDATE()),
        (64, 4, 1, 'PK-BLK-WC15W',  N'Belkin Wireless Charger 15W','belkin-wireless-charger',   N'Cái',   700000,   890000,   1, GETUTCDATE()),
        (65, 4, 1, 'PK-TM-WEBCAM',  N'Thinkmate Webcam 2K',       'thinkmate-webcam-2k',       N'Cái',   800000,   990000,   1, GETUTCDATE()),
        -- Màn hình (10)
        (66, 5, 1, 'MH-DL-U2723QE', N'Dell U2723QE 4K 27"',       'dell-u2723qe-4k-27',       N'Cái',   10000000, 12990000, 1, GETUTCDATE()),
        (67, 5, 1, 'MH-DL-S2422HG', N'Dell S2422HG Gaming 24"',   'dell-s2422hg-gaming-24',    N'Cái',   4000000,  4990000,  1, GETUTCDATE()),
        (68, 5, 1, 'MH-LG-27UP850', N'LG 27UP850 4K USB-C',       'lg-27up850-4k-usb-c',      N'Cái',   8000000,  9990000,  1, GETUTCDATE()),
        (69, 5, 1, 'MH-LG-34WN80C', N'LG 34WN80C Ultrawide 34"',  'lg-34wn80c-ultrawide-34',   N'Cái',   12000000, 14990000, 1, GETUTCDATE()),
        (70, 5, 1, 'MH-SS-LS27B',   N'Samsung LS27B 27" IPS',     'samsung-ls27b-27-ips',      N'Cái',   5000000,  5990000,  1, GETUTCDATE()),
        (71, 5, 1, 'MH-AS-VG27AQ',  N'ASUS VG27AQ 27" Gaming',    'asus-vg27aq-27-gaming',     N'Cái',   7000000,  8490000,  1, GETUTCDATE()),
        (72, 5, 1, 'MH-BQ-PD2725U', N'BenQ PD2725U Designer 27"', 'benq-pd2725u-designer-27',  N'Cái',   15000000, 17990000, 1, GETUTCDATE()),
        (73, 5, 1, 'MH-AC-CB242Y',  N'Acer CB242Y 24" FHD IPS',   'acer-cb242y-24-fhd-ips',    N'Cái',   3000000,  3690000,  1, GETUTCDATE()),
        (74, 5, 1, 'MH-HP-E24MV',   N'HP E24mv G4 24" FHD',       'hp-e24mv-g4-24-fhd',       N'Cái',   5000000,  5990000,  1, GETUTCDATE()),
        (75, 5, 1, 'MH-VP-VP2756',  N'ViewSonic VP2756 27" 4K',   'viewsonic-vp2756-27-4k',    N'Cái',   9000000,  10990000, 1, GETUTCDATE()),
        -- Máy tính để bàn (5)
        (76, 6, 3, 'MT-DL-OPT7020', N'Dell OptiPlex 7020 i7',     'dell-optiplex-7020-i7',     N'Bộ',    18000000, 21990000, 1, GETUTCDATE()),
        (77, 6, 3, 'MT-HP-PRO400',  N'HP ProDesk 400 G9 i5',      'hp-prodesk-400-g9-i5',      N'Bộ',    12000000, 14990000, 1, GETUTCDATE()),
        (78, 6, 3, 'MT-LN-TC-M90S', N'Lenovo ThinkCentre M90s i7','lenovo-thinkcentre-m90s',    N'Bộ',    16000000, 18990000, 1, GETUTCDATE()),
        (79, 6, 3, 'MT-AP-MINI-M2', N'Apple Mac Mini M2',          'apple-mac-mini-m2',         N'Bộ',    13000000, 15490000, 1, GETUTCDATE()),
        (80, 6, 3, 'MT-AP-IMAC-M3', N'Apple iMac M3 24"',         'apple-imac-m3-24',          N'Bộ',    32000000, 36990000, 1, GETUTCDATE()),
        -- Máy in (5)
        (81, 7, 1, 'MI-HP-LJ-M404', N'HP LaserJet M404dn',        'hp-laserjet-m404dn',        N'Cái',   6000000,  7290000,  1, GETUTCDATE()),
        (82, 7, 1, 'MI-CN-MF746',   N'Canon MF746Cx Color',       'canon-mf746cx-color',       N'Cái',   12000000, 14490000, 1, GETUTCDATE()),
        (83, 7, 1, 'MI-EP-L3560',   N'Epson EcoTank L3560',       'epson-ecotank-l3560',       N'Cái',   5000000,  5990000,  1, GETUTCDATE()),
        (84, 7, 1, 'MI-BR-HLL2',    N'Brother HL-L2400DW',        'brother-hl-l2400dw',        N'Cái',   3500000,  4290000,  1, GETUTCDATE()),
        (85, 7, 1, 'MI-HP-DJ2775',  N'HP DeskJet 2775 AIO',       'hp-deskjet-2775-aio',       N'Cái',   2000000,  2490000,  1, GETUTCDATE()),
        -- Thiết bị mạng (5)
        (86, 8, 1, 'TN-TP-AX73',    N'TP-Link Archer AX73 WiFi6', 'tp-link-archer-ax73-wifi6', N'Cái',   2000000,  2490000,  1, GETUTCDATE()),
        (87, 8, 1, 'TN-AS-RT-AX86', N'ASUS RT-AX86U Pro WiFi6',   'asus-rt-ax86u-pro-wifi6',   N'Cái',   4500000,  5290000,  1, GETUTCDATE()),
        (88, 8, 1, 'TN-UB-UAP-AC',  N'Ubiquiti UAP-AC-Pro',       'ubiquiti-uap-ac-pro',       N'Cái',   3000000,  3690000,  1, GETUTCDATE()),
        (89, 8, 1, 'TN-TP-SW16G',   N'TP-Link Switch 16-Port Gig','tp-link-switch-16-port-gig',N'Cái',   1500000,  1890000,  1, GETUTCDATE()),
        (90, 8, 1, 'TN-NT-GS308E',  N'Netgear GS308E 8-Port',     'netgear-gs308e-8-port',     N'Cái',   900000,   1190000,  1, GETUTCDATE()),
        -- Âm thanh (5)
        (91, 9, 1, 'AM-AP-APMX',    N'AirPods Max USB-C',          'airpods-max-usb-c',         N'Cái',   11000000, 13490000, 1, GETUTCDATE()),
        (92, 9, 1, 'AM-AP-APP2',    N'AirPods Pro 2 USB-C',       'airpods-pro-2-usb-c',       N'Cái',   5000000,  5990000,  1, GETUTCDATE()),
        (93, 9, 1, 'AM-SS-BUD3P',   N'Samsung Galaxy Buds3 Pro',  'samsung-galaxy-buds3-pro',  N'Cái',   4500000,  5290000,  1, GETUTCDATE()),
        (94, 9, 1, 'AM-SN-WH1000',  N'Sony WH-1000XM5',           'sony-wh-1000xm5',          N'Cái',   6500000,  7990000,  1, GETUTCDATE()),
        (95, 9, 1, 'AM-JBL-T770',   N'JBL Tune 770NC',            'jbl-tune-770nc',            N'Cái',   1500000,  1890000,  1, GETUTCDATE()),
        -- Linh kiện (5)
        (96,  10, 1, 'LK-SS-SSD1T',  N'Samsung 990 EVO SSD 1TB',  'samsung-990-evo-ssd-1tb',   N'Cái',   2500000,  2990000,  1, GETUTCDATE()),
        (97,  10, 1, 'LK-KS-RAM16',  N'Kingston Fury 16GB DDR5',  'kingston-fury-16gb-ddr5',   N'Thanh', 1200000,  1490000,  1, GETUTCDATE()),
        (98,  10, 1, 'LK-WD-HDD2T',  N'WD Blue 2TB HDD',          'wd-blue-2tb-hdd',           N'Cái',   1200000,  1490000,  1, GETUTCDATE()),
        (99,  10, 1, 'LK-CM-HYPER',  N'Cooler Master Hyper 212',   'cooler-master-hyper-212',   N'Cái',   700000,   890000,   1, GETUTCDATE()),
        (100, 10, 1, 'LK-CS-RM750E', N'Corsair RM750e PSU',        'corsair-rm750e-psu',        N'Cái',   2200000,  2690000,  1, GETUTCDATE());
    SET IDENTITY_INSERT [Products] OFF;
    PRINT N'   → 100 Products đã tạo.';
END

-- ==========================================================================
-- 10. WAREHOUSES
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Warehouses] WHERE Code = 'WH-HCM01')
BEGIN
    SET IDENTITY_INSERT [Warehouses] ON;
    INSERT INTO [Warehouses] (Id, Code, Name, Address, Phone, ManagerId, BranchId, IsActive, CreatedAt) VALUES
        (1, 'WH-HCM01', N'Kho chính TP.HCM',    N'KCN Tân Bình, TP.HCM',              '0901234567', 21, 1, 1, GETUTCDATE()),
        (2, 'WH-HCM02', N'Kho phụ Quận 7',      N'123 Nguyễn Thị Thập, Q7, TP.HCM',   '0901234568', 22, 1, 1, GETUTCDATE()),
        (3, 'WH-HN01',  N'Kho Hà Nội',          N'KCN Sài Đồng, Long Biên, Hà Nội',   '0241234568', NULL, 2, 1, GETUTCDATE()),
        (4, 'WH-DN01',  N'Kho Đà Nẵng',         N'KCN Hòa Khánh, Liên Chiểu, ĐN',     '0361234568', NULL, 3, 1, GETUTCDATE());
    SET IDENTITY_INSERT [Warehouses] OFF;
    PRINT N'   → 4 Warehouses đã tạo.';
END

-- ==========================================================================
-- 11. CUSTOMERS (20 khách hàng mẫu)
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Customers] WHERE Code = 'KH001')
BEGIN
    SET IDENTITY_INSERT [Customers] ON;
    INSERT INTO [Customers] (Id, Code, FullName, Phone, Email, Address, TaxCode, CustomerType, IsActive, CreatedAt) VALUES
        (1,  'KH001', N'Công ty TNHH ABC Tech',         '0281234001', 'contact@abctech.vn',     N'45 Nguyễn Huệ, Q1, HCM',           '0101234567', 'Company',    1, GETUTCDATE()),
        (2,  'KH002', N'Công ty CP Minh Đức',            '0281234002', 'info@minhduc.com.vn',    N'78 Trần Hưng Đạo, Q1, HCM',         '0102345678', 'Company',    1, GETUTCDATE()),
        (3,  'KH003', N'Nguyễn Hoài Nam',                '0912345001', 'nam.nh@gmail.com',       N'12 Bùi Viện, Q1, HCM',              NULL,          'Individual', 1, GETUTCDATE()),
        (4,  'KH004', N'Trần Thị Lan Anh',               '0912345002', 'lananh.tt@gmail.com',    N'34 Võ Thị Sáu, Q3, HCM',            NULL,          'Individual', 1, GETUTCDATE()),
        (5,  'KH005', N'Công ty TNHH Thế Giới Số',       '0281234003', 'sales@thegioi-so.vn',    N'56 Lê Lợi, Q1, HCM',                '0103456789', 'Company',    1, GETUTCDATE()),
        (6,  'KH006', N'Công ty CP Phú Thịnh',           '0281234004', 'info@phuthinh.com.vn',   N'89 CMT8, Q10, HCM',                 '0104567890', 'Company',    1, GETUTCDATE()),
        (7,  'KH007', N'Lê Minh Tuấn',                   '0912345003', 'tuan.lm@yahoo.com',      N'23 Nguyễn Trãi, Q5, HCM',           NULL,          'Individual', 1, GETUTCDATE()),
        (8,  'KH008', N'Phạm Quốc Huy',                  '0912345004', 'huy.pq@outlook.com',     N'45 Điện Biên Phủ, Q3, HCM',         NULL,          'Individual', 1, GETUTCDATE()),
        (9,  'KH009', N'Công ty TNHH Đại Phát',          '0281234005', 'order@daiphat.vn',       N'67 Hai Bà Trưng, Q1, HCM',          '0105678901', 'Company',    1, GETUTCDATE()),
        (10, 'KH010', N'Công ty CP Hưng Thịnh IT',       '0281234006', 'sales@hungthinh-it.vn',  N'89 Pasteur, Q1, HCM',               '0106789012', 'Company',    1, GETUTCDATE()),
        (11, 'KH011', N'Hoàng Văn Thành',                '0912345005', 'thanh.hv@gmail.com',     N'12 Bà Triệu, Hoàn Kiếm, HN',       NULL,          'Individual', 1, GETUTCDATE()),
        (12, 'KH012', N'Đỗ Thị Hồng Nhung',             '0912345006', 'nhung.dh@gmail.com',     N'34 Tràng Tiền, Hoàn Kiếm, HN',     NULL,          'Individual', 1, GETUTCDATE()),
        (13, 'KH013', N'Công ty CP FPT Retail',          '0241234567', 'b2b@fptretail.com.vn',   N'Cầu Giấy, HN',                     '0107890123', 'Company',    1, GETUTCDATE()),
        (14, 'KH014', N'Công ty TNHH Sao Việt',          '0241234568', 'info@saoviet.vn',        N'78 Lý Thường Kiệt, HBT, HN',       '0108901234', 'Company',    1, GETUTCDATE()),
        (15, 'KH015', N'Vũ Đình Cường',                  '0912345007', 'cuong.vd@gmail.com',     N'56 Hoàng Hoa Thám, ĐĐ, HN',        NULL,          'Individual', 1, GETUTCDATE()),
        (16, 'KH016', N'Bùi Thị Mai Hương',              '0912345008', 'huong.btm@gmail.com',    N'90 Kim Mã, Ba Đình, HN',            NULL,          'Individual', 1, GETUTCDATE()),
        (17, 'KH017', N'Công ty TNHH IT Solutions ĐN',   '0361234567', 'sales@itsol-dn.vn',      N'23 Trần Phú, Hải Châu, ĐN',         '0109012345', 'Company',    1, GETUTCDATE()),
        (18, 'KH018', N'Phan Văn Hùng',                  '0912345009', 'hung.pv@gmail.com',      N'45 Bạch Đằng, Hải Châu, ĐN',       NULL,          'Individual', 1, GETUTCDATE()),
        (19, 'KH019', N'Công ty CP Bình Minh Tech',      '0361234568', 'info@binhminh-tech.vn',  N'67 Nguyễn Văn Linh, HC, ĐN',        '0110123456', 'Company',    1, GETUTCDATE()),
        (20, 'KH020', N'Ngô Thanh Tùng',                 '0912345010', 'tung.nt@gmail.com',      N'89 Hùng Vương, HC, ĐN',             NULL,          'Individual', 1, GETUTCDATE());
    SET IDENTITY_INSERT [Customers] OFF;
    PRINT N'   → 20 Customers đã tạo.';
END

-- ==========================================================================
-- 12. TAGS (CMS)
-- ==========================================================================

IF NOT EXISTS (SELECT 1 FROM [Tags] WHERE Slug = 'cong-nghe')
BEGIN
    SET IDENTITY_INSERT [Tags] ON;
    INSERT INTO [Tags] (Id, Name, Slug, UsageCount, IsActive, CreatedAt) VALUES
        (1, N'Công nghệ',    'cong-nghe',     0, 1, GETUTCDATE()),
        (2, N'Khuyến mãi',   'khuyen-mai',    0, 1, GETUTCDATE()),
        (3, N'Sản phẩm mới', 'san-pham-moi',  0, 1, GETUTCDATE()),
        (4, N'Hướng dẫn',    'huong-dan',     0, 1, GETUTCDATE()),
        (5, N'Tin tức',       'tin-tuc',       0, 1, GETUTCDATE()),
        (6, N'Đánh giá',     'danh-gia',      0, 1, GETUTCDATE()),
        (7, N'Mẹo vặt',      'meo-vat',       0, 1, GETUTCDATE()),
        (8, N'So sánh',       'so-sanh',       0, 1, GETUTCDATE());
    SET IDENTITY_INSERT [Tags] OFF;
    PRINT N'   → 8 Tags đã tạo.';
END

PRINT N'=== SEED DATA: Hoàn tất ===';

COMMIT TRANSACTION;
GO
