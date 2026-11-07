# Tiến độ dự án — ERP_HRM_CMS Enterprise

Cập nhật lần cuối: 2026-11-08 (Hoàn tất **PHIÊN C1 – ASP.NET Core Identity (`IdentityUser<int>`), `LegacyCompatiblePasswordHasher` và Vai trò Khách hàng (`CUSTOMER`), Build 0 Warning / 0 Error và Test 261/261 XANH 100%**)

---

## TỔNG QUAN KẾT QUẢ ĐẠT ĐƯỢC (CẬP NHẬT TẠI PHIÊN C1)

| Phiên | Phân hệ / Mục tiêu | Trạng thái thực tế | Kiểm thử tự động |
| :--- | :--- | :---: | :---: |
| **Phiên 1** | Tái Cấu Trúc Clean Architecture (4 Tầng), Phân Quyền Scope & Xóa Nợ P0 | **Một phần** *(Đã tách 4 project `Domain`/`Application`/`Infrastructure`/`Web` và chuyển `AppDbContext` + Entities; nhưng **20/34 controller còn gọi trực tiếp `AppDbContext`** và phần lớn service nghiệp vụ còn nằm ở `ERP.Web/Services/`)* | 100% Pass |
| **Phiên 2** | Cơ Sở Dữ Liệu Enterprise HRM Trên SSMS & EF Core (Hire-to-Retire) | **Hoàn tất** | 100% Pass |
| **Phiên 3** | Nâng Cấp Theme "Nordic People & Indigo Slate" & Design System CSS | **Hoàn tất** | 100% Pass |
| **Phiên 4** | Hồ Sơ Nhân Sự 360°, Sơ Đồ Tổ Chức Tương Tác, Quyết Định & Tài Sản | **Hoàn tất** | 100% Pass |
| **Phiên 5** | Chấm Công Thông Minh, Phân Ca (Shift Roster), Quỹ Phép & Duyệt Đơn Từ Đa Cấp | **Hoàn tất** | 100% Pass |
| **Phiên 6** | Cổng Tự Phục Vụ Nhân Viên (ESS Portal) Zero IDOR & Real-time SignalR | **Hoàn tất** | 100% Pass |
| **Phiên 7** | Tuyển Dụng ATS (Kanban Pipeline), Onboarding & Cổng Tuyển Dụng SEO | **Hoàn tất** | 100% Pass |
| **Phiên 8** | Đánh Giá KPI, Ma Trận 9-Box, LMS Đào Tạo & Khép Kín Chuỗi Tính Lương C&B | **Hoàn tất** | 100% Pass |
| **Phiên 9** | Executive People Analytics, Tối Ưu Hiệu Năng & Sẵn Sàng Production | **Hoàn tất** | 100% Pass |
| **Phiên A** | Đối Soát Tài Liệu, Đóng Nợ P0 (`P0-02`, `P0-04`), Khử 15 Warning & Dọn Repo | **Hoàn tất** | 249 / 255 ca thực thi (2/2 test Phiên A xanh) |
| **Phiên B** | Nâng Lên **.NET 10 LTS (`net10.0`)**, Đồng Bộ Gói 10.0.14, Build & Test Xanh | **Hoàn tất** | **255 / 255 ca thực thi XANH 100% (0 Warning, 0 Error)** |
| **Phiên C1** | **ASP.NET Core Identity (`IdentityUser<int>`), `LegacyCompatiblePasswordHasher` & Vai Trò `CUSTOMER`** | **Hoàn tất** | **261 / 261 ca thực thi XANH 100% (0 Warning, 0 Error)** |

---

## CHI TIẾT KẾT QUẢ PHIÊN C1: ASP.NET CORE IDENTITY VÀ VAI TRÒ KHÁCH HÀNG (CUSTOMER) (2026-11-08)

