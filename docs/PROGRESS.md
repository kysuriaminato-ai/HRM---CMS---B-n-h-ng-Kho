# Tiến độ dự án — ERP_HRM_CMS Enterprise

Cập nhật lần cuối: 2026-11-05 (Hoàn tất 100% mục tiêu 9 PHIÊN NÂNG CẤP TOÀN DIỆN — 253/253 test XANH 100%)

---

## TỔNG QUAN KẾT QUẢ ĐẠT ĐƯỢC SAU 9 PHIÊN

| Phiên | Phân hệ / Mục tiêu | Trạng thái | Kiểm thử tự động |
| :--- | :--- | :---: | :---: |
| **Phiên 1** | Tái Cấu Trúc Clean Architecture (4 Tầng), Phân Quyền Scope & Xóa Nợ P0 | **Hoàn tất** | 100% Pass |
| **Phiên 2** | Cơ Sở Dữ Liệu Enterprise HRM Trên SSMS & EF Core (Hire-to-Retire) | **Hoàn tất** | 100% Pass |
| **Phiên 3** | Nâng Cấp Theme "Nordic People & Indigo Slate" & Design System CSS | **Hoàn tất** | 100% Pass |
| **Phiên 4** | Hồ Sơ Nhân Sự 360°, Sơ Đồ Tổ Chức Tương Tác, Quyết Định & Tài Sản | **Hoàn tất** | 100% Pass |
| **Phiên 5** | Chấm Công Thông Minh, Phân Ca (Shift Roster), Quỹ Phép & Duyệt Đơn Từ Đa Cấp | **Hoàn tất** | 100% Pass |
| **Phiên 6** | Cổng Tự Phục Vụ Nhân Viên (ESS Portal) Zero IDOR & Real-time SignalR | **Hoàn tất** | 100% Pass |
| **Phiên 7** | Tuyển Dụng ATS (Kanban Pipeline), Onboarding & Cổng Tuyển Dụng SEO | **Hoàn tất** | 100% Pass |
| **Phiên 8** | Đánh Giá KPI, Ma Trận 9-Box, LMS Đào Tạo & Khép Kín Chuỗi Tính Lương C&B | **Hoàn tất** | 100% Pass |
| **Phiên 9** | Executive People Analytics, Tối Ưu Hiệu Năng & Sẵn Sàng Production | **Hoàn tất** | **253 / 253 Test Xanh** |

---

## CHI TIẾT KẾT QUẢ PHIÊN 9: EXECUTIVE PEOPLE ANALYTICS & PRODUCTION READINESS

### 1. Trung Tâm Phân Tích Nguồn Nhân Lực Cấp Điều Hành (`/NhanSu/BaoCao`)
- **Tầng Ứng dụng & Dịch vụ (`IPeopleAnalyticsService` & `PeopleAnalyticsService`)**:
  + **Tối ưu hóa Truy vấn Cơ sở dữ liệu Cực hạn**:
    - 100% các truy vấn đọc báo cáo sử dụng `.AsNoTracking()`.
    - Dùng truy vấn gộp nhóm `GroupBy` và tổng hợp trực tiếp trên SQL Server, giảm thiểu tối đa tải bộ nhớ và ngăn chặn triệt để hiện tượng N+1.
    - Áp dụng phân quyền phạm vi dữ liệu đa cấp `IDataScopeService`: Giám đốc/SuperAdmin xem toàn bộ công ty; Quản lý chi nhánh/phòng ban chỉ xem số liệu trong phạm vi phụ trách.
  + **6 Chỉ Số KPI Điều Hành Then Chốt (Executive Stat Cards)**:
    1. *Tổng nhân sự hiện diện & Định biên (Headcount vs Budgeted)* kèm % tăng trưởng.
    2. *Tỷ lệ biến động nhân sự (Turnover Rate %)* kèm xu hướng so với kỳ trước.
    3. *Tỷ lệ tuyển dụng lấp đầy (Fill Rate %)* đối chiếu số lượng định biên cần tuyển.
    4. *Tỷ lệ chuyên cần đi làm đúng giờ (Attendance Rate %)* đo lường kỷ luật lao động.
    5. *Quỹ lương Gross & Chi phí BHXH bắt buộc NSDLĐ (Gross Payroll & Employer Insurance)*.
    6. *Điểm hiệu suất KPI trung bình toàn công ty (Average Company KPI Score)*.
  + **4 Biểu Đồ Trực Quan Tương Tác Đồng Bộ Theme Động (Chart.js + Light/Dark Mode Sync)**:
    1. *Biểu đồ Biến động Nhân sự 12 Tháng (Headcount Movement)*: Xu hướng Tuyển mới vs Nghỉ việc.
    2. *Biểu đồ Cơ cấu Quỹ Lương & Chi phí theo Phòng ban (Department Payroll Breakdown)*: So sánh Lương cơ bản, Phụ cấp, OT và Bảo hiểm.
    3. *Biểu đồ Phễu Tuyển dụng (Recruitment Funnel)*: Chuyển đổi từ Ứng tuyển -> Sơ loại -> Phỏng vấn -> Offer -> Nhận việc.
    4. *Tháp Thâm Niên & Xếp Loại KPI (Seniority Pyramid & KPI Distribution)*: Cơ cấu phân bổ thời gian gắn bó (<1 năm, 1-3 năm, 3-5 năm, 5-10 năm, >10 năm).
  + **Bảng Cảnh Báo Hành Động Sớm Cho HR (Early HR Action Matrix)**:
    - *Hợp đồng sắp hết hạn trong 30 ngày*: Đánh dấu mức độ khẩn cấp (Critical <= 7 ngày, Warning <= 15 ngày).
    - *Nhân sự sắp hết thời gian thử việc*: Cảnh báo trước 60 ngày để làm thủ tục đánh giá ký chính thức.
    - *Chứng chỉ chuyên môn sắp hết hạn trong 45 ngày*: Cảnh báo tái đào tạo/gia hạn bằng cấp.
    - *Phòng ban làm thêm giờ (OT) cao vượt ngưỡng*: Cảnh báo phòng ban có tổng giờ OT vượt 200 giờ/tháng.
  + **Xuất Báo Cáo Quản Trị Nhân Sự Ra File Excel Đa Sheet Chuẩn Mực (`ClosedXML`)**:
    - Sheet 1: *Tổng Quan Điều Hành*: Chỉ số KPI và thông số vĩ mô.
    - Sheet 2: *Biến Động Nhân Sự*: Thống kê 12 tháng tuyển dụng và nghỉ việc.
    - Sheet 3: *Quỹ Lương & Chi Phí*: Chi tiết chi phí nhân sự theo từng phòng ban.
    - Sheet 4: *Tuyển Dụng & Cảnh Báo*: Danh sách ứng viên và các hợp đồng cần xử lý gấp.

