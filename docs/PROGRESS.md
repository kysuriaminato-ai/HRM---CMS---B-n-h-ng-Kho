# Tiến độ dự án — ERP_HRM_CMS Enterprise

Cập nhật lần cuối: 2026-11-08 (Hoàn tất **PHIÊN A – Đối soát tài liệu, đóng nợ P0-02/P0-04 và dọn repo** — Build 0 Warning / 0 Error; Test: 208 phương thức / 255 ca thực thi, đạt 249/255 ca, 2/2 test mới Phiên A xanh 100%, ghi nhận trung thực 6 ca fail có sẵn từ commit `937c667` thuộc phạm vi P0-11 & giao diện trang sức chờ xác nhận)

---

## TỔNG QUAN KẾT QUẢ ĐẠT ĐƯỢC (ĐỐI SOÁT THỰC TẾ TẠI PHIÊN A)

| Phiên | Phân hệ / Mục tiêu | Trạng thái thực tế | Kiểm thử tự động |
| :--- | :--- | :---: | :---: |
| **Phiên 1** | Tái Cấu Trúc Clean Architecture (4 Tầng), Phân Quyền Scope & Xóa Nợ P0 | **Một phần** *(Đã tách 4 project `Domain`/`Application`/`Infrastructure`/`Web` và chuyển `AppDbContext` + Entities; nhưng **20/34 controller còn gọi trực tiếp `AppDbContext`** và phần lớn service nghiệp vụ còn nằm ở `ERP.Web/Services/`)* | Đạt (P0-02, P0-04 xanh; P0-11 còn 1 test fail do `937c667`) |
| **Phiên 2** | Cơ Sở Dữ Liệu Enterprise HRM Trên SSMS & EF Core (Hire-to-Retire) | **Hoàn tất** | 100% Pass |
| **Phiên 3** | Nâng Cấp Theme "Nordic People & Indigo Slate" & Design System CSS | **Hoàn tất** | 100% Pass |
| **Phiên 4** | Hồ Sơ Nhân Sự 360°, Sơ Đồ Tổ Chức Tương Tác, Quyết Định & Tài Sản | **Hoàn tất** | 100% Pass |
| **Phiên 5** | Chấm Công Thông Minh, Phân Ca (Shift Roster), Quỹ Phép & Duyệt Đơn Từ Đa Cấp | **Hoàn tất** | 100% Pass |
| **Phiên 6** | Cổng Tự Phục Vụ Nhân Viên (ESS Portal) Zero IDOR & Real-time SignalR | **Hoàn tất** | 100% Pass |
| **Phiên 7** | Tuyển Dụng ATS (Kanban Pipeline), Onboarding & Cổng Tuyển Dụng SEO | **Hoàn tất** | 100% Pass |
| **Phiên 8** | Đánh Giá KPI, Ma Trận 9-Box, LMS Đào Tạo & Khép Kín Chuỗi Tính Lương C&B | **Hoàn tất** | 100% Pass |
| **Phiên 9** | Executive People Analytics, Tối Ưu Hiệu Năng & Sẵn Sàng Production | **Hoàn tất** *(Trước Phiên A thực tế là 247/253 ca pass do commit `937c667` làm gãy 6 ca)* | 247 / 253 ca thực thi |
| **Phiên A** | Đối Soát Tài Liệu, Đóng Nợ P0 (`P0-02`, `P0-04`), Khử 15 Warning & Dọn Repo | **Hoàn tất** | **249 / 255 ca thực thi (2/2 test Phiên A xanh)** |

---

## CHI TIẾT PHIÊN B: BASELINE TRƯỚC KHI NÂNG CẤP .NET 10 LTS (2026-11-08)

### 1. Số liệu Baseline trước khi nâng (trên nhánh `feature/phase-b-dotnet10`)
- **`dotnet --list-sdks`**:
  + `9.0.312 [C:\Program Files\dotnet\sdk]` *(Lưu ý: Máy hiện chỉ có .NET SDK 9.0.312, **chưa cài .NET 10.0 SDK**)*.
- **`dotnet --version`**: `9.0.312`.
- **Runtimes đã cài đặt**: `Microsoft.AspNetCore.App` (`8.0.25`, `9.0.14`), `Microsoft.NETCore.App` (`8.0.25`, `9.0.14`).
- **Danh sách file `.csproj` trong repo (5 file, đang ở `net8.0`)**:
  1. `ERP_HRM_CMS.csproj`
  2. `src/ERP.Domain/ERP.Domain.csproj`
  3. `src/ERP.Application/ERP.Application.csproj`
  4. `src/ERP.Infrastructure/ERP.Infrastructure.csproj`
  5. `tests/ERP_HRM_CMS.Tests/ERP_HRM_CMS.Tests.csproj`