### 1. Báo cáo hiện trạng trước khi sửa code (Nhiệm vụ 1)
- **Các thành phần xác thực & phân quyền đã rà soát:**
  - `Controllers/AuthController.cs`: xử lý `Login`, `Logout`, `Register`, `ChangePassword`, `SignInUserPrincipalAsync`, `RedirectBasedOnRole`.
  - `Helpers/MatKhauHelper.cs`: băm PBKDF2-SHA256 100.000 vòng (salt 16 byte), kiểm chứng SHA-256 hex 64 ký tự cũ (`IsLegacyHash`), `VerifyDummy`, `KiemTraDoManh`, `GenerateTemporaryPassword`.
  - `Services/LoginThrottleService.cs` & `src/ERP.Domain/Entities/LoginAttempt.cs`: khóa tạm thời theo cặp `(Username, IpAddress)` sau 5 lần sai (`FailuresBeforeLockout = 5`), thời gian khóa lũy tiến 1 → 2 → 4 → 8 → 16 → tối đa 30 phút (`MaxLockout`), tự xóa sau 24 giờ (`ForgetAfter`).
  - `Services/Security/CookieSecurityStampValidator.cs`: kiểm tra `IsActive` và `SecurityStamp` từ database trên mỗi request (`OnValidatePrincipal`).
  - `src/ERP.Infrastructure/Security/PermissionService.cs`: tính quyền hiệu lực từ `IsSuperAdmin`, `Roles` -> `RolePermissions` và `UserPermissions` (`IsGranted`), có cache qua `IAppCacheService`.
  - `src/ERP.Domain/Entities/AuthEntities.cs`: thực thể `User`, `Role`, `Module`, `Permission`, `UserRole`, `RolePermission`, `UserPermission`.
  - `Data/DbInitializer.cs` & `Program.cs`: khởi tạo vai trò/quyền qua `SeedHistory`, tài khoản `admin` & `quantritong`, cấu hình Cookie `ERP_HRM_CMS_AuthCookie`.
- **Danh sách mọi nơi đọc/ghi các thuộc tính bảo mật (đúng tên thuộc tính trong code):**
  - `PasswordHash` & `PasswordSalt`: `src/ERP.Domain/Entities/AuthEntities.cs`, `Controllers/AuthController.cs` (`Login`, `Register`, `ChangePassword`), `Controllers/QuanTriTaiKhoanController.cs` (`TaoMoi`, `DatLaiMatKhau`), `Data/DbInitializer.cs` (`SeedAdminAccountsAsync`, `CreateSuperAdminAccountAsync`, `AssignDemoRolesAsync`), `Services/HRM/EmployeeService.cs` (`CreateEmployeeAsync`, `CreateQuickAccountAsync`), `Services/HRM/SelfServicePortalService.cs` (`VerifyPasswordAsync`), và các file test (`TestHelpers.cs`, `Phase1ArchitectureAndSecurityTests.cs`, `Phase4CoreHrm360Tests.cs`, `Phase5AttendanceAndLeaveTests.cs`, `Phase6SelfServicePortalTests.cs`, `UiPhaseATests.cs`).
  - `IsSuperAdmin`: `src/ERP.Domain/Entities/AuthEntities.cs`, `Controllers/AuthController.cs`, `Controllers/BanHangController.cs`, `Controllers/DonTuController.cs`, `Controllers/HomeController.cs`, `Controllers/MenuController.cs`, `Controllers/QuanTriTaiKhoanController.cs`, `Data/DbInitializer.cs`, `Filters/KiemTraQuyenAttribute.cs`, `Models/ViewModels/TaiKhoanViewModels.cs`, `Services/HRM/LeaveManagementService.cs`, `Services/Security/SuperAdminService.cs`, `src/ERP.Application/Common/Interfaces/ICurrentUserService.cs`, `src/ERP.Application/Common/Security/DataScopeService.cs`, `LeaveApprovalPolicy.cs`, `PosDiscountPolicy.cs`, `src/ERP.Infrastructure/Identity/CurrentUserService.cs`, `src/ERP.Infrastructure/Security/PermissionService.cs`.
  - `IsActive`: `src/ERP.Domain/Entities/AuthEntities.cs`, `Controllers/AuthController.cs` (`Login`, `Register`, `ChangePassword`), `Controllers/QuanTriTaiKhoanController.cs`, `Data/DbInitializer.cs`, `Services/HRM/EmployeeService.cs`, `Services/HRM/HrDecisionService.cs`, `Services/Security/CookieSecurityStampValidator.cs`, `Services/Security/SuperAdminService.cs`, `src/ERP.Infrastructure/Security/PermissionService.cs`.
  - `MustChangePassword`: `src/ERP.Domain/Entities/AuthEntities.cs`, `Controllers/AuthController.cs`, `Controllers/QuanTriTaiKhoanController.cs`, `Models/ViewModels/TaiKhoanViewModels.cs`, `Services/HRM/EmployeeService.cs`, `src/ERP.Application/Common/Interfaces/ICurrentUserService.cs`, `src/ERP.Infrastructure/Identity/CurrentUserService.cs`, `src/ERP.Infrastructure/Middleware/MustChangePasswordMiddleware.cs`.
  - `SecurityStamp`: `src/ERP.Domain/Entities/AuthEntities.cs`, `src/ERP.Infrastructure/Persistence/AppDbContext.cs`, `Controllers/AuthController.cs` (`ChangePassword`, `SignInUserPrincipalAsync`), `Controllers/QuanTriTaiKhoanController.cs` (`Sua`, `KhoaMoKhoa`, `LuuPhanQuyen`, `GanNhanhVaiTro`, `DatLaiMatKhau`), `Services/HRM/EmployeeService.cs`, `Services/HRM/HrDecisionService.cs`, `Services/Security/CookieSecurityStampValidator.cs`, `src/ERP.Application/Common/Interfaces/ICurrentUserService.cs`.
  - `EmployeeId`: `src/ERP.Domain/Entities/AuthEntities.cs`, `src/ERP.Infrastructure/Persistence/AppDbContext.cs` (`IX_Users_EmployeeId`), `Controllers/AuthController.cs`, `Controllers/QuanTriTaiKhoanController.cs`, `Services/HRM/EmployeeService.cs`, `Services/HRM/HrDecisionService.cs`, `src/ERP.Application/Common/Interfaces/ICurrentUserService.cs`, `src/ERP.Infrastructure/Identity/CurrentUserService.cs`, cùng các controller/service HRM truy vấn theo nhân viên.
  - `LockedUntilUtc` & `FailedCount`: `src/ERP.Domain/Entities/LoginAttempt.cs`, `Services/LoginThrottleService.cs` (`GetRemainingLockoutAsync`, `RecordFailureAsync`, `ClearAsync`), `tests/ERP_HRM_CMS.Tests/VietnamTimeTests.cs`.