### 2. Rà Soát Bảo Mật, Hiệu Năng & Schema Health Master
- **Rà soát Phân quyền & Endpoint Protection**:
  + 100% các Controller quản trị trong hệ thống được bảo vệ nghiêm ngặt bằng `[Authorize]` hoặc `[KiemTraQuyen]`.
  + Tách bạch hoàn toàn giữa các Controller công khai (`PublicController`, `AuthController`, `HomeController`, `GioHangController`) và các Controller quản trị nội bộ.
- **Kịch Bản SQL Master Kiểm Định Toàn Diện (`db/scripts/V6_99_Verify_Schema_Health.sql`)**:
  + Tự động quét kiểm tra sự tồn tại của toàn bộ 50+ bảng dữ liệu Enterprise HRM, SCM, CMS và Auth.
  + Kiểm tra toàn vẹn các khóa ngoại (Foreign Keys) và các chỉ mục then chốt (Non-clustered Indexes).
  + Kiểm tra an toàn cột mã hóa thông tin nhạy cảm (`IdentityCard`, `BankAccountNumber`, `InsuranceNumber`) qua Data Protection.

### 3. Kiểm Định Tự Động Toàn Diện
- **Kết quả Build & Test Toàn Hệ Thống**:
  + `dotnet build ERP_HRM_CMS.sln`: **Thành công — 0 Error(s)**.
  + `dotnet test tests/ERP_HRM_CMS.Tests`: **253 / 253 bài kiểm thử XANH 100% (0 Failed, 0 Skipped)**.

---

## HƯỚNG DẪN TRIỂN KHAI PRODUCTION (PRODUCTION DEPLOYMENT GUIDE)

### A. Triển Khai Trên Windows Server + IIS (Internet Information Services)
1. **Cài Đặt Yêu Cầu Trên Máy Chủ**:
   - Cài đặt IIS Web Server kèm tính năng ASP.NET 4.8 / .NET Extensibility.
   - Cài đặt **.NET 8 Hosting Bundle (ASP.NET Core Module v2 - ANCM)** từ Microsoft.
   - Cài đặt SQL Server 2019/2022 Enterprise hoặc Standard.
2. **Xuất Bản Ứng Dụng (Publish Release)**:
   ```powershell
   dotnet publish ERP_HRM_CMS.csproj -c Release -o C:\inetpub\wwwroot\ERP_HRM_CMS
   ```
3. **Cấu Hình Application Pool & Thư Mục Khóa Mã Hóa**:
   - Tạo Application Pool trong IIS (ví dụ `ErpPool`), đặt `.NET CLR Version: No Managed Code`, `Managed Pipeline Mode: Integrated`.
   - Vào `Advanced Settings` của App Pool > Bật `Load User Profile: True`.
   - Tạo thư mục lưu trữ khóa Data Protection bên ngoài web root: `C:\ProgramData\ERP_HRM_CMS\Keys`. Phân quyền `Read/Write` cho `IIS AppPool\ErpPool`.
4. **Biến Môi Trường Hệ Thống (Environment Variables)**:
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - `ConnectionStrings__DefaultConnection`: `Server=10.0.0.x;Database=ERP_HRM_CMS_PROD;User Id=erp_app;Password=YourComplexPassword#2026;TrustServerCertificate=False;`
   - `DataProtection__KeysPath`: `C:\ProgramData\ERP_HRM_CMS\Keys`
   - `SeedAdmin__Password`: `MatKhauAdminQuanTriCapCao#2026`

### B. Triển Khai Bằng Docker & Linux Container
1. **Khởi Chạy Hệ Thống Bằng Docker Compose**:
   ```bash
   docker-compose -f docker-compose.prod.yml up -d
   ```
2. **Mount Persistent Volumes Cho Data Protection**:
   ```yaml
   volumes:
     - /var/data/erp_keys:/root/.aspnet/DataProtection-Keys
     - /var/data/erp_uploads:/app/wwwroot/uploads
   ```

### C. Quy Trình Vận Hành Cơ Sở Dữ Liệu Trên SSMS
- Sau khi khởi tạo database mới hoặc cập nhật phiên bản:
  1. Mở SSMS, kết nối đến SQL Server Production.
  2. Mở file `db/scripts/V6_99_Verify_Schema_Health.sql` và nhấn **F5** để chạy kiểm tra sức khỏe.
  3. Kiểm tra kết quả in ra tại cửa sổ `Messages`: đảm bảo 100% bảng, khóa ngoại và chỉ mục then chốt hiển thị dấu `[✔]`.
