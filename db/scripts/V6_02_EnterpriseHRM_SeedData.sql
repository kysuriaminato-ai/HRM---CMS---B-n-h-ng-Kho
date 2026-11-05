-- ==========================================================================================
-- PHIÊN 2: DỮ LIỆU MẪU CHUẨN DOANH NGHIỆP ENTERPRISE HRM (SSMS)
-- Script: db/scripts/V6_02_EnterpriseHRM_SeedData.sql
-- Ngày: 2026-11-05
-- Tác giả: Enterprise SQL Server DBA & EF Core Architect
-- Mô tả:
--   1. Danh mục Loại nghỉ phép chuẩn Bộ luật Lao động (LeaveTypes).
--   2. Quỹ phép năm (EmployeeLeaveBalances) cho nhân viên năm 2026.
--   3. 6 Tin tuyển dụng chuẩn SEO kèm Slug (JobPostings).
--   4. 18 Hồ sơ ứng viên đủ 6 cột Kanban (Applied, Screening, Interview, Offered, Hired, Rejected).
--   5. Lịch phỏng vấn chi tiết theo từng vòng (InterviewSchedules).
--   6. Danh mục tài sản công ty (CompanyAssets) & Cấp phát nhân sự (EmployeeAssetAssignments).
--   7. Quyết định Nhân sự điện tử (HrDecisions): Bổ nhiệm, Điều chuyển, Tăng lương, Khen thưởng.
--   8. Khóa kỳ chấm công (AttendanceLocks) & Đơn giải trình chấm công (AttendanceAdjustmentRequests).
--   9. Quy trình Hội nhập Onboarding & Bàn giao Offboarding (OnboardingChecklists / Tasks).
-- Lưu ý: Hoàn toàn IDEMPOTENT - chạy an toàn nhiều lần không nhân bản dữ liệu.
-- ==========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=== [V6_02] Bắt đầu nạp Dữ liệu Mẫu Chuẩn Doanh nghiệp Enterprise HRM ===';
GO

-- ==========================================================================================
-- 1. DANH MỤC LOẠI NGHỈ PHÉP CHUẨN (LEAVETYPES)
-- ==========================================================================================
PRINT N'1. Đồng bộ danh mục loại nghỉ phép chuẩn...';

IF NOT EXISTS (SELECT 1 FROM [dbo].[LeaveTypes] WHERE [Code] = N'ANNUAL_LEAVE')
    INSERT INTO [dbo].[LeaveTypes] ([Code], [Name], [MaxDaysPerYear], [IsPaid], [AllowHalfDay], [RequiresAttachment], [Description], [IsActive], [CreatedAt], [IsDeleted])
    VALUES (N'ANNUAL_LEAVE', N'Nghỉ phép năm', 12, 1, 1, 0, N'Nghỉ phép năm hưởng nguyên lương theo Bộ luật Lao động', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[LeaveTypes] WHERE [Code] = N'SICK_LEAVE')
    INSERT INTO [dbo].[LeaveTypes] ([Code], [Name], [MaxDaysPerYear], [IsPaid], [AllowHalfDay], [RequiresAttachment], [Description], [IsActive], [CreatedAt], [IsDeleted])
    VALUES (N'SICK_LEAVE', N'Nghỉ ốm đau', 30, 0, 1, 1, N'Nghỉ ốm có giấy chứng nhận nghỉ việc hưởng BHXH (C65-HD)', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[LeaveTypes] WHERE [Code] = N'MATERNITY_LEAVE')
    INSERT INTO [dbo].[LeaveTypes] ([Code], [Name], [MaxDaysPerYear], [IsPaid], [AllowHalfDay], [RequiresAttachment], [Description], [IsActive], [CreatedAt], [IsDeleted])
    VALUES (N'MATERNITY_LEAVE', N'Nghỉ thai sản', 180, 0, 0, 1, N'Nghỉ sinh con theo chế độ thai sản của cơ quan BHXH', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[LeaveTypes] WHERE [Code] = N'WEDDING_LEAVE')
    INSERT INTO [dbo].[LeaveTypes] ([Code], [Name], [MaxDaysPerYear], [IsPaid], [AllowHalfDay], [RequiresAttachment], [Description], [IsActive], [CreatedAt], [IsDeleted])
    VALUES (N'WEDDING_LEAVE', N'Nghỉ kết hôn', 3, 1, 0, 1, N'Nghỉ bản thân kết hôn hưởng nguyên lương (Điều 115 BLLĐ)', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[LeaveTypes] WHERE [Code] = N'FUNERAL_LEAVE')
    INSERT INTO [dbo].[LeaveTypes] ([Code], [Name], [MaxDaysPerYear], [IsPaid], [AllowHalfDay], [RequiresAttachment], [Description], [IsActive], [CreatedAt], [IsDeleted])
    VALUES (N'FUNERAL_LEAVE', N'Nghỉ tang chế', 3, 1, 0, 0, N'Tứ thân phụ mẫu, vợ/chồng, con mất hưởng nguyên lương', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[LeaveTypes] WHERE [Code] = N'UNPAID_LEAVE')
    INSERT INTO [dbo].[LeaveTypes] ([Code], [Name], [MaxDaysPerYear], [IsPaid], [AllowHalfDay], [RequiresAttachment], [Description], [IsActive], [CreatedAt], [IsDeleted])
    VALUES (N'UNPAID_LEAVE', N'Nghỉ không lương', 0, 0, 1, 0, N'Nghỉ việc riêng không hưởng lương theo thỏa thuận', 1, SYSUTCDATETIME(), 0);

PRINT N'[OK] Đã kiểm tra 6 loại nghỉ phép chuẩn.';
GO

-- ==========================================================================================
-- 2. QUỸ PHÉP NĂM NHÂN VIÊN (EMPLOYEELEAVEBALANCES NĂM 2026)
-- ==========================================================================================
PRINT N'2. Khởi tạo quỹ phép năm 2026 cho nhân sự...';

DECLARE @AnnualLeaveTypeId INT = (SELECT TOP 1 [Id] FROM [dbo].[LeaveTypes] WHERE [Code] = N'ANNUAL_LEAVE');

