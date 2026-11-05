-- =========================================================================================
-- ERP & HRM Enterprise System - V6_03: NẠP DỮ LIỆU MẪU 200 NHÂN VIÊN THỰC TẾ
-- Tương thích: Microsoft SQL Server 2017+ / Azure SQL Database
-- Tính chất: Idempotent (chạy nhiều lần an toàn, không trùng lặp mã nhân sự)
-- =========================================================================================

SET NOCOUNT ON;
PRINT N'===> [BẮT ĐẦU] Nạp dữ liệu 200 Nhân sự mẫu thực tế cho hệ thống...';

-- 1. Đảm bảo các Phòng Ban chuẩn tồn tại
DECLARE @BranchHnId INT = (SELECT TOP 1 [Id] FROM [dbo].[Branches] WHERE [Code] = N'CN-HN');
DECLARE @BranchHcmId INT = (SELECT TOP 1 [Id] FROM [dbo].[Branches] WHERE [Code] = N'CN-HCM');

IF @BranchHnId IS NULL
BEGIN
    DECLARE @CompanyId INT = (SELECT TOP 1 [Id] FROM [dbo].[Companies]);
    IF @CompanyId IS NULL
    BEGIN
        INSERT INTO [dbo].[Companies] ([Code], [Name], [TaxCode], [IsActive], [CreatedAt])
        VALUES (N'TIENPHONG', N'Công ty Cổ phần Công nghệ & Thương mại Tiên Phong', N'0109998888', 1, GETDATE());
        SET @CompanyId = SCOPE_IDENTITY();
    END

    INSERT INTO [dbo].[Branches] ([CompanyId], [Code], [Name], [Address], [IsActive], [CreatedAt])
    VALUES (@CompanyId, N'CN-HN', N'Chi nhánh Hà Nội (Trụ sở chính)', N'Số 1 Duy Tân, Cầu Giấy, Hà Nội', 1, GETDATE());
    SET @BranchHnId = SCOPE_IDENTITY();
END

IF @BranchHcmId IS NULL
BEGIN
    INSERT INTO [dbo].[Branches] ([CompanyId], [Code], [Name], [Address], [IsActive], [CreatedAt])
    VALUES (@CompanyId, N'CN-HCM', N'Chi nhánh TP. Hồ Chí Minh', N'123 Nguyễn Thị Minh Khai, Q1, TP. HCM', 1, GETDATE());
    SET @BranchHcmId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Code] = N'PB-IT')
    INSERT INTO [dbo].[Departments] ([BranchId], [Code], [Name], [IsActive], [CreatedAt]) VALUES (@BranchHnId, N'PB-IT', N'Phòng Công nghệ & Hệ thống', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Code] = N'PB-HR')
    INSERT INTO [dbo].[Departments] ([BranchId], [Code], [Name], [IsActive], [CreatedAt]) VALUES (@BranchHnId, N'PB-HR', N'Phòng Nhân sự & Đào tạo', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Code] = N'PB-KD')
    INSERT INTO [dbo].[Departments] ([BranchId], [Code], [Name], [IsActive], [CreatedAt]) VALUES (@BranchHnId, N'PB-KD', N'Phòng Kinh doanh & Bán lẻ', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Code] = N'PB-KHO')
    INSERT INTO [dbo].[Departments] ([BranchId], [Code], [Name], [IsActive], [CreatedAt]) VALUES (@BranchHnId, N'PB-KHO', N'Phòng Quản lý Kho vận', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Code] = N'PB-MKT')
    INSERT INTO [dbo].[Departments] ([BranchId], [Code], [Name], [IsActive], [CreatedAt]) VALUES (@BranchHnId, N'PB-MKT', N'Phòng Marketing & Truyền Thông', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Code] = N'PB-KT')
    INSERT INTO [dbo].[Departments] ([BranchId], [Code], [Name], [IsActive], [CreatedAt]) VALUES (@BranchHnId, N'PB-KT', N'Phòng Tài Chính - Kế Toán', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Code] = N'PB-CSKH')
    INSERT INTO [dbo].[Departments] ([BranchId], [Code], [Name], [IsActive], [CreatedAt]) VALUES (@BranchHnId, N'PB-CSKH', N'Phòng Chăm Sóc Khách Hàng', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Code] = N'PB-CN-HCM')
    INSERT INTO [dbo].[Departments] ([BranchId], [Code], [Name], [IsActive], [CreatedAt]) VALUES (@BranchHcmId, N'PB-CN-HCM', N'Chi Nhánh Miền Nam', 1, GETDATE());