- **Các file test liên quan:** `AuthTests.cs`, `LoginThrottleTests.cs`, `SecurityTests.cs`, `SecurityHardeningTests.cs`, `SeedGuardTests.cs`, `RoleSeedTests.cs`, `PermissionSeedOnceTests.cs`, `PasswordPolicyTests.cs`, `Phase1ArchitectureAndSecurityTests.cs`.

### 2. Các hạng mục đã triển khai trong Phiên C1
1. **Tích hợp ASP.NET Core Identity (`AppUser` / `User : IdentityUser<int>`)**:
   - Thêm gói `Microsoft.Extensions.Identity.Stores` (`10.0.14`) vào `ERP.Domain` và `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (`10.0.14`) vào `ERP.Infrastructure` (vẫn đảm bảo `CleanArchitecture_DependencyRule_IsStrictlyEnforced` xanh 100%).
   - Cho `User` (cùng alias toàn cục `AppUser`) kế thừa `IdentityUser<int>`, giữ nguyên kiểu khóa `int` và toàn bộ các cột nghiệp vụ hiện có.
   - Cho `AppDbContext` kế thừa `IdentityUserContext<User, int, IdentityUserClaim<int>, IdentityUserLogin<int>, IdentityUserToken<int>>`, giữ nguyên các bảng `Roles`, `UserRoles`, `Permissions`, `RolePermissions`, `UserPermissions` làm nguồn phân quyền duy nhất.
2. **Xây dựng `LegacyCompatiblePasswordHasher : IPasswordHasher<User>`**:
   - Hỗ trợ đồng thời: (1) Hash chuẩn Identity v3 (`PBKDF2-HMAC-SHA512`), (2) Hash PBKDF2-SHA256 cũ (`MatKhauHelper` + `PasswordSalt`), (3) Hash SHA-256 hex 64 ký tự cũ.
   - Trả về `PasswordVerificationResult.SuccessRehashNeeded` khi đăng nhập thành công với hash cũ để `AuthController.Login` tự động nâng cấp lên chuẩn Identity v3.
3. **Migration `20261107185412_PhaseC1_AspNetCoreIdentity_And_CustomerRole`**:
   - Tạo bảng `AspNetUsers`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`.
   - Copy nguyên vẹn 100% dữ liệu từ bảng `Users` sang `AspNetUsers` (giữ `Id`, `EmployeeId`, `Username`, `PasswordHash`, `PasswordSalt`, `IsActive`, `IsSuperAdmin`, `MustChangePassword`, `SecurityStamp`, `LastLoginAt`, `CreatedAt`).
   - **Không xóa bảng `Users` trong Phiên C1**; có đầy đủ `Up` và `Down`.