IF @AnnualLeaveTypeId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[EmployeeLeaveBalances] (
        [EmployeeId], [Year], [LeaveTypeId], [StandardDays], [SeniorityBonusDays],
        [CarriedOverDays], [UsedDays], [RemainingDays], [ExpiredCarryOverDate], [Note], [CreatedAt], [IsDeleted]
    )
    SELECT
        e.[Id],
        2026,
        @AnnualLeaveTypeId,
        12.00,
        CASE
            WHEN DATEDIFF(YEAR, e.[HireDate], '2026-01-01') >= 15 THEN 3.00
            WHEN DATEDIFF(YEAR, e.[HireDate], '2026-01-01') >= 10 THEN 2.00
            WHEN DATEDIFF(YEAR, e.[HireDate], '2026-01-01') >= 5 THEN 1.00
            ELSE 0.00
        END AS [SeniorityBonusDays],
        2.00 AS [CarriedOverDays],
        1.50 AS [UsedDays],
        (12.00 +
            CASE
                WHEN DATEDIFF(YEAR, e.[HireDate], '2026-01-01') >= 15 THEN 3.00
                WHEN DATEDIFF(YEAR, e.[HireDate], '2026-01-01') >= 10 THEN 2.00
                WHEN DATEDIFF(YEAR, e.[HireDate], '2026-01-01') >= 5 THEN 1.00
                ELSE 0.00
            END + 2.00 - 1.50) AS [RemainingDays],
        '2026-03-31T23:59:59' AS [ExpiredCarryOverDate],
        N'Quỹ phép thường niên năm 2026 (hạn phép tồn 31/03/2026)' AS [Note],
        SYSUTCDATETIME(),
        0
    FROM [dbo].[Employees] e
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[EmployeeLeaveBalances] elb
        WHERE elb.[EmployeeId] = e.[Id]
          AND elb.[Year] = 2026
          AND elb.[LeaveTypeId] = @AnnualLeaveTypeId
    );

    PRINT N'[OK] Đã cập nhật Quỹ phép năm 2026 cho các nhân sự hiện hành.';
END
GO

-- ==========================================================================================
-- 3. 6 TIN TUYỂN DỤNG ĐA PHÒNG BAN CHUẨN SEO (JOBPOSTINGS)
-- ==========================================================================================
PRINT N'3. Tạo 6 tin tuyển dụng chuẩn SEO...';

DECLARE @DeptIT INT = (SELECT TOP 1 [Id] FROM [dbo].[Departments] WHERE [Code] LIKE '%IT%' ORDER BY [Id]);
DECLARE @DeptHR INT = (SELECT TOP 1 [Id] FROM [dbo].[Departments] WHERE [Code] LIKE '%HR%' ORDER BY [Id]);
DECLARE @DeptKD INT = (SELECT TOP 1 [Id] FROM [dbo].[Departments] WHERE [Code] LIKE '%KD%' ORDER BY [Id]);
DECLARE @DeptKHO INT = (SELECT TOP 1 [Id] FROM [dbo].[Departments] WHERE [Code] LIKE '%KHO%' ORDER BY [Id]);

-- Fallback phòng ban
IF @DeptIT IS NULL SET @DeptIT = (SELECT TOP 1 [Id] FROM [dbo].[Departments] ORDER BY [Id]);
IF @DeptHR IS NULL SET @DeptHR = @DeptIT;
IF @DeptKD IS NULL SET @DeptKD = @DeptIT;
IF @DeptKHO IS NULL SET @DeptKHO = @DeptIT;

DECLARE @PosCEO INT = (SELECT TOP 1 [Id] FROM [dbo].[Positions] WHERE [Code] LIKE '%CEO%' OR [Code] LIKE '%GD%' ORDER BY [Id]);
DECLARE @PosTP INT = (SELECT TOP 1 [Id] FROM [dbo].[Positions] WHERE [Code] LIKE '%TP%' ORDER BY [Id]);
DECLARE @PosNV INT = (SELECT TOP 1 [Id] FROM [dbo].[Positions] WHERE [Code] LIKE '%NV%' ORDER BY [Id]);

IF @PosTP IS NULL SET @PosTP = (SELECT TOP 1 [Id] FROM [dbo].[Positions] ORDER BY [Id]);
IF @PosNV IS NULL SET @PosNV = @PosTP;

-- 3.1. Trưởng phòng Kinh doanh Doanh nghiệp
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobPostings] WHERE [Code] = N'TP-KD-2026')
    INSERT INTO [dbo].[JobPostings] ([Code], [Title], [Slug], [DepartmentId], [PositionId], [Vacancies], [Description], [Requirements], [SalaryRange], [WorkLocation], [Status], [OpenDate], [CloseDate], [CreatedAt], [IsDeleted])
    VALUES (
        N'TP-KD-2026',
        N'Trưởng phòng Kinh doanh Doanh nghiệp (B2B)',
        N'truong-phong-kinh-doanh-doanh-nghiep',
        @DeptKD, @PosTP, 1,
        N'Xây dựng chiến lược mở rộng thị trường B2B, quản lý đội ngũ 15 nhân viên kinh doanh, cam kết KPI doanh thu năm.',
        N'Tối thiểu 3 năm kinh nghiệm quản lý sales B2B phần mềm/bán lẻ; kỹ năng đàm phán cấp cao.',
        N'25 - 40 triệu + Thưởng KPI doanh số',
        N'Trụ sở chính Hà Nội',
        N'Open',
        '2026-10-01', '2026-12-31', SYSUTCDATETIME(), 0
    );

-- 3.2. Kỹ sư Cầu nối & Lập trình viên Senior .NET Core
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobPostings] WHERE [Code] = N'DEV-DOTNET-2026')
    INSERT INTO [dbo].[JobPostings] ([Code], [Title], [Slug], [DepartmentId], [PositionId], [Vacancies], [Description], [Requirements], [SalaryRange], [WorkLocation], [Status], [OpenDate], [CloseDate], [CreatedAt], [IsDeleted])
    VALUES (
        N'DEV-DOTNET-2026',
        N'Kỹ sư Cầu nối & Lập trình viên Senior .NET Core',
        N'ky-su-cau-noi-lap-trinh-vien-senior-dotnet',
        @DeptIT, @PosNV, 2,
        N'Phát triển lõi hệ thống ERP/HRM/CMS Clean Architecture, tối ưu SQL Server, Serilog, Redis Cache và Microservices.',
        N'Thành thạo C# .NET 8, EF Core, SQL Server High Performance, Clean Architecture; tư duy bảo mật vững vàng.',
        N'25 - 35 triệu',
        N'Trụ sở chính Hà Nội / Hybrid',
        N'Open',
        '2026-10-05', '2026-11-30', SYSUTCDATETIME(), 0
    );