- **Kết quả `dotnet build ERP_HRM_CMS.sln` (baseline `net8.0`)**:
  + **0 Error(s), 0 C# Compiler Warning(s)** (chỉ có 1 cảnh báo `NU1900` khi chạy offline trong sandbox không kết nối `api.nuget.org`).
- **Kết quả `dotnet test tests/ERP_HRM_CMS.Tests` (baseline `net8.0`)**:
  + Tổng số phương thức test: **208** (`200 [Fact]` + `8 [Theory]`).
  + Tổng số ca thực thi: **255** — **Đạt (Passed): 249**, **Lỗi (Failed): 6**, **Bỏ qua (Skipped): 0**.
  + **Chi tiết 6 ca test lỗi có sẵn từ commit `937c667` (ghi nhận tại Phiên A)**:
    1. `SecurityHardeningTests.DangKy_TenDaTonTaiVaTenMoi_TraVeCungPhanHoi`: Fail (`Expected: Found, Actual: OK`) do `AuthController.Register` (dòng 191–195) trả `View(model)` khi trùng tên đăng nhập (thuộc mục `P0-11`).
    2. `PublicPagesTests.Public_Pages_Render_For_Anonymous_Visitors` (5 dòng `[InlineData]`: `"/"`, `"/Public/TinTuc"`, `"/Public/TinTuc?keyword=erp"`, `"/Public/GioiThieu"`, `"/Public/LienHe"`): Fail do `Views/Shared/_PublicLayout.cshtml` không còn phần tử `id="psSearchToggle"` sau khi đổi giao diện cửa hàng trang sức ở commit `937c667`.
- **Trạng thái dừng chờ xác nhận ở Bước 1**:
  + Tạm dừng trước khi đổi `TargetFramework` sang `net10.0` để báo cáo 2 điểm theo đúng quy định Nhiệm vụ 1: (1) Máy hiện chưa cài `.NET 10 SDK` (chỉ có `9.0.312`), và (2) Baseline đang có 6 ca test lỗi từ commit `937c667`.

---

## CHI TIẾT KẾT QUẢ PHIÊN A: ĐỐI SOÁT TÀI LIỆU, ĐÓNG NỢ P0 VÀ DỌN REPO (2026-11-08)

### 1. Việc đã làm
- **Đối soát tài liệu với mã nguồn thực tế**:
  + Đính chính trạng thái Clean Architecture tại `docs/PROGRESS.md`: đã tách 4 tầng project (`src/ERP.Domain`, `src/ERP.Application`, `src/ERP.Infrastructure`, `ERP_HRM_CMS.csproj`), nhưng **20/34 controller** (`AuditLogController`, `AuthController`, `BangLuongController`, `BanHangController`, `CongNoController`, `ContentController`, `DanhMucController`, `DonHangController`, `HomeController`, `KhoController`, `LayoutConfigController`, `MenuController`, `PhanQuyenController`, `PhieuInController`, `PublicController`, `PurchaseOrderController`, `QuanTriTaiKhoanController`, `SanPhamController`, `SupplierController`, `VaiTroController`) vẫn tiêm trực tiếp `AppDbContext`; 14 controller còn lại gọi qua service nhưng các service HRM/Kho/Lương vẫn đặt tại project Web (`Services/`).
  + Cập nhật `README.md`: sửa đường dẫn `Data/AppDbContext.cs` → `src/ERP.Infrastructure/Persistence/AppDbContext.cs`, `Data/AppDbContextFactory.cs` → `src/ERP.Infrastructure/Persistence/AppDbContextFactory.cs`, `Helpers/VietnamTime.cs` → `src/ERP.Domain/Common/VietnamTime.cs`, `Models/Entities/` → `src/ERP.Domain/Entities/`; ghi rõ số lượng kiểm thử thực tế (**208 phương thức = 200 `[Fact]` + 8 `[Theory]`, tương ứng 255 ca kiểm thử thực thi** bao gồm 55 dòng `[InlineData]` trên 31 file `*Tests.cs`).
