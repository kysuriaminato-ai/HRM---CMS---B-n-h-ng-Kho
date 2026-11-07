# ADR-004: Chuyển sang ASP.NET Core Identity, nâng .NET và ánh xạ vai trò

- **Trạng thái:** Đã chấp thuận và triển khai **Phiên B** (.NET 10 LTS) & **Phiên C1** (ASP.NET Core Identity + vai trò `CUSTOMER`).
- **Ngày:** 2026-11-04 (Cập nhật Phiên C1: 2026-11-08)
- **Liên quan:** `docs/ANALYSIS.md` (S8, S14, S15, P1-24, P1-25), `docs/ARCHITECTURE.md`, `docs/PROGRESS.md`

---

## 1. Bối cảnh

- Dự án yêu cầu ASP.NET Core Identity. Trước Phiên C1, repo tự quản bảng `Users` và băm mật khẩu PBKDF2 bằng `MatKhauHelper` (100.000 vòng, salt 16 byte), đồng thời hỗ trợ kiểm chứng và nâng cấp hash SHA-256 hex 64 ký tự cũ.
- Ở **Phiên B**, toàn bộ 5 project trong solution đã được nâng từ `.NET 8` (`net8.0`) lên **.NET 10 LTS (`net10.0`)** với các gói EF Core / Identity / Mvc.Testing `10.0.14`.
- Hệ thống phân quyền hiện có dùng các bảng `Roles`, `UserRoles`, `Permissions`, `RolePermissions`, `UserPermissions` với các vai trò: `SUPERADMIN`, `ADMIN_SANPHAM`, `ADMIN_DONHANG`, `ADMIN_NHANVIEN`, `ADMIN_BANGLUONG`, `ADMIN_KHO`, `ADMIN_MENU`, `ADMIN_CONTENT`, `HR_MANAGER`, `SALES_STAFF`, `SALES_DIRECTOR`, `WAREHOUSE_STAFF`, `BRANCH_MANAGER`, `DEPARTMENT_HEAD`, `EMPLOYEE` và bổ sung `CUSTOMER` tại Phiên C1.

## 2. Quyết định

### 2.1 Nâng lên .NET 10 LTS (đã hoàn tất ở Phiên B)
Toàn bộ 5 project (`ERP.Domain`, `ERP.Application`, `ERP.Infrastructure`, `ERP_HRM_CMS`, `ERP_HRM_CMS.Tests`) đã nâng `TargetFramework` lên `net10.0` (0 lỗi, 0 cảnh báo, toàn bộ test xanh).