-- 3.3. Chuyên viên Kế toán Tổng hợp & Thuế
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobPostings] WHERE [Code] = N'KT-TH-2026')
    INSERT INTO [dbo].[JobPostings] ([Code], [Title], [Slug], [DepartmentId], [PositionId], [Vacancies], [Description], [Requirements], [SalaryRange], [WorkLocation], [Status], [OpenDate], [CloseDate], [CreatedAt], [IsDeleted])
    VALUES (
        N'KT-TH-2026',
        N'Chuyên viên Kế toán Tổng hợp & Quyết toán Thuế',
        N'chuyen-vien-ke-toan-tong-hop-thue',
        @DeptHR, @PosNV, 1,
        N'Lập báo cáo tài chính, quyết toán thuế GTGT, TNDN, TNCN; kiểm soát dòng tiền và công nợ khách hàng/nhà cung cấp.',
        N'Tốt nghiệp ĐH chuyên ngành Kế toán/Kiểm toán, tối thiểu 3 năm kinh nghiệm kế toán tổng hợp doanh nghiệp.',
        N'14 - 18 triệu',
        N'Chi nhánh TP. Hồ Chí Minh',
        N'Open',
        '2026-10-10', '2026-11-25', SYSUTCDATETIME(), 0
    );

-- 3.4. Chuyên viên Tuyển dụng & Đào tạo Nhân sự HRM
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobPostings] WHERE [Code] = N'HR-REC-2026')
    INSERT INTO [dbo].[JobPostings] ([Code], [Title], [Slug], [DepartmentId], [PositionId], [Vacancies], [Description], [Requirements], [SalaryRange], [WorkLocation], [Status], [OpenDate], [CloseDate], [CreatedAt], [IsDeleted])
    VALUES (
        N'HR-REC-2026',
        N'Chuyên viên Tuyển dụng & Đào tạo Nhân sự HRM',
        N'chuyen-vien-tuyen-dung-dao-tao-nhan-su',
        @DeptHR, @PosNV, 1,
        N'Tìm kiếm ứng viên, vận hành quy trình phỏng vấn Onboarding/Offboarding, tổ chức các khóa đào tạo nội bộ.',
        N'Kinh nghiệm 2 năm tuyển dụng Headhunt hoặc In-house ngành CNTT/Bán lẻ; giao tiếp tiếng Anh tốt.',
        N'12 - 16 triệu',
        N'Trụ sở chính Hà Nội',
        N'Open',
        '2026-09-15', '2026-11-15', SYSUTCDATETIME(), 0
    );

-- 3.5. Trưởng nhóm Quản trị Kho vận & Điều phối SCM
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobPostings] WHERE [Code] = N'SCM-LOG-2026')
    INSERT INTO [dbo].[JobPostings] ([Code], [Title], [Slug], [DepartmentId], [PositionId], [Vacancies], [Description], [Requirements], [SalaryRange], [WorkLocation], [Status], [OpenDate], [CloseDate], [CreatedAt], [IsDeleted])
    VALUES (
        N'SCM-LOG-2026',
        N'Trưởng nhóm Quản trị Kho vận & Điều phối SCM',
        N'truong-nhom-quan-tri-kho-van-dieu-phoi-scm',
        @DeptKHO, @PosTP, 1,
        N'Điều phối xuất nhập kho đa chi nhánh, kiểm kê tồn kho định kỳ, quản lý đơn đặt hàng NCC và tối ưu chi phí vận chuyển.',
        N'Hiểu biết sâu về chuỗi cung ứng SCM, tối thiểu 2 năm kinh nghiệm quản lý kho tổng diện tích > 2000m2.',
        N'15 - 20 triệu',
        N'Tổng kho Hà Nội & Chi nhánh Đà Nẵng',
        N'Open',
        '2026-10-01', '2026-11-20', SYSUTCDATETIME(), 0
    );

-- 3.6. Chuyên viên Marketing & Phát triển Thương hiệu
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobPostings] WHERE [Code] = N'MKT-BRAND-2026')
    INSERT INTO [dbo].[JobPostings] ([Code], [Title], [Slug], [DepartmentId], [PositionId], [Vacancies], [Description], [Requirements], [SalaryRange], [WorkLocation], [Status], [OpenDate], [CloseDate], [CreatedAt], [IsDeleted])
    VALUES (
        N'MKT-BRAND-2026',
        N'Chuyên viên Marketing & Phát triển Thương hiệu',
        N'chuyen-vien-marketing-phat-trien-thuong-hieu',
        @DeptKD, @PosNV, 2,
        N'Lên kế hoạch Digital Marketing, quản lý nội dung đa kênh, tổ chức sự kiện ra mắt tính năng phần mềm ERP.',
        N'Thành thạo SEO, Content Marketing, chạy quảng cáo Ads, sử dụng tốt các công cụ phân tích dữ liệu thị trường.',
        N'12 - 18 triệu',
        N'Trụ sở chính Hà Nội',
        N'Open',
        '2026-10-15', '2026-12-15', SYSUTCDATETIME(), 0
    );

PRINT N'[OK] Đã kiểm tra và nạp 6 tin tuyển dụng chuẩn SEO.';
GO

-- ==========================================================================================
-- 4. 18 HỒ SƠ ỨNG VIÊN ĐỦ 6 CỘT KANBAN (CANDIDATES)
-- ==========================================================================================
PRINT N'4. Tạo 18 hồ sơ ứng viên đủ 6 cột Kanban...';

DECLARE @JobDev INT = (SELECT TOP 1 [Id] FROM [dbo].[JobPostings] WHERE [Code] = N'DEV-DOTNET-2026');
DECLARE @JobSales INT = (SELECT TOP 1 [Id] FROM [dbo].[JobPostings] WHERE [Code] = N'TP-KD-2026');
DECLARE @JobAcc INT = (SELECT TOP 1 [Id] FROM [dbo].[JobPostings] WHERE [Code] = N'KT-TH-2026');
DECLARE @JobHR INT = (SELECT TOP 1 [Id] FROM [dbo].[JobPostings] WHERE [Code] = N'HR-REC-2026');

IF @JobDev IS NULL SET @JobDev = (SELECT TOP 1 [Id] FROM [dbo].[JobPostings] ORDER BY [Id]);
IF @JobSales IS NULL SET @JobSales = @JobDev;
IF @JobAcc IS NULL SET @JobAcc = @JobDev;
IF @JobHR IS NULL SET @JobHR = @JobDev;

-- 1. Cột Applied (Ứng tuyển mới) - 3 ứng viên
IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-APP-001')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-APP-001', N'Trần Đình Khang', N'khang.tran@email.vn', N'0912345671', N'/uploads/resumes/khang_tran.pdf', N'Mong muốn cống hiến cho mảng phần mềm ERP doanh nghiệp', N'Applied', '2026-10-28', NULL, @JobDev, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-APP-002')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-APP-002', N'Lê Thị Hoàng Yến', N'yen.le@email.vn', N'0912345672', N'/uploads/resumes/yen_le.pdf', N'Có kinh nghiệm 4 năm Sales B2B khối viễn thông', N'Applied', '2026-10-29', NULL, @JobSales, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-APP-003')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-APP-003', N'Vũ Ngọc Ánh', N'anh.vu@email.vn', N'0912345673', N'/uploads/resumes/anh_vu.pdf', N'Kế toán viên trẻ tốt nghiệp loại Giỏi ĐH Kinh tế Quốc dân', N'Applied', '2026-10-30', NULL, @JobAcc, SYSUTCDATETIME(), 0);