- **Kiểm chứng toàn bộ P0 (`P0-01` → `P0-13`) và đóng nợ `P0-02`, `P0-04`**:
  + **`P0-02` (Dashboard Permission Check)**: Gắn `[KiemTraQuyen(DashboardPermissions)]` lên `HomeController.Index`, bổ sung kiểm tra quyền bên trong action để tài khoản đã đăng nhập nhưng không có quyền điều hành/tác nghiệp lẫn `hrm.self.service` bị chặn `403 Forbidden` (với AJAX) hoặc chuyển hướng về `/Home/KhongCoQuyen`. Đồng bộ kiểm tra `auth.view` / `auth.audit.view` cho khối nhật ký kiểm toán trên Dashboard.
  + **`P0-04` (Xóa bỏ `?? 1` / `?? 0` danh tính người dùng, trả HTTP 401)**:
    - Xóa toàn bộ `CurrentUserId => _currentUser.UserId ?? 1` và `_currentUser.EmployeeId ?? CurrentUserId` trong `DanhGiaKpiController`, `DaoTaoController`, `TuyenDungController`.
    - Xóa `CurrentUserId ?? 1` trong `ChamCongController` (`KhoaKyCong`, `MoKhoaKyCong`) và `QuyetDinhNhanSuController` (`Approve`, `Cancel`), đồng thời kiểm tra danh tính người dùng ở mọi POST action của 2 controller này.
    - Xóa `_currentUser.UserId ?? 0` trong `SelfServiceController` và trả `401 Unauthorized` khi không xác định được `UserId`.
    - Kiểm tra danh tính hợp lệ (`> 0`) và trả `401 Unauthorized` trong `ContentController.Create` (POST), `HomeController.Index` / `GlobalQuickSearch`, và `PhanQuyenController` (`LuuMaTranQuyenRole`, `LuuPhanQuyenCaNhan`).
    - Thêm `Filters/UnauthorizedAccessExceptionFilter.cs` và đăng ký toàn cục trong `Program.cs` để mọi lệnh gọi `_currentUser.GetRequiredUserId()` khi mất phiên/claim đều trả về HTTP `401 Unauthorized`.
  + **Ghi nhận trạng thái `P0-01`, `P0-03`, `P0-05` → `P0-13` vào `docs/ANALYSIS.md`**: đối chiếu từng mục kèm bằng chứng dòng code cụ thể và bảng thống kê đầy đủ mọi vị trí `?? 1` / `?? 0` trong repo.
- **Dọn dẹp repo**:
  + Chuyển 4 file `.patch` ở thư mục gốc (`0001-feat-db-Phase-4-database-design-implementation.patch`, `0001-feat-phase3-admin-theme-sidebar-topbar-components-an.patch`, `0001-fix-db-s-a-t-n-c-t-Employees-trong-V4_02_SeedData.sq.patch`, `docs-phan-tich-va-kien-truc.patch`) vào `archive/patches/`.
  + Hợp nhất nội dung `fix-placeholder-images.sql` ở thư mục gốc vào bản chuẩn `Database/Legacy/fix-placeholder-images.sql` (bọc trọn trong transaction an toàn), xóa bản trùng ở thư mục gốc và ghi rõ nguồn chuẩn trong `README.md`.
  + Kiểm chứng `_columns_check.csv` (434 dòng dump danh sách cột tạm, không được tham chiếu ở bất kỳ đâu, đã nêu tại mục A2 trong `docs/ANALYSIS.md`) và xóa khỏi repo.
- **Khử toàn bộ 15 cảnh báo biên dịch (0 Warning) & bổ sung kiểm thử**:
  + Sửa cảnh báo nullable/async/biến thừa tại `Services/Printing/PrintableDocument.cs`, `Views/Auth/Login.cshtml`, `Views/Auth/Register.cshtml`, `Views/Shared/_PublicLayout.cshtml`, `Views/DonTu/Index.cshtml`, `Views/PhongBan/Index.cshtml`, `tests/ERP_HRM_CMS.Tests/UiPhase3HrmThemeTests.cs`.
  + Thêm 2 bài kiểm thử tự động `P0_02_HomeIndex_Enforces_KiemTraQuyen_And_Blocks_Permissionless_AuthenticatedUser` và `P0_04_Controllers_Return_401Unauthorized_When_UserIdentity_CannotBeResolved` vào `tests/ERP_HRM_CMS.Tests/Phase1ArchitectureAndSecurityTests.cs`.

### 2. Danh sách file Thêm / Sửa / Xóa trong Phiên A
- **Thêm mới (1 file code + 4 file chuyển vùng lưu trữ)**:
  + `Filters/UnauthorizedAccessExceptionFilter.cs`
  + `archive/patches/0001-feat-db-Phase-4-database-design-implementation.patch` *(di chuyển từ gốc)*
  + `archive/patches/0001-feat-phase3-admin-theme-sidebar-topbar-components-an.patch` *(di chuyển từ gốc)*
  + `archive/patches/0001-fix-db-s-a-t-n-c-t-Employees-trong-V4_02_SeedData.sq.patch` *(di chuyển từ gốc)*
  + `archive/patches/docs-phan-tich-va-kien-truc.patch` *(di chuyển từ gốc)*