4. **Cơ chế Cookie xác thực duy nhất & Khóa đăng nhập**:
   - Dùng duy nhất `CookieAuthenticationDefaults.AuthenticationScheme` (`ERP_HRM_CMS_AuthCookie`) kèm `CookieSecurityStampValidator.ValidateAsync`, không sinh hai cookie song song.
   - Kết hợp `IdentityOptions.Lockout` (`AccessFailedCount`, `LockoutEnd` trên `AspNetUsers`) và `LoginThrottleService` (bảng `LoginAttempts` khóa theo `Username + IP`, 5 lần sai, khóa tăng dần tối đa 30 phút).
5. **Vai trò `CUSTOMER`**:
   - Seed vai trò `CUSTOMER` (idempotent qua `SeedHistory` với khóa `ROLE:CUSTOMER`), không có quyền quản trị nào (`0` permission).
   - Đăng ký công khai (`POST /Auth/Register`) gán vai trò `CUSTOMER` thay cho `EMPLOYEE` và kích hoạt ngay (`IsActive = true`).
   - Cập nhật `RedirectBasedOnRole`: `CUSTOMER` chuyển hướng về `/`, `EMPLOYEE` về `/CongNhanVien`, `SUPERADMIN` và các nhóm admin về đúng trang quản trị.

### 3. Số liệu kiểm chứng Migration & Đăng nhập Rehash trên `ERP_HRM_CMS_DB_COPY`
- **Sao lưu & tạo bản sao:** Đã sao lưu `ERP_HRM_CMS_DB` ra `ERP_HRM_CMS_DB_c1.bak` và khôi phục thành `ERP_HRM_CMS_DB_COPY`.
- **Đối chiếu số dòng sau khi chạy `dotnet ef database update` trên `ERP_HRM_CMS_DB_COPY`:**
  - Số dòng bảng `Users`: **12 dòng**
  - Số dòng bảng `AspNetUsers`: **12 dòng**
  - Số dòng khớp toàn bộ (`Id`, `Username`, `PasswordHash`, `PasswordSalt`, `SecurityStamp`): **12 / 12 dòng (100%)**.
- **Kiểm chứng đăng nhập bằng mật khẩu cũ & tự động Rehash trên `ERP_HRM_CMS_DB_COPY`:**
  - `admin` (`SUPERADMIN`): Đăng nhập `302 Found` → `/PhanQuyen/MaTranPhanQuyen` | Hash nâng cấp từ PBKDF2 cũ (44 ký tự `mBaziiyX...`) sang Identity v3 (84 ký tự `AQAAAAIA...`).
  - `quantritong` (`SUPERADMIN`): Đăng nhập `302 Found` → `/PhanQuyen/MaTranPhanQuyen` | Hash nâng cấp từ PBKDF2 cũ (44 ký tự `dhEb9P6L...`) sang Identity v3 (84 ký tự `AQAAAAIA...`).
  - `nhanvien_c1` (`EMPLOYEE`): Đăng nhập `302 Found` → `/CongNhanVien` | Hash nâng cấp từ PBKDF2 cũ (44 ký tự `mBaziiyX...`) sang Identity v3 (84 ký tự `AQAAAAIA...`).
  - `NVBH` (`SALES_STAFF` - tài khoản thường): Đăng nhập `302 Found` → `/BanHang/POS` | Hash nâng cấp từ PBKDF2 cũ (44 ký tự `xpt1cwNo...`) sang Identity v3 (84 ký tự `AQAAAAIA...`).

---

## CHI TIẾT KẾT QUẢ PHIÊN B: NÂNG CẤP .NET 10 LTS, BUILD VÀ TEST XANH (2026-11-08)