-- 2. Cột Screening (Sơ loại hồ sơ) - 3 ứng viên
IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-SCR-001')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-SCR-001', N'Đỗ Minh Quân', N'quan.do@email.vn', N'0912345674', N'/uploads/resumes/quan_do.pdf', N'CV sáng giá, kinh nghiệm Tech Lead tại công ty Outsource Nhật Bản', N'Screening', '2026-10-20', 78.50, @JobDev, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-SCR-002')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-SCR-002', N'Nguyễn Thanh Huyền', N'huyen.nguyen@email.vn', N'0912345675', N'/uploads/resumes/huyen_nguyen.pdf', N'Có mạng lưới quan hệ khách hàng doanh nghiệp B2B rộng lớn', N'Screening', '2026-10-22', 82.00, @JobSales, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-SCR-003')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-SCR-003', N'Phạm Gia Bảo', N'bao.pham@email.vn', N'0912345676', N'/uploads/resumes/bao_pham.pdf', N'Đã từng quyết toán thuế 3 kỳ cho doanh nghiệp FDI', N'Screening', '2026-10-23', 75.00, @JobAcc, SYSUTCDATETIME(), 0);

-- 3. Cột Interview (Đang phỏng vấn) - 3 ứng viên
IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-INT-001')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-INT-001', N'Hoàng Quốc Thái', N'thai.hoang@email.vn', N'0912345677', N'/uploads/resumes/thai_hoang.pdf', N'Chuyên sâu microservices, message queue và Clean Architecture', N'Interview', '2026-10-15', 88.00, @JobDev, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-INT-002')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-INT-002', N'Bùi Phương Thảo', N'thao.bui@email.vn', N'0912345678', N'/uploads/resumes/thao_bui.pdf', N'Kỹ năng thuyết trình tự tin, khả năng chốt hợp đồng lớn xuất sắc', N'Interview', '2026-10-16', 85.50, @JobSales, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-INT-003')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-INT-003', N'Đặng Thu Trang', N'trang.dang@email.vn', N'0912345679', N'/uploads/resumes/trang_dang.pdf', N'Nắm vững luật lao động, xây dựng thang bảng lương và KPI chuẩn', N'Interview', '2026-10-17', 81.00, @JobHR, SYSUTCDATETIME(), 0);

-- 4. Cột Offered (Đã gửi Offer lương) - 3 ứng viên
IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-OFF-001')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-OFF-001', N'Trịnh Văn Cường', N'cuong.trinh@email.vn', N'0912345680', N'/uploads/resumes/cuong_trinh.pdf', N'Đạt 92 điểm phỏng vấn kỹ thuật, đề xuất mức lương 32.000.000đ', N'Offered', '2026-10-10', 92.00, @JobDev, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-OFF-002')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-OFF-002', N'Phan Hồng Phúc', N'phuc.phan@email.vn', N'0912345681', N'/uploads/resumes/phuc_phan.pdf', N'Đã đồng ý thư mời nhận việc vị trí Trưởng phòng Kinh doanh', N'Offered', '2026-10-11', 90.00, @JobSales, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-OFF-003')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-OFF-003', N'Dương Thùy Linh', N'linh.duong@email.vn', N'0912345682', N'/uploads/resumes/linh_duong.pdf', N'Đã gửi Offer mức lương 17.000.000đ cho vị trí Kế toán Tổng hợp', N'Offered', '2026-10-12', 87.00, @JobAcc, SYSUTCDATETIME(), 0);

-- 5. Cột Hired (Đã tuyển dụng thành công) - 3 ứng viên
IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-HIR-001')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-HIR-001', N'Ngô Văn Đạt', N'dat.ngo@email.vn', N'0912345683', N'/uploads/resumes/dat_ngo.pdf', N'Đã hoàn tất ký HĐLĐ và nhận bàn giao thiết bị công ty', N'Hired', '2026-09-20', 94.50, @JobDev, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-HIR-002')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-HIR-002', N'Mai Tuyết Mai', N'mai.mai@email.vn', N'0912345684', N'/uploads/resumes/mai_mai.pdf', N'Đã gia nhập và phụ trách mảng đào tạo nhân sự nội bộ', N'Hired', '2026-09-25', 89.00, @JobHR, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-HIR-003')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-HIR-003', N'Đinh Tuấn Kiệt', N'kiet.dinh@email.vn', N'0912345685', N'/uploads/resumes/kiet_dinh.pdf', N'Đã onboard chi nhánh TP.HCM, tham gia vận hành kho vận', N'Hired', '2026-09-28', 88.00, @JobSales, SYSUTCDATETIME(), 0);

-- 6. Cột Rejected (Từ chối / Không phù hợp) - 3 ứng viên
IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-REJ-001')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-REJ-001', N'Lý Vĩnh Phát', N'phat.ly@email.vn', N'0912345686', N'/uploads/resumes/phat_ly.pdf', N'Kỹ năng lập trình chưa đạt yêu cầu mức độ Senior .NET', N'Rejected', '2026-10-05', 45.00, @JobDev, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-REJ-002')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-REJ-002', N'Tạ Kiều Trinh', N'trinh.ta@email.vn', N'0912345687', N'/uploads/resumes/trinh_ta.pdf', N'Kỳ vọng lương vượt khung ngân sách tuyển dụng doanh nghiệp', N'Rejected', '2026-10-06', 58.00, @JobSales, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Candidates] WHERE [Code] = N'UV-REJ-003')
    INSERT INTO [dbo].[Candidates] ([Code], [FullName], [Email], [Phone], [ResumeUrl], [CoverLetter], [Status], [AppliedDate], [EvaluationScore], [JobPostingId], [CreatedAt], [IsDeleted])
    VALUES (N'UV-REJ-003', N'Vương Đình Khải', N'khai.vuong@email.vn', N'0912345688', N'/uploads/resumes/khai_vuong.pdf', N'Không phản hồi liên hệ phỏng vấn qua điện thoại và email', N'Rejected', '2026-10-07', 30.00, @JobAcc, SYSUTCDATETIME(), 0);

PRINT N'[OK] Đã kiểm tra và nạp 18 hồ sơ ứng viên đủ 6 cột Kanban.';
GO