### 2.2 Dùng ASP.NET Core Identity (`IdentityUser<int>`), giữ mật khẩu cũ & khóa ngoại (đã triển khai ở Phiên C1)
- Thực thể người dùng [`User`](file:///c:/HRM%20-%20CMS%20-%20B%C3%A1n%20h%C3%A0ng%20&%20Kho/src/ERP.Domain/Entities/AuthEntities.cs) (cùng alias toàn cục `AppUser`) kế thừa `IdentityUser<int>`, giữ khóa chính `Id` kiểu `int` và toàn bộ các cột nghiệp vụ hiện có (`EmployeeId`, `Username`, `PasswordHash`, `PasswordSalt`, `IsActive`, `IsSuperAdmin`, `MustChangePassword`, `SecurityStamp`, `LastLoginAt`, `CreatedAt`) để không làm gãy bất kỳ khóa ngoại nào (`UserRoles`, `UserPermissions`, `Notifications`, `AuditLogs`, `Orders`, `LeaveRequests`...).
- [`AppDbContext`](file:///c:/HRM%20-%20CMS%20-%20B%C3%A1n%20h%C3%A0ng%20&%20Kho/src/ERP.Infrastructure/Persistence/AppDbContext.cs) kế thừa `IdentityUserContext<User, int, IdentityUserClaim<int>, IdentityUserLogin<int>, IdentityUserToken<int>>`, ánh xạ thực thể `User` vào bảng `AspNetUsers`.
- Viết [`LegacyCompatiblePasswordHasher : IPasswordHasher<User>`](file:///c:/HRM%20-%20CMS%20-%20B%C3%A1n%20h%C3%A0ng%20&%20Kho/Services/Security/LegacyCompatiblePasswordHasher.cs):
  - **Verify:** Nhận diện và kiểm chứng 3 định dạng:
    1. Hash chuẩn ASP.NET Core Identity v3 (`PBKDF2-HMAC-SHA512`, tiền tố `0x01`, Base64 84 ký tự) → trả về `PasswordVerificationResult.Success`.
    2. Hash PBKDF2-SHA256 cũ (`MatKhauHelper.Verify` với cột `PasswordSalt`, Base64 44 ký tự) → trả về `PasswordVerificationResult.SuccessRehashNeeded`.
    3. Hash SHA-256 hex 64 ký tự cũ (`MatKhauHelper.IsLegacyHash`) → trả về `PasswordVerificationResult.SuccessRehashNeeded`.
  - **Rehash tự động:** Khi đăng nhập thành công với hash cũ (`SuccessRehashNeeded`), [`AuthController`](file:///c:/HRM%20-%20CMS%20-%20B%C3%A1n%20h%C3%A0ng%20&%20Kho/Controllers/AuthController.cs) gọi `_userManager.PasswordHasher.HashPassword` để nâng cấp ngay sang chuẩn Identity v3 và lưu xuống `AspNetUsers`.
- **Migration (`20261107185412_PhaseC1_AspNetCoreIdentity_And_CustomerRole`):**
  - Tạo bảng `AspNetUsers`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`.
  - Copy nguyên vẹn 100% dòng từ bảng `Users` sang `AspNetUsers` bằng `SET IDENTITY_INSERT [AspNetUsers] ON` (giữ nguyên `Id`, `EmployeeId`, `Username`, `PasswordHash`, `PasswordSalt`, `IsActive`, `IsSuperAdmin`, `MustChangePassword`, `SecurityStamp`, `LastLoginAt`, `CreatedAt`).
  - Chuyển các khóa ngoại sang `AspNetUsers` nhưng **giữ nguyên bảng `Users` trong Phiên C1** (không xóa bảng `Users` cho đến khi xác nhận ở phiên sau). Có `Up` và `Down` rõ ràng.
- **Khóa đăng nhập (Lockout):**
  - Cấu hình `IdentityOptions.Lockout` (`MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 1 phút`, `AllowedForNewUsers = true`) và đồng bộ `AccessFailedCount` / `LockoutEnd` trên `AspNetUsers` kết hợp với [`LoginThrottleService`](file:///c:/HRM%20-%20CMS%20-%20B%C3%A1n%20h%C3%A0ng%20&%20Kho/Services/LoginThrottleService.cs) trên bảng `LoginAttempts` (5 lần sai theo cặp `Username + IP`, khóa lũy tiến 1 → 2 → 4 → 8 → 16 → tối đa 30 phút, không khóa vĩnh viễn). Hai cơ chế được đồng bộ thống nhất trong `AuthController.Login`, không xung đột nhau.
- **Cơ chế Cookie xác thực duy nhất (Mục 6):**
  - **Quyết định:** Giữ duy nhất một scheme xác thực `CookieAuthenticationDefaults.AuthenticationScheme` với cookie `ERP_HRM_CMS_AuthCookie` (đặt làm `DefaultScheme`, `DefaultAuthenticateScheme`, `DefaultChallengeScheme`, `DefaultSignInScheme`), không sinh thêm cookie `.AspNetCore.Identity.Application` thứ hai.
  - **Lý do chọn:**
    1. Toàn bộ hệ thống phân quyền (`KiemTraQuyenAttribute`, `CurrentUserService`, `DataScopeService`), middleware cưỡng chế đổi mật khẩu lần đầu (`MustChangePasswordMiddleware`), và bộ kiểm thử tích hợp hiện hữu đều đọc Claims (`ClaimTypes.NameIdentifier`, `ClaimTypes.Role`, `ErpClaimTypes.IsSuperAdmin`, `ErpClaimTypes.EmployeeId`, `ErpClaimTypes.DepartmentId`, `ErpClaimTypes.BranchId`, `SecurityStamp`) từ scheme duy nhất này.
    2. Giữ trọn vẹn [`CookieSecurityStampValidator.ValidateAsync`](file:///c:/HRM%20-%20CMS%20-%20B%C3%A1n%20h%C3%A0ng%20&%20Kho/Services/Security/CookieSecurityStampValidator.cs) trên mỗi request để thu hồi phiên tức thì (`RejectPrincipal` + `SignOutAsync`) ngay khi tài khoản bị khóa (`IsActive = false`), đổi quyền hoặc đổi mật khẩu (`SecurityStamp` thay đổi).
    3. Giữ nguyên chính sách `CookieSecurePolicy.Always` trên Production (`SameAsRequest` trên Development HTTP) và `HttpOnly = true`, `SameSite = Lax`.

### 2.3 Ánh xạ vai trò & Phân quyền (giữ bảng cũ, thêm `CUSTOMER`)
- Giữ nguyên các bảng `Roles`, `UserRoles`, `Permissions`, `RolePermissions`, `UserPermissions` làm nguồn phân quyền duy nhất. Không dùng `IdentityRole<int>` để tránh tạo hai hệ thống vai trò song song.
- Bổ sung vai trò `CUSTOMER` (seed idempotent qua `SeedHistory` với khóa `ROLE:CUSTOMER`, không có bất kỳ quyền quản trị nào trong `RolePermissions`).
- Đăng ký công khai (`/Auth/Register`) gán vai trò `CUSTOMER` thay cho `EMPLOYEE`, kích hoạt ngay (`IsActive = true`), và khi đăng nhập chuyển hướng về trang chủ cửa hàng công khai (`/`).

| Role yêu cầu | Role trong hệ thống | Quyền / Đích chuyển hướng sau đăng nhập |
|---|---|---|
| SuperAdmin | `SUPERADMIN` | Toàn quyền hệ thống → `/PhanQuyen/MaTranPhanQuyen` |
| Admin nhóm | `ADMIN_MENU`, `ADMIN_CONTENT`, `ADMIN_SANPHAM`, `ADMIN_DONHANG`, `ADMIN_NHANVIEN`, `ADMIN_BANGLUONG`, `ADMIN_KHO` | Quyền theo từng phân hệ → trang quản trị phân hệ tương ứng |
| HR / Quản lý | `HR_MANAGER`, `BRANCH_MANAGER`, `DEPARTMENT_HEAD` | Quyền nhân sự / phạm vi chi nhánh, phòng ban → `/NhanSu/Index` |
| Sales | `SALES_STAFF`, `SALES_DIRECTOR` | Quyền bán hàng / đơn hàng → `/BanHang/POS` |
| WarehouseStaff | `WAREHOUSE_STAFF` | Quyền tồn kho / nhập xuất → `/Kho/Index` |
| Employee | `EMPLOYEE` | Quyền `hrm.self.service` → `/CongNhanVien` (ESS Portal) |
| Customer | `CUSTOMER` (**tạo mới ở Phiên C1**) | Không có quyền quản trị (0 permission) → `/` (Trang chủ cửa hàng) |

### 2.4 Phạm vi thực hiện (đã xác nhận)
- **Phiên B (Hoàn tất):** Nâng toàn bộ solution lên `.NET 10 LTS` (`net10.0`), đồng bộ gói `10.0.14`, build 0 warning và test 255/255 xanh.
- **Phiên C1 (Hoàn tất):** Tích hợp `ASP.NET Core Identity` (`User : IdentityUser<int>`, `UserManager<User>`, `SignInManager<User>`), `LegacyCompatiblePasswordHasher`, migration sang `AspNetUsers` (giữ lại bảng `Users`), giữ hệ thống bảng `Roles`/`Permissions` hiện tại, thêm vai trò `CUSTOMER` cho đăng ký công khai.
- **Phiên C2 (Tiếp theo):** Tách khu vực Admin, Staff, Shop (Areas / Route conventions).
- **Phiên D (Tiếp theo):** Tùy biến ngành hàng trang sức theo xác nhận.

## 3. Hệ quả & Kiểm chứng thực tế (Phiên C1)

- `dotnet build ERP_HRM_CMS.sln`: **0 Warning, 0 Error** trên `.NET 10.0`.
- `dotnet test tests/ERP_HRM_CMS.Tests`: **261/261 ca kiểm thử đạt Xanh 100%** (tăng từ 255 lên 261 ca, bổ sung 6 test cho `LegacyCompatiblePasswordHasher` và vai trò `CUSTOMER`).
- Trên bản sao `ERP_HRM_CMS_DB_COPY`:
  - Số dòng bảng `Users` (**12 dòng**) và `AspNetUsers` (**12 dòng**) khớp 100% cả `Id`, `Username`, `PasswordHash`, `PasswordSalt`, `SecurityStamp`.
  - Đăng nhập thành công với mật khẩu cũ và tự động nâng cấp (`SuccessRehashNeeded` → Identity v3 `AQAAAAIA...` dài 84 ký tự) cho cả 4 nhóm tài khoản: `admin`, `quantritong`, `nhanvien_c1` (`EMPLOYEE`), và `NVBH` (`SALES_STAFF`).