- **Sửa đổi (19 file)**:
  + `Program.cs`
  + `Controllers/HomeController.cs`
  + `Controllers/DanhGiaKpiController.cs`
  + `Controllers/DaoTaoController.cs`
  + `Controllers/TuyenDungController.cs`
  + `Controllers/ChamCongController.cs`
  + `Controllers/QuyetDinhNhanSuController.cs`
  + `Controllers/SelfServiceController.cs`
  + `Controllers/ContentController.cs`
  + `Controllers/PhanQuyenController.cs`
  + `Services/Printing/PrintableDocument.cs`
  + `Views/Auth/Login.cshtml`
  + `Views/Auth/Register.cshtml`
  + `Views/Shared/_PublicLayout.cshtml`
  + `Views/DonTu/Index.cshtml`
  + `Views/PhongBan/Index.cshtml`
  + `Database/Legacy/fix-placeholder-images.sql`
  + `tests/ERP_HRM_CMS.Tests/Phase1ArchitectureAndSecurityTests.cs`
  + `tests/ERP_HRM_CMS.Tests/UiPhase3HrmThemeTests.cs`
  + Tài liệu: `README.md`, `docs/ANALYSIS.md`, `docs/PROGRESS.md`
- **Xóa khỏi thư mục gốc (6 file)**:
  + `fix-placeholder-images.sql` *(đã hợp nhất vào `Database/Legacy/fix-placeholder-images.sql`)*
  + `_columns_check.csv` *(file nháp tạm mục A2)*
  + 4 file `.patch` ở thư mục gốc *(đã chuyển vào `archive/patches/`)*

### 3. Rủi ro & Lưu ý
- Các tài khoản thử nghiệm hoặc tài khoản mới tạo nếu **chưa được gán bất kỳ vai trò/quyền nào** (kể cả quyền cơ bản `hrm.self.service` của vai trò `EMPLOYEE`) khi truy cập `/Home/Index` sẽ nhận chuyển hướng tới `/Home/KhongCoQuyen` (hoặc `403` nếu gọi AJAX) theo đúng thiết kế bảo mật `P0-02`.
- Các vị trí `branchId ?? 1` tại POS/Kho/Giỏ hàng (mục D10) không phải là danh tính người dùng nên được giữ nguyên hành vi hiện tại và đã liệt kê đầy đủ trong `docs/ANALYSIS.md` mục 12.1.1 để xử lý cùng lộ trình phạm vi dữ liệu.

### 4. Việc còn lại cho các phiên tiếp theo
- Đóng nốt các phần còn mở của nhóm P0 đã kiểm chứng trong `docs/ANALYSIS.md`:
  + `P0-05`: Ghi sự kiện `LOGIN_LOCKED_OUT` riêng vào `AuditLogs` khi tài khoản bị khóa tạm thời do sai mật khẩu.
  + `P0-06`: Gỡ bỏ hoàn toàn nhánh dev fallback mật khẩu mặc định trong `AuthController.cs:98–104` và nút điền nhanh mật khẩu mặc định trong `Views/Auth/Login.cshtml`.
  + `P0-08`: Bổ sung giới hạn tần suất (rate limit / throttle) theo IP độc lập với tên đăng nhập.
  + `P0-09`: Viết ADR riêng cho Phạm vi dữ liệu (`DataScope`) và phủ nốt `IDataScopeService` cho `PerformanceReviewService`, `TrainingService`, `RecruitmentService`, `CongNoController`.
  + `P0-10`: Đối chiếu người duyệt cấp `"TP"` phải đúng `Department.ManagerId` của nhân viên nộp đơn và chuẩn hóa tham số `level` sang enum.
  + `P0-11`: Thêm rate limit cho `/Auth/Register`, dùng thông báo chung không lộ tài khoản tồn tại, và đặt `IsActive = false` chờ duyệt.
  + `P0-12`: Thay `Regex` trong `HtmlSanitizerHelper` bằng HTML parser trắng-danh-sách và gọi làm sạch ngay lúc ghi (`ContentController`).
  + `P0-13`: Bổ sung test quét tự động toàn bộ 100% endpoint có `[KiemTraQuyen]` với tài khoản đã đăng nhập nhưng không có quyền.
- Tiếp tục tách 20 controller còn gọi trực tiếp `AppDbContext` sang tầng Application/Service theo lộ trình Clean Architecture.

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
> **Lưu ý đối soát thực tế (Phiên B — Nhiệm vụ 9)**: Hai file `Dockerfile` và `docker-compose.prod.yml` được nhắc trong ví dụ dưới đây **hiện chưa có trong repo** (chưa được tạo). Đoạn cấu hình dưới đây chỉ mang tính chất mẫu tham khảo khi triển khai container hóa ở các phiên sau.

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