-- ==========================================================================================
-- 5. LỊCH PHỎNG VẤN ỨNG VIÊN (INTERVIEWSCHEDULES)
-- ==========================================================================================
PRINT N'5. Tạo lịch phỏng vấn chi tiết...';

DECLARE @CandThai INT = (SELECT TOP 1 [Id] FROM [dbo].[Candidates] WHERE [Code] = N'UV-INT-001');
DECLARE @CandThao INT = (SELECT TOP 1 [Id] FROM [dbo].[Candidates] WHERE [Code] = N'UV-INT-002');
DECLARE @CandTrang INT = (SELECT TOP 1 [Id] FROM [dbo].[Candidates] WHERE [Code] = N'UV-INT-003');
DECLARE @CandCuong INT = (SELECT TOP 1 [Id] FROM [dbo].[Candidates] WHERE [Code] = N'UV-OFF-001');

DECLARE @AdminUserId INT = (SELECT TOP 1 [Id] FROM [dbo].[Users] WHERE [Username] = 'admin');
DECLARE @FirstEmpId INT = (SELECT TOP 1 [Id] FROM [dbo].[Employees] ORDER BY [Id]);

IF @CandThai IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[InterviewSchedules] WHERE [CandidateId] = @CandThai AND [RoundNumber] = 1)
    INSERT INTO [dbo].[InterviewSchedules] ([CandidateId], [RoundNumber], [Title], [ScheduledTime], [DurationMinutes], [InterviewType], [LocationOrMeetingLink], [InterviewerEmployeeId], [InterviewerUserId], [Status], [Score], [Feedback], [Result], [CreatedAt], [IsDeleted])
    VALUES (@CandThai, 1, N'Phỏng vấn Vòng 1: Đánh giá Năng lực Kỹ thuật & Tư duy Thuật toán', '2026-11-06T14:00:00', 60, N'Online', N'https://meet.google.com/erp-interview-rnd1', @FirstEmpId, @AdminUserId, N'Scheduled', NULL, N'Chuẩn bị câu hỏi chuyên sâu về EF Core và Concurrency', N'Pending', SYSUTCDATETIME(), 0);

IF @CandThai IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[InterviewSchedules] WHERE [CandidateId] = @CandThai AND [RoundNumber] = 2)
    INSERT INTO [dbo].[InterviewSchedules] ([CandidateId], [RoundNumber], [Title], [ScheduledTime], [DurationMinutes], [InterviewType], [LocationOrMeetingLink], [InterviewerEmployeeId], [InterviewerUserId], [Status], [Score], [Feedback], [Result], [CreatedAt], [IsDeleted])
    VALUES (@CandThai, 2, N'Phỏng vấn Vòng 2: Văn hóa Doanh nghiệp & Thỏa thuận Lương', '2026-11-10T09:30:00', 45, N'Offline', N'Phòng họp VIP Tầng 5 - Trụ sở Hà Nội', @FirstEmpId, @AdminUserId, N'Scheduled', NULL, NULL, N'Pending', SYSUTCDATETIME(), 0);

IF @CandThao IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[InterviewSchedules] WHERE [CandidateId] = @CandThao)
    INSERT INTO [dbo].[InterviewSchedules] ([CandidateId], [RoundNumber], [Title], [ScheduledTime], [DurationMinutes], [InterviewType], [LocationOrMeetingLink], [InterviewerEmployeeId], [InterviewerUserId], [Status], [Score], [Feedback], [Result], [CreatedAt], [IsDeleted])
    VALUES (@CandThao, 1, N'Phỏng vấn Vòng 1: Kỹ năng Thuyết phục & Xử lý Tình huống Khách hàng', '2026-11-05T15:30:00', 45, N'Offline', N'Phòng họp P302 - Trụ sở Hà Nội', @FirstEmpId, @AdminUserId, N'Completed', 85.50, N'Ứng viên nhanh nhẹn, tự tin, hiểu rõ bài toán bán hàng B2B.', N'Passed', SYSUTCDATETIME(), 0);

IF @CandCuong IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[InterviewSchedules] WHERE [CandidateId] = @CandCuong)
    INSERT INTO [dbo].[InterviewSchedules] ([CandidateId], [RoundNumber], [Title], [ScheduledTime], [DurationMinutes], [InterviewType], [LocationOrMeetingLink], [InterviewerEmployeeId], [InterviewerUserId], [Status], [Score], [Feedback], [Result], [CreatedAt], [IsDeleted])
    VALUES (@CandCuong, 1, N'Phỏng vấn Vòng Kỹ thuật Chuyên sâu & Kiến trúc Clean Architecture', '2026-10-25T10:00:00', 60, N'Online', N'https://meet.google.com/erp-cuong-interview', @FirstEmpId, @AdminUserId, N'Completed', 92.00, N'Kiến trúc sư phần mềm kinh nghiệm, am hiểu sâu DDD và Microservices.', N'Passed', SYSUTCDATETIME(), 0);

PRINT N'[OK] Đã nạp dữ liệu lịch phỏng vấn ứng viên.';
GO

-- ==========================================================================================
-- 6. DANH MỤC TÀI SẢN & BIÊN BẢN CẤP PHÁT (COMPANYASSETS / ASSIGNMENTS)
-- ==========================================================================================
PRINT N'6. Tạo danh mục tài sản và cấp phát cho nhân sự...';

DECLARE @BranchId INT = (SELECT TOP 1 [Id] FROM [dbo].[Branches] ORDER BY [Id]);
DECLARE @AdminId INT = (SELECT TOP 1 [Id] FROM [dbo].[Users] WHERE [Username] = 'admin');