### 1. Số liệu Baseline trước khi nâng & Sau khi nâng lên .NET 10 LTS (trên nhánh `feature/phase-b-dotnet10`)

| Tiêu chí | Baseline ban đầu (từ Phiên A, `net8.0`) | Baseline sau khi sửa 6 test lỗi `937c667` (`net8.0`) | Sau khi nâng lên **.NET 10 LTS (`net10.0`)** |
| :--- | :--- | :--- | :--- |
| **`dotnet --list-sdks`** | `9.0.312 [C:\Program Files\dotnet\sdk]` | `9.0.312` + `10.0.403 [C:\Users\MAi THU\.dotnet\sdk]` | `10.0.403 [C:\Users\MAi THU\.dotnet\sdk]` |
| **`dotnet --version`** | `9.0.312` | `10.0.403` | `10.0.403` |
| **`TargetFramework` (5 file `.csproj`)** | `net8.0` | `net8.0` | **`net10.0`** (0 còn `net8.0` trong repo) |
| **`dotnet build ERP_HRM_CMS.sln`** | **0 Error, 0 C# Warning** | **0 Error, 0 C# Warning** | **0 Error, 0 Warning** (cả Restore & Build) |
| **`dotnet test tests/ERP_HRM_CMS.Tests`** | **249 Passed, 6 Failed, 0 Skipped** (255 ca) | **255 Passed, 0 Failed, 0 Skipped** (255 ca) | **255 Passed, 0 Failed, 0 Skipped** (255 ca / 208 phương thức) |
| **`dotnet ef migrations list`** | 4 migrations | 4 migrations | **4 migrations nguyên vẹn** (`20261104074034_InitialBaseline`, `20261104082158_AddSeedHistory`, `20261104175015_V6_00_Security_And_Architecture`, `20261104191312_V6_01_EnterpriseHRM_Schema_And_Indexes`) |

### 2. Bảng đối chiếu thay đổi gói NuGet (5 file `.csproj`)

| Project | Gói NuGet | Phiên bản cũ (`net8.0`) | Phiên bản mới (`net10.0`) | Ghi chú / Lý do |
| :--- | :--- | :---: | :---: | :--- |
| `src/ERP.Domain` | *(Không dùng gói ngoài)* | — | — | Nâng `TargetFramework` từ `net8.0` lên `net10.0` |
| `src/ERP.Application` | `Microsoft.Extensions.DependencyInjection.Abstractions` | `9.0.2` | **`10.0.14`** | Đồng bộ với .NET 10 LTS |
| `src/ERP.Infrastructure` | `Microsoft.EntityFrameworkCore.SqlServer` | `8.0.0` | **`10.0.14`** | Nâng EF Core SQL Server lên 10.0.14 |
| `src/ERP.Infrastructure` | `Microsoft.Extensions.DependencyInjection.Abstractions` | `9.0.2` | *(Đã gỡ khai báo trùng)* | Khử cảnh báo `NU1510` của .NET 10 Package Pruning vì đã có sẵn từ `FrameworkReference Microsoft.AspNetCore.App` và `ERP.Application` |
| `src/ERP.Infrastructure` | `Serilog.AspNetCore` | `9.0.0` | **`10.0.0`** | Bản chính thức hỗ trợ .NET 10 |
| `src/ERP.Infrastructure` | `Serilog.Sinks.Console` | `6.0.0` | **`6.1.1`** | Bản ổn định mới nhất |
| `src/ERP.Infrastructure` | `Serilog.Sinks.File` | `6.0.0` | **`7.0.0`** | Bản ổn định mới nhất |
| `src/ERP.Infrastructure` | `System.Security.Cryptography.Pkcs` | *(Transitive `9.0.11`)* | **`10.0.14`** | Ghim tường minh lên `10.0.14` để khử cảnh báo lỗ hổng bảo mật `NU1902`/`NU1903` từ gói bắc cầu |
| `ERP_HRM_CMS` | `Microsoft.EntityFrameworkCore.SqlServer` | `8.0.0` | **`10.0.14`** | Đồng bộ EF Core 10.0.14 |
| `ERP_HRM_CMS` | `Microsoft.EntityFrameworkCore.Tools` | `8.0.0` | **`10.0.14`** | Đồng bộ EF Core Tools 10.0.14 |
| `ERP_HRM_CMS` | `ClosedXML` | `0.105.1` | **`0.105.1`** | Giữ nguyên vì `0.105.1` đã là bản ổn định mới nhất trên NuGet và tương thích hoàn toàn với .NET 10 |
| `tests/ERP_HRM_CMS.Tests` | `Microsoft.AspNetCore.Mvc.Testing` | `8.0.0` | **`10.0.14`** | Đồng bộ TestServer / WebApplicationFactory với ASP.NET Core 10 |
| `tests/ERP_HRM_CMS.Tests` | `Microsoft.NET.Test.Sdk` | `17.8.0` | **`18.11.0`** | Bản ổn định mới nhất hỗ trợ .NET 10 |
| `tests/ERP_HRM_CMS.Tests` | `xunit` | `2.6.2` | **`2.9.3`** | Bản ổn định mới nhất của nhánh xUnit v2 |
| `tests/ERP_HRM_CMS.Tests` | `xunit.runner.visualstudio` | `2.5.4` | **`2.8.2`** | Nâng lên bản ổn định mới nhất của nhánh `2.x` đồng bộ với `xunit 2.9.3` (nhánh `3.x`/`4.x` dành cho cấu trúc xUnit v3) |