-- 2. Đảm bảo các Chức danh chuẩn tồn tại
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'CEO')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'CEO', N'Tổng Giám Đốc', 50000000, 0.50, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'TP-HR')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'TP-HR', N'Trưởng Phòng Nhân Sự', 25000000, 0.20, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'TP-IT')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'TP-IT', N'Trưởng Phòng Công Nghệ', 30000000, 0.25, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'DEV-SR')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'DEV-SR', N'Kỹ Sư Phần Mềm Cao Cấp', 24000000, 0.15, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'DEV-JR')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'DEV-JR', N'Kỹ Sư Phần Mềm', 14000000, 0.10, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'TP-KD')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'TP-KD', N'Trưởng Phòng Kinh Doanh', 26000000, 0.25, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'NV-KD')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'NV-KD', N'Nhân Viên Kinh Doanh', 12000000, 0.10, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'NV-KHO')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'NV-KHO', N'Thủ Kho Chuyên Trách', 10000000, 0.10, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'TP-KT')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'TP-KT', N'Kế Toán Trưởng', 25000000, 0.20, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'NV-KT')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'NV-KT', N'Chuyên Viên Kế Toán', 13000000, 0.10, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'TP-MKT')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'TP-MKT', N'Trưởng Phòng Marketing', 23000000, 0.20, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'NV-MKT')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'NV-MKT', N'Chuyên Viên Marketing', 12000000, 0.10, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'NV-HR')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'NV-HR', N'Chuyên Viên Tuyển Dụng & C&B', 13000000, 0.10, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [dbo].[Positions] WHERE [Code] = N'NV-CSKH')
    INSERT INTO [dbo].[Positions] ([Code], [Name], [BaseSalary], [AllowanceRate], [IsActive], [CreatedAt]) VALUES (N'NV-CSKH', N'Chuyên Viên Chăm Sóc Khách Hàng', 11000000, 0.08, 1, GETDATE());

-- 3. Đảm bảo Loại nghỉ phép năm tồn tại
IF NOT EXISTS (SELECT 1 FROM [dbo].[LeaveTypes] WHERE [Code] = N'ANNUAL_LEAVE')
BEGIN
    INSERT INTO [dbo].[LeaveTypes] ([Code], [Name], [MaxDaysPerYear], [IsPaid], [AllowHalfDay], [Description], [IsActive], [CreatedAt], [IsDeleted])
    VALUES (N'ANNUAL_LEAVE', N'Nghỉ phép năm', 12, 1, 1, N'Nghỉ phép năm hưởng nguyên lương theo Bộ luật Lao động', 1, GETDATE(), 0);
END
DECLARE @AnnualLeaveTypeId INT = (SELECT TOP 1 [Id] FROM [dbo].[LeaveTypes] WHERE [Code] = N'ANNUAL_LEAVE');

-- 4. Vòng lặp sinh dữ liệu 200 nhân viên
DECLARE @DeptCount INT = (SELECT COUNT(*) FROM [dbo].[Departments]);
DECLARE @PosCount INT = (SELECT COUNT(*) FROM [dbo].[Positions]);