-- 6.1. Danh mục tài sản
IF NOT EXISTS (SELECT 1 FROM [dbo].[CompanyAssets] WHERE [AssetCode] = N'AST-LAP-001')
    INSERT INTO [dbo].[CompanyAssets] ([AssetCode], [AssetName], [AssetType], [SerialNumber], [Specification], [PurchaseDate], [PurchasePrice], [WarrantyExpiryDate], [Status], [BranchId], [CreatedAt], [IsDeleted])
    VALUES (N'AST-LAP-001', N'Laptop Dell XPS 15 9530', N'Laptop', N'SN-DELL-XPS-9530-01', N'Core i7-13700H, RAM 32GB, SSD 1TB NVMe, RTX 4060 8GB', '2026-01-15', 45000000.00, '2029-01-15', N'Assigned', @BranchId, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[CompanyAssets] WHERE [AssetCode] = N'AST-LAP-002')
    INSERT INTO [dbo].[CompanyAssets] ([AssetCode], [AssetName], [AssetType], [SerialNumber], [Specification], [PurchaseDate], [PurchasePrice], [WarrantyExpiryDate], [Status], [BranchId], [CreatedAt], [IsDeleted])
    VALUES (N'AST-LAP-002', N'ThinkPad X1 Carbon Gen 11', N'Laptop', N'SN-TP-X1C-GEN11-02', N'Core i7-1365U, RAM 16GB, SSD 512GB, 14" 2.8K OLED', '2026-02-20', 38000000.00, '2029-02-20', N'Assigned', @BranchId, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[CompanyAssets] WHERE [AssetCode] = N'AST-MON-001')
    INSERT INTO [dbo].[CompanyAssets] ([AssetCode], [AssetName], [AssetType], [SerialNumber], [Specification], [PurchaseDate], [PurchasePrice], [WarrantyExpiryDate], [Status], [BranchId], [CreatedAt], [IsDeleted])
    VALUES (N'AST-MON-001', N'Màn hình Dell UltraSharp 27" 4K U2723QE', N'Monitor', N'SN-DELL-U2723QE-01', N'27 inch 4K IPS Black, USB-C 90W Hub, 100% sRGB & DCI-P3', '2026-03-10', 12500000.00, '2029-03-10', N'Assigned', @BranchId, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[CompanyAssets] WHERE [AssetCode] = N'AST-CRD-001')
    INSERT INTO [dbo].[CompanyAssets] ([AssetCode], [AssetName], [AssetType], [SerialNumber], [Specification], [PurchaseDate], [PurchasePrice], [WarrantyExpiryDate], [Status], [BranchId], [CreatedAt], [IsDeleted])
    VALUES (N'AST-CRD-001', N'Thẻ từ thông minh ra vào văn phòng RFID', N'AccessCard', N'RFID-CARD-001', N'Thẻ bảo mật Mifare Desfire EV3 phân tầng tòa nhà', '2026-01-01', 150000.00, '2030-01-01', N'Assigned', @BranchId, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[CompanyAssets] WHERE [AssetCode] = N'AST-MOB-001')
    INSERT INTO [dbo].[CompanyAssets] ([AssetCode], [AssetName], [AssetType], [SerialNumber], [Specification], [PurchaseDate], [PurchasePrice], [WarrantyExpiryDate], [Status], [BranchId], [CreatedAt], [IsDeleted])
    VALUES (N'AST-MOB-001', N'Điện thoại iPhone 15 Pro Max công vụ', N'Mobile', N'SN-APPLE-15PM-001', N'256GB Titan Tự Nhiên, cài đặt sẵn ERP Security Profile', '2026-04-05', 31000000.00, '2027-04-05', N'Assigned', @BranchId, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[CompanyAssets] WHERE [AssetCode] = N'AST-LAP-003')
    INSERT INTO [dbo].[CompanyAssets] ([AssetCode], [AssetName], [AssetType], [SerialNumber], [Specification], [PurchaseDate], [PurchasePrice], [WarrantyExpiryDate], [Status], [BranchId], [CreatedAt], [IsDeleted])
    VALUES (N'AST-LAP-003', N'Laptop HP EliteBook 840 G10', N'Laptop', N'SN-HP-840G10-03', N'Core i5-1335U, RAM 16GB, SSD 512GB, 14 inch FHD', '2026-05-12', 24000000.00, '2028-05-12', N'Available', @BranchId, SYSUTCDATETIME(), 0);

-- 6.2. Cấp phát tài sản
DECLARE @AssetLap1 INT = (SELECT TOP 1 [Id] FROM [dbo].[CompanyAssets] WHERE [AssetCode] = N'AST-LAP-001');
DECLARE @AssetMon1 INT = (SELECT TOP 1 [Id] FROM [dbo].[CompanyAssets] WHERE [AssetCode] = N'AST-MON-001');
DECLARE @TargetEmp INT = (SELECT TOP 1 [Id] FROM [dbo].[Employees] ORDER BY [Id]);

IF @AssetLap1 IS NOT NULL AND @TargetEmp IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[EmployeeAssetAssignments] WHERE [AssetId] = @AssetLap1 AND [EmployeeId] = @TargetEmp)
BEGIN
    INSERT INTO [dbo].[EmployeeAssetAssignments] ([AssetId], [EmployeeId], [AssignedDate], [ConditionOnAssign], [HandoverDocumentUrl], [Status], [AssignedById], [Note], [CreatedAt], [IsDeleted])
    VALUES (@AssetLap1, @TargetEmp, '2026-01-20', N'Máy mới 100%, nguyên hộp kèm sạc 130W Type-C và chuột không dây Dell', N'/uploads/assets/BBBG-AST-LAP-001.pdf', N'Active', @AdminId, N'Cấp phát thiết bị làm việc chính thức', SYSUTCDATETIME(), 0);
END

IF @AssetMon1 IS NOT NULL AND @TargetEmp IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[EmployeeAssetAssignments] WHERE [AssetId] = @AssetMon1 AND [EmployeeId] = @TargetEmp)
BEGIN
    INSERT INTO [dbo].[EmployeeAssetAssignments] ([AssetId], [EmployeeId], [AssignedDate], [ConditionOnAssign], [HandoverDocumentUrl], [Status], [AssignedById], [Note], [CreatedAt], [IsDeleted])
    VALUES (@AssetMon1, @TargetEmp, '2026-01-20', N'Màn hình mới 100%, kèm cáp Type-C và HDMI zin theo máy', N'/uploads/assets/BBBG-AST-MON-001.pdf', N'Active', @AdminId, N'Cấp phát màn hình ngoài phục vụ đồ họa & code', SYSUTCDATETIME(), 0);
END

PRINT N'[OK] Đã nạp danh mục và cấp phát tài sản doanh nghiệp.';
GO

-- ==========================================================================================
-- 7. QUYẾT ĐỊNH NHÂN SỰ ĐIỆN TỬ (HRDECISIONS)
-- ==========================================================================================
PRINT N'7. Tạo các quyết định Nhân sự điện tử...';

DECLARE @TargetEmp1 INT = (SELECT TOP 1 [Id] FROM [dbo].[Employees] ORDER BY [Id]);
DECLARE @TargetEmp2 INT = (SELECT TOP 1 [Id] FROM [dbo].[Employees] WHERE [Id] <> @TargetEmp1 ORDER BY [Id]);
IF @TargetEmp2 IS NULL SET @TargetEmp2 = @TargetEmp1;

DECLARE @DeptOld INT = (SELECT TOP 1 [Id] FROM [dbo].[Departments] ORDER BY [Id]);
DECLARE @DeptNew INT = (SELECT TOP 1 [Id] FROM [dbo].[Departments] WHERE [Id] <> @DeptOld ORDER BY [Id]);
IF @DeptNew IS NULL SET @DeptNew = @DeptOld;

