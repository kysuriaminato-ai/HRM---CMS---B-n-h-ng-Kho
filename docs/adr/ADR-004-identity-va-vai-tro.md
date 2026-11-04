# ADR-004: Chuyển sang ASP.NET Core Identity, nâng .NET và ánh xạ vai trò

- **Trạng thái:** Đề xuất. Quyết định 2.1 (giữ .NET 8), 2.2 (Identity) và 2.3 (ánh xạ vai trò) đã được người dùng đồng ý. Quyết định 2.4 (phạm vi thực hiện) chờ xác nhận.
- **Ngày:** 2026-11-04
- **Liên quan:** `docs/ANALYSIS.md` (S8, S14, S15, P1-24, P1-25), `docs/ARCHITECTURE.md`

---

## 1. Bối cảnh

- Dự án yêu cầu ASP.NET Core Identity. Hiện tại repo tự quản bảng `Users` và băm mật khẩu PBKDF2 bằng `MatKhauHelper` (100.000 vòng, salt 16 byte). Hash SHA-256 cũ vẫn được nhận và nâng cấp khi đăng nhập.
- Repo đang dùng .NET 8, hết hỗ trợ **10/11/2026**.
- Role hiện có: `SUPERADMIN`, `ADMIN_SANPHAM`, `ADMIN_DONHANG`, `ADMIN_NHANVIEN`, `ADMIN_BANGLUONG`, `ADMIN_KHO`, `ADMIN_MENU`, `ADMIN_CONTENT`, `HR_MANAGER`, `SALES_STAFF`, `SALES_DIRECTOR`, `WAREHOUSE_STAFF`, `EMPLOYEE`. Quyền gắn theo mã (`product.*`, `order.*`, `hrm.*`...) qua bảng `RolePermissions`, `UserPermissions`.
- Yêu cầu phiên 2 liệt kê role: SuperAdmin, Admin, HR, Accountant, WarehouseStaff, Sales, ContentEditor, Employee.

## 2. Quyết định

### 2.1 Giữ .NET 8 (đã chấp thuận)
Giữ `TargetFramework` là `net8.0`. .NET 8 hết hỗ trợ 10/11/2026, nhưng dự án chưa triển khai production nên chấp nhận được. Sẽ nâng lên .NET LTS mới trước khi triển khai thật.

### 2.2 Dùng ASP.NET Core Identity, giữ mật khẩu cũ (đã chấp thuận)
- Dùng `IdentityUser<int>` và `IdentityRole<int>` để giữ kiểu khóa `int` của bảng hiện tại.
- Viết `LegacyCompatiblePasswordHasher : IPasswordHasher<AppUser>`:
  - Verify: nhận hash PBKDF2 hiện có (`MatKhauHelper.Verify`) và hash SHA-256 cũ.
  - Hash mới: PBKDF2 với số vòng mới. Khi đăng nhập thành công với hash cũ, cập nhật sang hash mới (cùng cơ chế nâng cấp hiện có).
- Bảng: migration đổi `Users` sang `AspNetUsers`, copy dữ liệu và giữ `PasswordHash`, `PasswordSalt`. Trường `PasswordSalt` được giữ lại trong entity mở rộng để hasher đọc được hash cũ.
- Khóa đăng nhập: dùng `UserLockoutEnabled` của Identity với 5 lần sai, tăng thời gian khóa. Bảng `LoginAttempts` hiện tại giữ lại cho đến khi chuyển xong, rồi bỏ.
- Mọi thay đổi bảng qua migration, kèm file `.sql` đi kèm (theo quy tắc dự án). Script copy dữ liệu viết thành migration có `Up` rõ ràng, không chạy SQL tay.

### 2.3 Ánh xạ vai trò (đã chấp thuận: giữ bảng cũ, thêm Accountant)

| Role yêu cầu | Role trong hệ thống | Ghi chú |
|---|---|---|
| SuperAdmin | `SUPERADMIN` | Giữ nguyên. Toàn quyền, chỉ SuperAdmin sửa vai trò `SUPERADMIN` |
| Admin | Quản trị viên nhóm: gán một hoặc nhiều `ADMIN_*` | Không tạo role `ADMIN` riêng. Đề xuất, cần xác nhận |
| HR | `HR_MANAGER` | Quyền `hrm.*` |
| Accountant | `ACCOUNTANT` (**tạo mới**) | Quyền `scm.debt.*`, `hrm.payroll.view`, `report.*` |
| WarehouseStaff | `WAREHOUSE_STAFF` | Quyền `inventory.*`, `product.view` |
| Sales | `SALES_STAFF` | Quyền `order.pos`, `order.view`, `product.view` |
| ContentEditor | `ADMIN_CONTENT` | Quyền `content.*`, `layout.manage` (cần rà lại, đây là quyền khá rộng) |
| Employee | `EMPLOYEE` | Quyền `hrm.self.service` |

Quy tắc: không xóa role cũ trong giai đoạn chuyển đổi. Vai trò alias cũ (`QTVP`, `SalesStaff`...) vẫn không được tạo lại (theo README).

### 2.4 Phạm vi thực hiện (chờ xác nhận)
- **2b:** tạo 4 project, chuyển `AppDbContext` và entity sang `Infrastructure`/`Domain`, giữ nguyên hành vi.
- **2c:** Identity, `LegacyCompatiblePasswordHasher`, policy theo module, khóa tài khoản, Serilog, Health Check.

## 3. Hệ quả

- Tích cực: đúng yêu cầu dự án. Có sẵn cơ chế khóa tài khoản, đổi mật khẩu, xác nhận email của Identity để dùng sau này.
- Rủi ro: migration bảng người dùng. Cần sao lưu trước, thử trên bản sao của `ERP_HRM_CMS_DB`. Session và cookie cũ sẽ bị vô hiệu sau khi đổi scheme.
- Rủi ro: không build được trong môi trường hiện tại. Cần máy có .NET 10 SDK và SQL Server trước khi làm 2b.
- Tác động đến phân quyền: `KiemTraQuyenAttribute` và `PermissionService` giữ nguyên, nhưng role claim đổi nguồn từ Identity. Test `AuthorizationDefaultTests` và `SecurityHardeningTests` phải chạy lại.

## 4. Kiểm chứng sau khi chấp nhận

- `dotnet build` và `dotnet test` xanh trên .NET 10.
- Người dùng cũ đăng nhập được bằng mật khẩu hiện tại (cả hash PBKDF2 và SHA-256 cũ).
- Vai trò cũ không mất quyền: so sánh `GetEffectivePermissionsAsync` trước và sau migration cho từng user.
- Khóa tài khoản sau 5 lần sai hoạt động như trước.