### 3. Sửa 6 test lỗi có sẵn từ commit `937c667` (theo xác nhận ở Bước 1)
- `Controllers/AuthController.cs` (`Register` POST, dòng 191–219): Khôi phục phản hồi đồng nhất (`ThongBaoDangKyChung` + chuyển hướng `RedirectToAction(nameof(Login))` và gọi `MatKhauHelper.VerifyDummy`) khi tên đăng nhập đã tồn tại hoặc tạo mới, đóng lỗ hổng dò tên đăng nhập và giúp `SecurityHardeningTests.DangKy_TenDaTonTaiVaTenMoi_TraVeCungPhanHoi` đạt Xanh.
- `Views/Shared/_PublicLayout.cshtml` (dòng 165): Gắn lại `id="psSearchToggle"` cho nút mở thanh tìm kiếm trên Header công khai, giúp cả 5 ca `[InlineData]` của `PublicPagesTests.Public_Pages_Render_For_Anonymous_Visitors` đạt Xanh.

### 4. Kết quả Smoke Test thủ công trên Profile `http` (`http://localhost:5129`, .NET 10.0)
1. **Khởi động ứng dụng (`dotnet run --launch-profile http`) & Kiểm tra `/health`**:
   - Phản hồi `200 OK` — `status: "Healthy"` (`103.57 ms`).
2. **Mở trang đăng nhập (`GET /Auth/Login`) & Đăng nhập tài khoản `admin` (`POST /Auth/Login`)**:
   - `GET /Auth/Login` → `200 OK`.
   - `POST /Auth/Login` → `302 Found` (cấp cookie `ERP_HRM_CMS_AuthCookie`, chuyển hướng `/PhanQuyen/MaTranPhanQuyen`).
3. **Mở các phân hệ chính sau khi đăng nhập**:
   - **Dashboard (`GET /Home/Index`)**: `200 OK` (`57.351 bytes`).
   - **POS Bán hàng (`GET /BanHang/POS`)**: `200 OK` (`37.548 bytes`).
   - **Trang Kho (`GET /Kho/Index`)**: `200 OK` (`64.887 bytes`).
   - **Trang Sản phẩm (`GET /SanPham/Index`)**: `200 OK` (`68.552 bytes`).
4. **Đăng xuất (`POST /Auth/Logout`) & Kiểm tra chặn phiên cũ**:
   - `POST /Auth/Logout` → `302 Found` về `/Auth/Login`.
   - Thử truy cập lại `GET /Home/Index` sau khi đăng xuất → bị chặn `302 Found` về `/Auth/Login?ReturnUrl=%2FHome%2FIndex`.
5. **Đăng nhập lại (`POST /Auth/Login`) & Mở lại Dashboard**:
   - `POST /Auth/Login` → `302 Found`, `GET /Home/Index` → `200 OK` (`57.290 bytes`).

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
   - Cài đặt **.NET 10 Hosting Bundle (ASP.NET Core Module v2 - ANCM)** từ Microsoft.
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