DECLARE @PosOld INT = (SELECT TOP 1 [Id] FROM [dbo].[Positions] ORDER BY [Id]);
DECLARE @PosNew INT = (SELECT TOP 1 [Id] FROM [dbo].[Positions] WHERE [Id] <> @PosOld ORDER BY [Id]);
IF @PosNew IS NULL SET @PosNew = @PosOld;

DECLARE @AdminUser INT = (SELECT TOP 1 [Id] FROM [dbo].[Users] WHERE [Username] = 'admin');

-- 7.1. Quyết định Bổ nhiệm Trưởng phòng
IF NOT EXISTS (SELECT 1 FROM [dbo].[HrDecisions] WHERE [DecisionNumber] = N'QD-2026-001/BN')
    INSERT INTO [dbo].[HrDecisions] (
        [DecisionNumber], [EmployeeId], [DecisionType], [Title], [EffectiveDate], [SignDate],
        [OldDepartmentId], [NewDepartmentId], [OldPositionId], [NewPositionId],
        [OldSalary], [NewSalary], [Amount], [Reason], [AttachmentUrl], [Status],
        [ApprovedById], [ApprovedAt], [SignerName], [SignerTitle], [CreatedAt], [IsDeleted]
    )
    VALUES (
        N'QD-2026-001/BN', @TargetEmp1, N'Appointment',
        N'Quyết định Bổ nhiệm chức danh Trưởng phòng Kinh doanh Doanh nghiệp',
        '2026-01-01', '2025-12-28',
        @DeptOld, @DeptOld, @PosOld, @PosNew,
        18000000.00, 26000000.00, 0,
        N'Căn cứ năng lực điều hành và thành tích xuất sắc trong công tác phát triển thị trường năm 2025',
        N'/uploads/decisions/QD-2026-001-BN.pdf', N'Issued',
        @AdminUser, '2025-12-28T10:00:00', N'Nguyễn Văn Tiến Phong', N'Tổng Giám Đốc', SYSUTCDATETIME(), 0
    );

-- 7.2. Quyết định Khen thưởng Nhân viên Xuất sắc Quý 3 (Kèm tiền thưởng)
IF NOT EXISTS (SELECT 1 FROM [dbo].[HrDecisions] WHERE [DecisionNumber] = N'QD-2026-004/KT')
    INSERT INTO [dbo].[HrDecisions] (
        [DecisionNumber], [EmployeeId], [DecisionType], [Title], [EffectiveDate], [SignDate],
        [OldSalary], [NewSalary], [Amount], [Reason], [AttachmentUrl], [Status],
        [ApprovedById], [ApprovedAt], [SignerName], [SignerTitle], [CreatedAt], [IsDeleted]
    )
    VALUES (
        N'QD-2026-004/KT', @TargetEmp1, N'Reward',
        N'Quyết định Khen thưởng Nhân viên Xuất sắc Toàn công ty Quý 3/2026',
        '2026-10-01', '2026-09-30',
        NULL, NULL, 10000000.00,
        N'Đạt thành tích vượt 180% chỉ tiêu KPI, đóng góp sáng kiến tự động hóa quy trình ERP',
        N'/uploads/decisions/QD-2026-004-KT.pdf', N'Issued',
        @AdminUser, '2026-09-30T16:00:00', N'Nguyễn Văn Tiến Phong', N'Tổng Giám Đốc', SYSUTCDATETIME(), 0
    );

-- 7.3. Quyết định Tăng lương định kỳ trước hạn
IF NOT EXISTS (SELECT 1 FROM [dbo].[HrDecisions] WHERE [DecisionNumber] = N'QD-2026-003/TL')
    INSERT INTO [dbo].[HrDecisions] (
        [DecisionNumber], [EmployeeId], [DecisionType], [Title], [EffectiveDate], [SignDate],
        [OldSalary], [NewSalary], [Amount], [Reason], [AttachmentUrl], [Status],
        [ApprovedById], [ApprovedAt], [SignerName], [SignerTitle], [CreatedAt], [IsDeleted]
    )
    VALUES (
        N'QD-2026-003/TL', @TargetEmp2, N'SalaryAdjustment',
        N'Quyết định Điều chỉnh Tăng lương bậc chuyên gia trước thời hạn',
        '2026-07-01', '2026-06-25',
        20000000.00, 24000000.00, 0,
        N'Hoàn thành xuất sắc dự án nâng cấp hệ thống phần mềm lõi và đào tạo thế hệ kế cận',
        N'/uploads/decisions/QD-2026-003-TL.pdf', N'Issued',
        @AdminUser, '2026-06-25T14:30:00', N'Nguyễn Văn Tiến Phong', N'Tổng Giám Đốc', SYSUTCDATETIME(), 0
    );

PRINT N'[OK] Đã nạp các quyết định Nhân sự điện tử.';
GO

-- ==========================================================================================
-- 8. KHÓA CHẤM CÔNG & ĐƠN GIẢI TRÌNH CHẤM CÔNG (ATTENDANCELOCKS / ADJUSTMENTS)
-- ==========================================================================================
PRINT N'8. Tạo dữ liệu khóa chấm công & đơn giải trình...';

DECLARE @AdminUId INT = (SELECT TOP 1 [Id] FROM [dbo].[Users] WHERE [Username] = 'admin');
DECLARE @TestEmpId INT = (SELECT TOP 1 [Id] FROM [dbo].[Employees] ORDER BY [Id]);
DECLARE @DefaultShift INT = (SELECT TOP 1 [Id] FROM [dbo].[Shifts] ORDER BY [Id]);

-- 8.1. Khóa chấm công tháng 9/2026
IF NOT EXISTS (SELECT 1 FROM [dbo].[AttendanceLocks] WHERE [Month] = 9 AND [Year] = 2026)
    INSERT INTO [dbo].[AttendanceLocks] ([Month], [Year], [BranchId], [DepartmentId], [IsLocked], [LockedAt], [LockedById], [Note], [CreatedAt], [IsDeleted])
    VALUES (9, 2026, NULL, NULL, 1, '2026-10-02T18:00:00', @AdminUId, N'Đã chốt bảng công Tháng 09/2026 toàn công ty chuyển bộ phận Kế toán tính lương', SYSUTCDATETIME(), 0);