DECLARE @i INT = 1;
WHILE @i <= 200
BEGIN
    DECLARE @EmpCode NVARCHAR(50) = N'NV' + RIGHT('000' + CAST(@i AS VARCHAR(10)), 3);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [EmployeeCode] = @EmpCode)
    BEGIN
        -- Họ & Tên
        DECLARE @LastName NVARCHAR(50);
        SET @LastName = CASE (@i % 20)
            WHEN 0 THEN N'Trịnh' WHEN 1 THEN N'Nguyễn' WHEN 2 THEN N'Trần' WHEN 3 THEN N'Lê' WHEN 4 THEN N'Phạm'
            WHEN 5 THEN N'Hoàng' WHEN 6 THEN N'Huỳnh' WHEN 7 THEN N'Phan' WHEN 8 THEN N'Vũ' WHEN 9 THEN N'Võ'
            WHEN 10 THEN N'Đặng' WHEN 11 THEN N'Bùi' WHEN 12 THEN N'Đỗ' WHEN 13 THEN N'Hồ' WHEN 14 THEN N'Ngô'
            WHEN 15 THEN N'Dương' WHEN 16 THEN N'Lý' WHEN 17 THEN N'Đào' WHEN 18 THEN N'Đoàn' ELSE N'Mai' END;

        DECLARE @IsMale BIT = CASE WHEN (@i % 2 = 1) THEN 1 ELSE 0 END;
        DECLARE @Gender NVARCHAR(10) = CASE WHEN @IsMale = 1 THEN N'Nam' ELSE N'Nữ' END;
        DECLARE @MiddleFirst NVARCHAR(100);

        IF @IsMale = 1
        BEGIN
            SET @MiddleFirst = CASE ((@i / 2) % 20)
                WHEN 0 THEN N'Nhật Minh' WHEN 1 THEN N'Văn Hùng' WHEN 2 THEN N'Minh Tuấn' WHEN 3 THEN N'Hoàng Long'
                WHEN 4 THEN N'Quang Huy' WHEN 5 THEN N'Tuấn Anh' WHEN 6 THEN N'Đức Thắng' WHEN 7 THEN N'Quốc Bảo'
                WHEN 8 THEN N'Hữu Thắng' WHEN 9 THEN N'Tiến Dũng' WHEN 10 THEN N'Trọng Nghĩa' WHEN 11 THEN N'Đình Trọng'
                WHEN 12 THEN N'Văn Nam' WHEN 13 THEN N'Hải Đăng' WHEN 14 THEN N'Thành Đạt' WHEN 15 THEN N'Việt Anh'
                WHEN 16 THEN N'Xuân Bách' WHEN 17 THEN N'Minh Triết' WHEN 18 THEN N'Phúc Thịnh' ELSE N'Duy Khang' END;
        END
        ELSE
        BEGIN
            SET @MiddleFirst = CASE ((@i / 2) % 20)
                WHEN 0 THEN N'Bảo Trâm' WHEN 1 THEN N'Thị Mai' WHEN 2 THEN N'Thị Hương' WHEN 3 THEN N'Thu Hà'
                WHEN 4 THEN N'Ngọc Anh' WHEN 5 THEN N'Thùy Linh' WHEN 6 THEN N'Thanh Huyền' WHEN 7 THEN N'Bích Ngọc'
                WHEN 8 THEN N'Phương Thảo' WHEN 9 THEN N'Mỹ Linh' WHEN 10 THEN N'Khánh Vy' WHEN 11 THEN N'Lan Anh'
                WHEN 12 THEN N'Quỳnh Nga' WHEN 13 THEN N'Diệu Linh' WHEN 14 THEN N'Hoài Thương' WHEN 15 THEN N'Kim Oanh'
                WHEN 16 THEN N'Tuyết Mai' WHEN 17 THEN N'Ánh Tuyết' WHEN 18 THEN N'Cẩm Tú' ELSE N'Thảo Nguyên' END;
        END

        DECLARE @FullName NVARCHAR(150) = @LastName + N' ' + @MiddleFirst;
        DECLARE @Email NVARCHAR(150) = LOWER(N'nv' + RIGHT('000' + CAST(@i AS VARCHAR(10)), 3) + N'@tienphong.vn');
        DECLARE @Phone NVARCHAR(20) = N'09' + CAST(80000000 + @i AS VARCHAR(20));
        DECLARE @IdentityCard NVARCHAR(30) = N'enc:v1:0' + RIGHT('00000000000' + CAST(@i AS VARCHAR(20)), 11);

        -- Lựa chọn Phòng ban & Chức vụ
        DECLARE @DeptId INT;
        SELECT @DeptId = [Id] FROM (
            SELECT [Id], ROW_NUMBER() OVER (ORDER BY [Id]) AS rn FROM [dbo].[Departments]
        ) t WHERE rn = ((@i % @DeptCount) + 1);

        DECLARE @PosId INT, @BaseSalary DECIMAL(18,2);
        SELECT @PosId = [Id], @BaseSalary = [BaseSalary] FROM (
            SELECT [Id], [BaseSalary], ROW_NUMBER() OVER (ORDER BY [Id]) AS rn FROM [dbo].[Positions]
        ) t WHERE rn = ((@i % @PosCount) + 1);

        -- Trạng thái & Ngày vào làm
        DECLARE @Status NVARCHAR(50);
        DECLARE @HireDate DATE;
        DECLARE @BirthDate DATE = DATEADD(YEAR, -(22 + (@i % 18)), DATEADD(DAY, (@i * 7) % 365, '2026-01-01'));

        IF (@i % 15 = 0)
        BEGIN
            SET @Status = N'Resigned';
            SET @HireDate = DATEADD(YEAR, -(2 + (@i % 3)), DATEADD(DAY, (@i * 5) % 300, '2026-01-01'));
        END
        ELSE IF (@i % 10 = 0)
        BEGIN
            SET @Status = N'Probation';
            SET @HireDate = DATEADD(DAY, -((@i % 45) + 5), '2026-10-01');
        END
        ELSE
        BEGIN
            SET @Status = N'Active';
            SET @HireDate = DATEADD(YEAR, -(1 + (@i % 4)), DATEADD(DAY, (@i * 11) % 330, '2026-01-01'));
        END

        -- Chèn Employee
        INSERT INTO [dbo].[Employees] (
            [DepartmentId], [PositionId], [EmployeeCode], [FullName], [DateOfBirth], [Gender],
            [Email], [Phone], [IdentityCard], [Status], [HireDate], [CreatedAt], [IsDeleted]
        ) VALUES (
            @DeptId, @PosId, @EmpCode, @FullName, @BirthDate, @Gender,
            @Email, @Phone, @IdentityCard, @Status, @HireDate, @HireDate, 0
        );

        DECLARE @EmpId INT = SCOPE_IDENTITY();

        -- Chèn EmployeeDetail
        DECLARE @Address NVARCHAR(300) = CASE (@i % 8)
            WHEN 0 THEN N'Cầu Giấy, Hà Nội' WHEN 1 THEN N'Đống Đa, Hà Nội' WHEN 2 THEN N'Thanh Xuân, Hà Nội'
            WHEN 3 THEN N'Quận 1, TP. HCM' WHEN 4 THEN N'Quận Bình Thạnh, TP. HCM' WHEN 5 THEN N'Hải Châu, Đà Nẵng'
            WHEN 6 THEN N'Ngô Quyền, Hải Phòng' ELSE N'TP. Bắc Ninh' END;

        DECLARE @BankName NVARCHAR(100) = CASE (@i % 6)
            WHEN 0 THEN N'Techcombank' WHEN 1 THEN N'Vietcombank' WHEN 2 THEN N'MB Bank'
            WHEN 3 THEN N'BIDV' WHEN 4 THEN N'VPBank' ELSE N'ACB' END;

        DECLARE @Education NVARCHAR(100) = CASE WHEN (@i % 6 = 0) THEN N'Thạc sĩ' WHEN (@i % 8 = 0) THEN N'Cao đẳng' ELSE N'Đại học' END;

        INSERT INTO [dbo].[EmployeeDetails] (
            [EmployeeId], [CurrentAddress], [BankAccountNumber], [BankName],
            [EmergencyContactName], [EmergencyContactPhone], [EducationLevel]
        ) VALUES (
            @EmpId, @Address, N'190333' + RIGHT('000000' + CAST(@i AS VARCHAR(10)), 6), @BankName,
            N'Người thân ' + @LastName, N'090' + RIGHT('0000000' + CAST(@i AS VARCHAR(10)), 7), @Education
        );

        -- Chèn Contract
        DECLARE @ContractType NVARCHAR(100);
        DECLARE @EndDate DATE = NULL;
        DECLARE @ContractStatus NVARCHAR(50) = N'Active';
        DECLARE @ActualSalary DECIMAL(18,2) = CASE WHEN @BaseSalary > 0 THEN @BaseSalary ELSE 12000000 END;

        IF @Status = N'Probation'
        BEGIN
            SET @ContractType = N'Hợp đồng thử việc 60 ngày';
            SET @EndDate = DATEADD(DAY, 60, @HireDate);
            SET @ActualSalary = ROUND(@ActualSalary * 0.85, 0);
        END
        ELSE IF @Status = N'Resigned'
        BEGIN
            SET @ContractType = N'Hợp đồng đã thanh lý';
            SET @ContractStatus = N'Terminated';
            SET @EndDate = DATEADD(YEAR, 1, @HireDate);
        END
        ELSE
        BEGIN
            IF (@i % 3 = 0)
            BEGIN
                SET @ContractType = N'Hợp đồng lao động không thời hạn';
                SET @EndDate = NULL;
            END
            ELSE
            BEGIN
                SET @ContractType = N'Hợp đồng lao động xác định thời hạn 12 tháng';
                SET @EndDate = DATEADD(YEAR, 1, @HireDate);
            END
        END

        INSERT INTO [dbo].[Contracts] (
            [EmployeeId], [ContractNumber], [ContractType], [StartDate], [EndDate],
            [BasicSalary], [InsuranceSalary], [Status], [SignDate], [CreatedAt]
        ) VALUES (
            @EmpId, N'HDLD-' + @EmpCode, @ContractType, @HireDate, @EndDate,
            @ActualSalary, CASE WHEN @ActualSalary > 20000000 THEN 20000000 ELSE @ActualSalary END,
            @ContractStatus, @HireDate, @HireDate
        );

        -- Chèn EmployeeLeaveBalance (nếu bảng tồn tại)
        IF OBJECT_ID(N'dbo.EmployeeLeaveBalances', N'U') IS NOT NULL AND @AnnualLeaveTypeId IS NOT NULL
        BEGIN
            DECLARE @SeniorityBonus DECIMAL(5,2) = CASE WHEN DATEDIFF(YEAR, @HireDate, '2026-10-01') >= 5 THEN 1.00 ELSE 0.00 END;
            DECLARE @UsedDays DECIMAL(5,2) = CASE WHEN @Status = N'Probation' THEN 0.00 ELSE CAST((@i % 5) AS DECIMAL(5,2)) END;
            DECLARE @CarriedOver DECIMAL(5,2) = CASE WHEN (@i % 3 = 0) THEN 1.50 ELSE 0.00 END;
            DECLARE @Remaining DECIMAL(5,2) = 12.00 + @SeniorityBonus + @CarriedOver - @UsedDays;

            INSERT INTO [dbo].[EmployeeLeaveBalances] (
                [EmployeeId], [Year], [LeaveTypeId], [StandardDays], [SeniorityBonusDays],
                [CarriedOverDays], [UsedDays], [RemainingDays], [CreatedAt], [IsDeleted]
            ) VALUES (
                @EmpId, 2026, @AnnualLeaveTypeId, 12.00, @SeniorityBonus,
                @CarriedOver, @UsedDays, CASE WHEN @Remaining < 0 THEN 0.00 ELSE @Remaining END, @HireDate, 0
            );
        END
    END

    SET @i = @i + 1;
END

DECLARE @TotalEmployees INT = (SELECT COUNT(*) FROM [dbo].[Employees]);
PRINT N'===> [HOÀN TẤT] Tổng số nhân sự hiện có trong hệ thống: ' + CAST(@TotalEmployees AS NVARCHAR(10)) + N' nhân sự.';