-- 8.2. Đơn giải trình chấm công
IF @TestEmpId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendanceAdjustmentRequests] WHERE [RequestCode] = N'AR-2610-001')
    INSERT INTO [dbo].[AttendanceAdjustmentRequests] (
        [RequestCode], [EmployeeId], [WorkDate], [ShiftId], [AdjustmentType],
        [ActualInTime], [ActualOutTime], [Reason], [AttachmentUrl], [Status],
        [ApprovedById], [ApprovedAt], [ApprovalNote], [CreatedAt], [IsDeleted]
    )
    VALUES (
        N'AR-2610-001', @TestEmpId, '2026-10-25', @DefaultShift, N'MissedCheckIn',
        '08:15:00', '17:35:00', N'Thiết bị quẹt thẻ cửa tầng 3 gặp sự cố kỹ thuật không nhận vân tay lúc 08h15 sáng',
        N'/uploads/evidence/anh_xac_nhan_bao_ve.jpg', N'Approved',
        @AdminUId, '2026-10-25T18:00:00', N'Đã đối chiếu camera an ninh sảnh tầng 3, duyệt bổ sung giờ vào 08h15', SYSUTCDATETIME(), 0
    );

IF @TestEmpId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendanceAdjustmentRequests] WHERE [RequestCode] = N'AR-2610-002')
    INSERT INTO [dbo].[AttendanceAdjustmentRequests] (
        [RequestCode], [EmployeeId], [WorkDate], [ShiftId], [AdjustmentType],
        [ActualInTime], [ActualOutTime], [Reason], [AttachmentUrl], [Status],
        [ApprovedById], [ApprovedAt], [ApprovalNote], [CreatedAt], [IsDeleted]
    )
    VALUES (
        N'AR-2610-002', @TestEmpId, '2026-10-28', @DefaultShift, N'BusinessTrip',
        '08:00:00', '18:30:00', N'Đi gặp đối tác khách hàng B2B đột xuất tại KCN Bắc Ninh, có vé cầu đường và ảnh check-in',
        N'/uploads/evidence/ve_cau_duong_bac_ninh.pdf', N'Pending',
        NULL, NULL, NULL, SYSUTCDATETIME(), 0
    );

PRINT N'[OK] Đã nạp khóa chấm công và đơn giải trình bổ sung giờ.';
GO

-- ==========================================================================================
-- 9. QUY TRÌNH HỘI NHẬP ONBOARDING & BÀN GIAO NGHỈ VIỆC OFFBOARDING (CHECKLISTS / TASKS)
-- ==========================================================================================
PRINT N'9. Tạo quy trình Onboarding & Offboarding mẫu...';

DECLARE @EmpNew INT = (SELECT TOP 1 [Id] FROM [dbo].[Employees] ORDER BY [Id]);
DECLARE @AdminHr INT = (SELECT TOP 1 [Id] FROM [dbo].[Users] WHERE [Username] = 'admin');

-- 9.1. Checklist Onboarding nhân viên mới
DECLARE @OnboardingId INT;

IF @EmpNew IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[OnboardingChecklists] WHERE [EmployeeId] = @EmpNew AND [Type] = N'Onboarding')
BEGIN
    INSERT INTO [dbo].[OnboardingChecklists] ([EmployeeId], [Type], [Title], [StartDate], [TargetCompletionDate], [Status], [AssignedHrId], [Note], [CreatedAt], [IsDeleted])
    VALUES (@EmpNew, N'Onboarding', N'Quy trình Hội nhập Nhân sự mới (30 ngày đầu tiên)', '2026-10-01', '2026-10-31', N'InProgress', @AdminHr, N'Theo sát nhân viên mới trong tuần đầu và tháng đầu tiên', SYSUTCDATETIME(), 0);

    SET @OnboardingId = SCOPE_IDENTITY();

    -- Chi tiết 10 đầu việc Onboarding phân vai rõ ràng
    INSERT INTO [dbo].[OnboardingTasks] ([ChecklistId], [TaskName], [DepartmentRole], [AssignedToUserId], [DueDate], [CompletedDate], [IsCompleted], [Status], [OrderIndex], [CreatedAt], [IsDeleted])
    VALUES
        (@OnboardingId, N'Hoàn thiện hồ sơ nhân sự, ký hợp đồng lao động và khai báo BHXH', N'HR', @AdminHr, '2026-10-02', '2026-10-02', 1, N'Completed', 1, SYSUTCDATETIME(), 0),
        (@OnboardingId, N'Cấp email doanh nghiệp (@tienphong.vn) và tài khoản phần mềm ERP', N'IT', @AdminHr, '2026-10-02', '2026-10-02', 1, N'Completed', 2, SYSUTCDATETIME(), 0),
        (@OnboardingId, N'Cài đặt máy tính, cấp màn hình, thẻ từ và chuột bàn phím', N'IT', @AdminHr, '2026-10-03', '2026-10-03', 1, N'Completed', 3, SYSUTCDATETIME(), 0),
        (@OnboardingId, N'Phát áo đồng phục, sổ tay nhân viên và thẻ đeo công ty', N'HR', @AdminHr, '2026-10-03', '2026-10-03', 1, N'Completed', 4, SYSUTCDATETIME(), 0),
        (@OnboardingId, N'Giới thiệu nhân viên mới với toàn thể các phòng ban trong công ty', N'DepartmentHead', @AdminHr, '2026-10-03', '2026-10-03', 1, N'Completed', 5, SYSUTCDATETIME(), 0),
        (@OnboardingId, N'Phổ biến nội quy lao động, chính sách bảo mật và an toàn thông tin', N'HR', @AdminHr, '2026-10-05', '2026-10-05', 1, N'Completed', 6, SYSUTCDATETIME(), 0),
        (@OnboardingId, N'Gán Mentor hướng dẫn kỹ thuật và kế hoạch công việc tháng đầu', N'DepartmentHead', @AdminHr, '2026-10-07', '2026-10-07', 1, N'Completed', 7, SYSUTCDATETIME(), 0),
        (@OnboardingId, N'Đánh giá kết quả tuần làm việc đầu tiên cùng Trưởng phòng', N'DepartmentHead', @AdminHr, '2026-10-10', '2026-10-10', 1, N'Completed', 8, SYSUTCDATETIME(), 0),
        (@OnboardingId, N'Tham gia khóa đào tạo văn hóa doanh nghiệp và quy trình vận hành', N'HR', @AdminHr, '2026-10-18', NULL, 0, N'InProgress', 9, SYSUTCDATETIME(), 0),
        (@OnboardingId, N'Phỏng vấn đánh giá kết thúc thời gian thử việc 60 ngày', N'HR', @AdminHr, '2026-11-30', NULL, 0, N'Pending', 10, SYSUTCDATETIME(), 0);

    PRINT N'[OK] Đã tạo Checklist Onboarding kèm 10 đầu việc tiêu chuẩn.';
END
GO

PRINT N'=== [V6_02] Hoàn tất nạp Dữ liệu Mẫu Doanh nghiệp Enterprise HRM 100% ===';
GO

