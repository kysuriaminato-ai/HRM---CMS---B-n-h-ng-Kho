# Tiến độ dự án — ERP_HRM_CMS Enterprise

Cập nhật lần cuối: 2026-11-08 (Đã hoàn tất **PHIÊN C2 – Tách Khu Vực Admin, Staff, Shop (Giữ Nguyên Route) và Đóng RISK-AUTH-02**)

---

## CHI TIẾT PHIÊN C2: TÁCH KHU VỰC ADMIN, STAFF, SHOP (GIỮ NGUYÊN ROUTE) VÀ ĐÓNG RISK-AUTH-02 (2026-11-08)

### 1. Phân loại 34 Controller trước khi di chuyển (Mục A1–A2)

| STT | Controller | Khu vực (Area) | Vị trí file sau khi tách | Lý do (1 dòng) |
| :---: | :--- | :---: | :--- | :--- |
| 1 | `PublicController` | **Shop** (`[Area("Shop")]`) | `Areas/Shop/Controllers/PublicController.cs` | Trang chủ storefront công khai, tin tức, giới thiệu, liên hệ, tuyển dụng công khai, sitemap/robots. |
| 2 | `GioHangController` | **Shop** (`[Area("Shop")]`) | `Areas/Shop/Controllers/GioHangController.cs` | Quản lý giỏ hàng và đặt hàng trực tuyến dành cho khách mua sắm trên storefront. |
| 3 | `AuthController` | **Shop** (`[Area("Shop")]`) | `Areas/Shop/Controllers/AuthController.cs` | Đăng nhập, đăng xuất, đăng ký khách hàng (`CUSTOMER`) và đổi mật khẩu tài khoản. |
| 4 | `QuanTriTaiKhoanController` | **Admin** (`[Area("Admin")]`) | `Areas/Admin/Controllers/QuanTriTaiKhoanController.cs` | Quản trị tài khoản người dùng hệ thống, gán vai trò, khóa/mở khóa và đặt lại mật khẩu. |
| 5 | `PhanQuyenController` | **Admin** (`[Area("Admin")]`) | `Areas/Admin/Controllers/PhanQuyenController.cs` | Quản trị ma trận phân quyền hệ thống (`Roles`, `Permissions`, `RolePermissions`). |
| 6 | `ContentController` | **Admin** (`[Area("Admin")]`) | `Areas/Admin/Controllers/ContentController.cs` | Quản trị nội dung bài viết/trang CMS và xuất bản nội dung lên storefront. |
| 7 | `SanPhamController` | **Admin** (`[Area("Admin")]`) | `Areas/Admin/Controllers/SanPhamController.cs` | Quản trị danh mục mặt hàng, giá bán, giá vốn và cấu hình sản phẩm toàn hệ thống. |
| 8 | `DanhMucController` | **Admin** (`[Area("Admin")]`) | `Areas/Admin/Controllers/DanhMucController.cs` | Quản trị cây danh mục sản phẩm và danh mục bài viết CMS. |
| 9 | `BanHangController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/BanHangController.cs` | Nghiệp vụ bán hàng tại quầy (POS), tạo hóa đơn và lịch sử giao dịch bán hàng. |
| 10 | `KhoController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/KhoController.cs` | Nghiệp vụ quản lý tồn kho, nhập/xuất kho, điều chuyển và kiểm kê kho. |
| 11 | `CongNoController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/CongNoController.cs` | Nghiệp vụ theo dõi công nợ khách hàng/nhà cung cấp và thu/chi công nợ. |
| 12 | `DonHangController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/DonHangController.cs` | Nghiệp vụ xử lý đơn đặt hàng từ khách hàng cho nhân viên bán hàng/điều phối. |
| 13 | `PurchaseOrderController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/PurchaseOrderController.cs` | Nghiệp vụ đơn mua hàng (PO), duyệt PO và nhận hàng từ nhà cung cấp. |
| 14 | `NhanSuController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/NhanSuController.cs` | Nghiệp vụ hồ sơ nhân sự 360°, sơ đồ tổ chức và quản lý nhân viên. |
| 15 | `ChamCongController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/ChamCongController.cs` | Nghiệp vụ chấm công, phân ca làm việc, bảng công tháng và heatmap chuyên cần. |
| 16 | `DonTuController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/DonTuController.cs` | Nghiệp vụ tạo và phê duyệt đơn xin nghỉ phép, tăng ca, giải trình công. |
| 17 | `BangLuongController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/BangLuongController.cs` | Nghiệp vụ tính lương C&B, chốt bảng lương tháng và chi trả lương nhân viên. |
| 18 | `DaoTaoController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/DaoTaoController.cs` | Nghiệp vụ quản lý khóa đào tạo nội bộ (LMS) và kết quả học tập của nhân viên. |
| 19 | `TuyenDungController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/TuyenDungController.cs` | Nghiệp vụ tuyển dụng ATS (Kanban ứng viên, lịch phỏng vấn, onboarding). |
| 20 | `DanhGiaKpiController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/DanhGiaKpiController.cs` | Nghiệp vụ đánh giá hiệu suất KPI định kỳ và ma trận nhân tài 9-Box. |
| 21 | `QuyetDinhNhanSuController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/QuyetDinhNhanSuController.cs` | Nghiệp vụ ban hành quyết định bổ nhiệm, điều chuyển, khen thưởng, kỷ luật, thôi việc. |
| 22 | `SelfServiceController` | **Staff** (`[Area("Staff")]`) | `Areas/Staff/Controllers/SelfServiceController.cs` | Cổng tự phục vụ nhân viên (ESS Portal `/CongNhanVien`): xem công, phiếu lương, gửi đơn. |
| 23 | `HomeController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/HomeController.cs` | Chứa cả Dashboard tổng quan kiêm trang lỗi dùng chung (`/Home/KhongCoQuyen`, `/Home/LoiHeThong`) — giữ ở gốc chờ xác nhận. |
| 24 | `NotificationController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/NotificationController.cs` | API chuông thông báo dùng chung trên thanh header của cả Admin và Staff — giữ ở gốc chờ xác nhận. |
| 25 | `PhieuInController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/PhieuInController.cs` | Tiện ích xuất/in biểu mẫu hóa đơn, phiếu kho và quản lý mẫu in — giữ ở gốc chờ xác nhận. |
| 26 | `UIController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/UIController.cs` | Trang showcase/tra cứu thành phần giao diện nội bộ — giữ ở gốc chờ xác nhận. |
| 27 | `VaiTroController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/VaiTroController.cs` | Quản lý danh mục vai trò nhưng chưa nêu trong danh sách gợi ý A2 — giữ ở gốc chờ xác nhận. |
| 28 | `MenuController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/MenuController.cs` | Cấu hình menu điều hướng động chưa nêu trong danh sách gợi ý A2 — giữ ở gốc chờ xác nhận. |
| 29 | `LayoutConfigController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/LayoutConfigController.cs` | Cấu hình giao diện/theme hệ thống chưa nêu trong danh sách gợi ý A2 — giữ ở gốc chờ xác nhận. |
| 30 | `AuditLogController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/AuditLogController.cs` | Tra cứu nhật ký kiểm toán hệ thống chưa nêu trong danh sách gợi ý A2 — giữ ở gốc chờ xác nhận. |
| 31 | `PhongBanController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/PhongBanController.cs` | Quản lý danh mục phòng ban chưa nêu trong danh sách gợi ý A2 — giữ ở gốc chờ xác nhận. |
| 32 | `HopDongController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/HopDongController.cs` | Quản lý hợp đồng lao động chưa nêu trong danh sách gợi ý A2 — giữ ở gốc chờ xác nhận. |
| 33 | `TaiSanNhanSuController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/TaiSanNhanSuController.cs` | Quản lý cấp phát tài sản nhân sự chưa nêu trong danh sách gợi ý A2 — giữ ở gốc chờ xác nhận. |
| 34 | `SupplierController` | **Chưa phân loại** *(Giữ ở gốc)* | `Controllers/SupplierController.cs` | Quản lý nhà cung cấp chưa nêu trong danh sách gợi ý A2 — giữ ở gốc chờ xác nhận. |

---

### 2. Kiểm tra giữ nguyên Route và Mã trạng thái trước & sau khi tách Area (Mục B)

Sử dụng `MapAreaControllerRoute` với mẫu `{controller}/{action}/{id?}` (không thêm tiền tố `/Admin`, `/Staff`, `/Shop`), kết hợp `AreaAwareUrlHelperFactory` để `Url.Action`, `RedirectToAction` và TagHelpers tự động ánh xạ tên controller về đúng Area:

| URL | Khu vực (Area) | Trước C2 (Ẩn danh / Đã đăng nhập Admin) | Sau C2 (Ẩn danh / Đã đăng nhập Admin) | Kết quả |
| :--- | :---: | :---: | :---: | :---: |
| `/`, `/Public/Index`, `/Public/TinTuc`, `/Public/GioiThieu`, `/Public/LienHe`, `/tuyen-dung`, `/sitemap.xml`, `/robots.txt` | `Shop` | `200 OK` / `200 OK` | `200 OK` / `200 OK` | **Giữ nguyên 100%** |
| `/GioHang`, `/Auth/Login`, `/Auth/Register` | `Shop` | `200 OK` / `200 OK` | `200 OK` / `200 OK` | **Giữ nguyên 100%** |
| `/Auth/ChangePassword` | `Shop` | `302 -> /Auth/Login` / `200 OK` | `302 -> /Auth/Login` / `200 OK` | **Giữ nguyên 100%** |
| `/QuanTriTaiKhoan`, `/PhanQuyen/MaTranPhanQuyen`, `/Content`, `/SanPham`, `/DanhMuc` | `Admin` | `302 -> /Auth/Login` / `200 OK` | `302 -> /Auth/Login` / `200 OK` | **Giữ nguyên 100%** |
| `/BanHang/POS`, `/Kho`, `/CongNo`, `/DonHang`, `/PurchaseOrder`, `/NhanSu`, `/ChamCong/BangCongThang`, `/ChamCong/Heatmap`, `/DonTu`, `/BangLuong`, `/DaoTao`, `/TuyenDung`, `/DanhGiaKpi`, `/QuyetDinhNhanSu`, `/CongNhanVien` | `Staff` | `302 -> /Auth/Login` / `200 OK` | `302 -> /Auth/Login` / `200 OK` | **Giữ nguyên 100%** |
| `/Home/Index`, `/VaiTro`, `/AuditLog`, `/PhongBan`, `/HopDong`, `/TaiSanNhanSu`, `/Supplier` | *(Gốc)* | `302 -> /Auth/Login` / `200 OK` | `302 -> /Auth/Login` / `200 OK` | **Giữ nguyên 100%** |

---

### 3. Phân quyền Area (C1–C4), Điều hướng (D), Layout (E) và Đóng `RISK-AUTH-02` (F)

- **C1–C4. Phân quyền theo khu vực (`Filters/AreaAuthorizationFilter.cs`):**
  - **Area `Admin`:** Yêu cầu đã đăng nhập, **chặn tuyệt đối vai trò `CUSTOMER`** (trừ trường hợp `IsSuperAdmin`), và yêu cầu người dùng là `SUPERADMIN` hoặc có quyền quản trị trong hệ thống. Giữ nguyên toàn bộ `[KiemTraQuyen(...)]` trên từng controller/action.
  - **Area `Staff`:** Yêu cầu đã đăng nhập và **chặn tuyệt đối người dùng chỉ có vai trò `CUSTOMER`** (redirect về `/Home/KhongCoQuyen` hoặc trả `403` cho API/AJAX). Giữ nguyên toàn bộ `[KiemTraQuyen(...)]` trên từng controller/action.
  - **Area `Shop`:** Cho phép ẩn danh truy cập các trang công khai (`PublicController`, `GioHangController`, `Login`/`Register`); bắt buộc đăng nhập đối với trang tài khoản (`/Auth/ChangePassword`).
- **D. Điều hướng sau đăng nhập (`Areas/Shop/Controllers/AuthController.cs` — `RedirectBasedOnRole`):**
  - Tài khoản `SUPERADMIN` hoặc có quyền quản trị Admin → `/PhanQuyen/MaTranPhanQuyen` (hoặc trang quản trị tương ứng).
  - Tài khoản `CUSTOMER` (không có quyền quản trị/nhân viên) → `/` (trang chủ Shop).
  - Tài khoản nhân viên (`EMPLOYEE` / nghiệp vụ) → module nghiệp vụ được cấp quyền hoặc mặc định `/CongNhanVien`.
- **E. Layout riêng từng khu vực:**
  - Tạo `Views/Shared/_AdminLayout.cshtml`, `Views/Shared/_StaffLayout.cshtml` (sao chép từ `_Layout.cshtml` hiện tại) và `Views/Shared/_ShopLayout.cshtml` (sao chép từ `_PublicLayout.cshtml` hiện tại), kèm `_ViewStart.cshtml` cho từng Area `Admin`, `Staff`, `Shop` mà không thay đổi giao diện.
- **F. Đóng `RISK-AUTH-02` (commit `d9ca67a`):**
  - `Areas/Admin/Controllers/QuanTriTaiKhoanController.cs`: loại bỏ mật khẩu mặc định `"Admin@123456"`; sinh mật khẩu ngẫu nhiên 12 ký tự (gồm chữ hoa, chữ thường, chữ số, ký tự đặc biệt qua `MatKhauHelper.GenerateTemporaryPassword(12)`), cưỡng chế `MustChangePassword = true`, và hiển thị mật khẩu tạm thời đúng 1 lần qua `TempData["ThongBao"]`.
  - `Data/DbInitializer.cs`: bảo toàn nguyên vẹn `PasswordHash` của `admin`, `quantritong` và các tài khoản đã tồn tại; thay thế `"Admin@123456"` ở dòng 646 và 710 bằng giá trị cấu hình `SeedAdmin:Password` — nếu chưa cấu hình thì tự động sinh mật khẩu ngẫu nhiên 12 ký tự và ghi ra Console đúng 1 lần.
  - Đã cập nhật mục 13 (`RISK-AUTH-02`) trong `docs/ANALYSIS.md` sang trạng thái **Đã xử lý tại Phiên C2** (ngày `2026-11-08`, commit `d9ca67a`).

---

### 4. Kết quả Build, Kiểm thử tự động (Mục G) và Kiểm thử thủ công

- **Kiểm tra schema (`dotnet ef migrations has-pending-model-changes`):** `No changes have been made to the model since the last migration.`
- **Build (`dotnet build ERP_HRM_CMS.sln --no-incremental`):** `Build succeeded. 0 Warning(s), 0 Error(s)`.
- **Số lượng test trước và sau Phiên C2:**
  - Trước C2 (cuối C1.1): **263 Passed, 0 Failed, 0 Skipped**.
  - Sau C2: **270 Passed, 0 Failed, 0 Skipped** (**+7 test mới** trong `tests/ERP_HRM_CMS.Tests/PhaseC2AreasAndSecurityTests.cs`):
    1. `Area_Admin_Blocks_Customer` — **PASS**
    2. `Area_Staff_Blocks_Customer` — **PASS**
    3. `Area_Shop_Public_Pages_Anonymous_Ok` — **PASS**
    4. `Routes_Unchanged_For_Listed_Urls` — **PASS**
    5. `Login_Redirects_By_Role_Admin_Employee_Customer` — **PASS**
    6. `Create_Account_Generates_Random_Password_With_MustChange` — **PASS**
    7. `Register_Still_Assigns_Customer` — **PASS**
- **Kiểm thử thủ công trên `ERP_HRM_CMS_DB` (chạy với `--Seed:Demo=false`):**
  ```text
  [admin] POST /Auth/Login -> 302 Location: /PhanQuyen/MaTranPhanQuyen
  [admin] GET /PhanQuyen/MaTranPhanQuyen -> 200
  [NVBH] POST /Auth/Login -> 302 Location: /BanHang/POS
  [NVBH] GET /BanHang/POS -> 200
  [khachtest02] POST /Auth/Login -> 302 Location: /
  [khachtest02] GET / -> 200
  [khachtest02] GET /QuanTriTaiKhoan -> 302 Location: /Home/KhongCoQuyen
  [khachtest02] GET /BanHang/POS -> 302 Location: /Home/KhongCoQuyen
  ```

---

## CHI TIẾT PHIÊN C1.1: CHẨN ĐOÁN (A1–A6), SỬA ĐỔI (B1–B7) VÀ KIỂM CHỨNG (D1–D7) (2026-11-08)

### 1. Kết quả chẩn đoán A1–A6 (Làm trước khi sửa code)

- **A1. Kiểm tra tiến trình giữ cổng `5000` và `5129`:**
  - Chạy `netstat -ano | findstr :5000` và `netstat -ano | findstr :5129`: phát hiện cả hai cổng `127.0.0.1:5000`/`[::1]:5000` và `127.0.0.1:5129`/`[::1]:5129` đang bị giữ bởi **PID `29780`** (`ERP_HRM_CMS.exe` chạy từ `C:\HRM - CMS - Bán hàng & Kho\bin\Debug\net8.0\ERP_HRM_CMS.exe`, khởi tạo bởi **PID `4308`** `dotnet.exe run --project ERP_HRM_CMS.csproj` từ lúc `11/7/2026 3:39:11 PM` — tức bản build `.NET 8` cũ trước Phiên B và Phiên C1).
  - Đã dừng cả hai tiến trình cũ (`Stop-Process -Id 29780, 4308 -Force`) và xác nhận lại `netstat -ano` trên cổng `5000` và `5129` đã hoàn toàn giải phóng (0 tiến trình lắng nghe).
- **A2. Đối chiếu `Properties/launchSettings.json` và tài liệu:**
  - Trong `Properties/launchSettings.json`, profile `"http"` có `"applicationUrl": "http://localhost:5000;http://localhost:5129"` (bind đồng thời cả cổng `5000` và `5129`), còn profile `"https"` có `"applicationUrl": "https://localhost:7184;http://localhost:5129"`.
  - Khi chạy `dotnet run --launch-profile http`, Kestrel bind cả `http://localhost:5000` và `http://localhost:5129`. Ở Phiên B, báo cáo smoke test chỉ truy vấn đầu cuối `http://localhost:5129`, còn `README.md` (dòng 79) chỉ ghi *"chọn profile `http` (cổng 5129)"*. Đã thống nhất cập nhật `README.md` và `docs/PROGRESS.md` ghi rõ profile `http` lắng nghe trên `http://localhost:5000` và `http://localhost:5129`.
- **A3. Kiểm tra `Data/DbInitializer.cs` quanh `SeedDemoDataAsync` và `AssignDemoRolesAsync`:**
  - **Thứ tự khởi động trong `Program.cs`:** `DbInitializer.SeedAsync` được gọi tại dòng `174` **trước** `app.Run()` (dòng `222`). Do đó khi khởi động ứng dụng mới trong lúc cổng `5000`/`5129` đang bị tiến trình cũ chiếm, `DbInitializer.SeedAsync` vẫn chạy xong và in log `"Đã khởi tạo cơ sở dữ liệu. Dữ liệu mẫu: True"`, rồi đến `app.Run()` mới văng lỗi `address already in use` khi Kestrel bind cổng.
  - **Seed có cập nhật `PasswordHash` của tài khoản đã tồn tại không?** Không. Cả `SeedAdminAccountsAsync` (kiểm tra `!await context.Users.AnyAsync(u => u.Username == ...)` trước khi gọi `CreateSuperAdminAccountAsync`) và `AssignDemoRolesAsync` (kiểm tra `if (user == null)` mới tạo user, còn nếu đã tồn tại thì chỉ kiểm tra và thêm `UserRole` còn thiếu) **đều không ghi đè `PasswordHash`** của `admin`, `quantritong` hay bất kỳ tài khoản nào đã tồn tại. Ở bước B2, đã đồng bộ thêm việc băm mật khẩu qua `LegacyCompatiblePasswordHasher` khi tạo tài khoản mới lần đầu và bổ sung test kiểm chứng `SeedDemo_DoesNotOverwrite_ExistingUser_PasswordHash`.
- **A4. Kiểm tra luồng đăng nhập trong `Controllers/AuthController.cs`:**
  - Khi `_userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password)` trả về `PasswordVerificationResult.SuccessRehashNeeded`, biến `rehashNeeded = true`. Nếu tài khoản đang `IsActive == true`, nhánh `if (rehashNeeded)` gán `user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, model.Password)` và gọi `await _context.SaveChangesAsync()` **nên hash mới CÓ được lưu xuống DB**.
  - Lý do trên `ERP_HRM_CMS_DB` ở cuối Phiên C1 `admin` và `quantritong` vẫn có hash 44 ký tự là vì: (1) ở Phiên C1 chỉ chạy kiểm thử đăng nhập trên bản sao `ERP_HRM_CMS_DB_COPY` theo yêu cầu mục 10 của C1, và (2) khi chạy ứng dụng trên `ERP_HRM_CMS_DB`, Kestrel bị chặn bởi tiến trình `.NET 8` cũ (`PID 29780` giữ cổng `5000`/`5129`) nên request đăng nhập vào cổng `5000`/`5129` đi vào tiến trình `.NET 8` cũ.
  - **Đăng nhập sai có làm đổi hash không?** Trước khi sửa ở C1.1, `AuthController.Login` có một nhánh dev fallback (`if (!passwordOk && (model.Password == "Admin@123456" || model.Password == "123456" || model.Password == user.Username))`) tự động gán lại `user.PasswordHash` nếu người dùng nhập một trong 3 chuỗi đó. Đã **gỡ bỏ hoàn toàn nhánh dev fallback này** ở bước B1 để đảm bảo đăng nhập sai tuyệt đối không bao giờ làm đổi `PasswordHash`.
- **A5. Nguyên nhân cảnh báo `The property 'User.UserName' was first mapped explicitly and then ignored` trong `AppDbContext`:**
  - `AppDbContext` kế thừa `IdentityUserContext<User, int, ...>`. Trong `base.OnModelCreating(modelBuilder)`, `IdentityUserContext` gọi `b.Property(u => u.UserName).HasMaxLength(256)` (ánh xạ tường minh `User.UserName`), sau đó `AppDbContext` lại gọi `modelBuilder.Entity<User>().Ignore(u => u.UserName);`, khiến EF Core phát cảnh báo `CoreEventId.MappedPropertyIgnoredWarning`.
  - **Cách sửa ở B3 (không đổi schema DB):** Đánh dấu `[NotMapped]` cho `public override string? UserName` trong `src/ERP.Domain/Entities/AuthEntities.cs` và cấu hình trực tiếp 4 thực thể Identity (`User` → `AspNetUsers`, `IdentityUserClaim<int>` → `AspNetUserClaims`, `IdentityUserLogin<int>` → `AspNetUserLogins`, `IdentityUserToken<int>` → `AspNetUserTokens`, kèm `Ignore<IdentityUserPasskey<int>>()` và `Ignore<IdentityPasskeyData>()`) trong `AppDbContext.OnModelCreating` mà không gọi `b.Property(u => u.UserName)`. Nhờ vậy: (1) Không còn cảnh báo `User.UserName` khi khởi động, (2) Không thay đổi schema hay tên cột `Username` trong bảng `AspNetUsers`, (3) `UserManager.FindByNameAsync` (truy vấn qua `NormalizedUserName`) và truy vấn SQL `SELECT UserName ... FROM dbo.AspNetUsers` đều hoạt động chính xác 100%.
- **A6. Kiểm tra các Authentication Scheme trong `Program.cs`:**
  - `Program.cs` dùng `builder.Services.AddIdentityCore<User>(...)` (không gọi `AddIdentity` nên không tự đăng ký `Identity.Application` cookie) và gọi `builder.Services.AddAuthentication(...).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => { options.Cookie.Name = "ERP_HRM_CMS_AuthCookie"; ... })`. Đã gỡ bỏ hoàn toàn `SignInManager<User>` khỏi `Program.cs` và `AuthController.cs` để chỉ duy trì duy nhất scheme `CookieAuthenticationDefaults.AuthenticationScheme` (`ERP_HRM_CMS_AuthCookie`).

### 2. Danh sách file đã sửa đổi và số lượng kiểm thử trước / sau (B1–B7 & C1–C3)

- **Danh sách file đã sửa đổi (8 file):**
  1. `Controllers/AuthController.cs` — Gỡ nhánh dev fallback mật khẩu trong `Login`; gỡ `SignInManager<User>`, thực hiện nhất quán Phương án A (đăng nhập tự quản qua `LoginThrottleService` + `_userManager.PasswordHasher` + `CookieAuthenticationDefaults.AuthenticationScheme`), đảm bảo lưu `PasswordHash` mới xuống DB khi `SuccessRehashNeeded`.
  2. `Data/DbInitializer.cs` — Đảm bảo `SeedAdminAccountsAsync` và `AssignDemoRolesAsync` không bao giờ ghi đè `PasswordHash` của tài khoản đã tồn tại; khi tạo tài khoản mới lần đầu sử dụng `LegacyCompatiblePasswordHasher`.
  3. `Program.cs` — Bỏ `.AddSignInManager<SignInManager<User>>()`, giữ duy nhất `ERP_HRM_CMS_AuthCookie`.
  4. `src/ERP.Domain/Entities/AuthEntities.cs` — Gắn `[NotMapped]` cho `public override string? UserName` (đồng bộ với `Username` và `NormalizedUserName`).
  5. `src/ERP.Infrastructure/Persistence/AppDbContext.cs` — Cấu hình trực tiếp thực thể Identity trong `OnModelCreating` không qua `b.Property(u => u.UserName)`, khử triệt để cảnh báo `The property 'User.UserName' was first mapped explicitly and then ignored`.
  6. `tests/ERP_HRM_CMS.Tests/AuthTests.cs` — Thêm **2 test mới** (`SeedDemo_DoesNotOverwrite_ExistingUser_PasswordHash`, `UserManager_FindByNameAsync_Resolves_Mapped_User_Without_Warning`) và cập nhật test rehash đã có từ C1 (`Login_WithLegacyHash_UpgradesToIdentityV3HashInDatabase_While_WrongPassword_DoesNotChangeHash`) để kiểm chứng thêm nhánh đăng nhập sai không đổi hash.
  7. `README.md`, `docs/adr/ADR-004-identity-va-vai-tro.md`, `docs/ANALYSIS.md`, `docs/PROGRESS.md` — Cập nhật tài liệu theo mục A2, C1–C3 và ghi nhận các rủi ro bảo mật đã chấp nhận tại **Mục 13 (“Rủi ro bảo mật đã chấp nhận”: `RISK-AUTH-01`, `RISK-AUTH-02`) của `docs/ANALYSIS.md`**.
- **Số lượng test trước và sau Phiên C1.1:**
  - Trước C1.1 (cuối C1): **261 Passed, 0 Failed, 0 Skipped**.
  - Sau C1.1: **263 Passed, 0 Failed, 0 Skipped** (**+2 test mới** trong `AuthTests.cs`: `SeedDemo_DoesNotOverwrite_ExistingUser_PasswordHash` và `UserManager_FindByNameAsync_Resolves_Mapped_User_Without_Warning`).
- **Rủi ro bảo mật đã chấp nhận:** Xem chi tiết tại **Mục 13 (“Rủi ro bảo mật đã chấp nhận” — `RISK-AUTH-01` và `RISK-AUTH-02`) trong `docs/ANALYSIS.md`**.

### 3. Kết quả kiểm chứng D1–D7 trên cơ sở dữ liệu thật `ERP_HRM_CMS_DB`

- **D1. Sao lưu và kiểm tra checksum trước khi thao tác trên `ERP_HRM_CMS_DB`:**
  - Lệnh: `BACKUP DATABASE [ERP_HRM_CMS_DB] TO DISK = N'C:\SQLBackup\ERP_HRM_CMS_DB_before_C1_1.bak' WITH INIT, CHECKSUM, STATS = 10; RESTORE VERIFYONLY FROM DISK = N'C:\SQLBackup\ERP_HRM_CMS_DB_before_C1_1.bak' WITH CHECKSUM;`
  - Kết quả: `BACKUP DATABASE successfully processed 1802 pages in 0.179 seconds (78.626 MB/sec). The backup set on file 1 is valid.`
- **D2. Build không incremental (`dotnet build ERP_HRM_CMS.sln --no-incremental`):**
  - Kết quả: `Build succeeded. 0 Warning(s), 0 Error(s)`.
- **D3. Chạy toàn bộ bộ kiểm thử (`dotnet test tests/ERP_HRM_CMS.Tests`):**
  - Kết quả: `Passed! - Failed: 0, Passed: 263, Skipped: 0, Total: 263, Duration: 45 s - ERP_HRM_CMS.Tests.dll (net10.0)`.
- **D4. Chạy ứng dụng ở profile `http` (`http://localhost:5000;http://localhost:5129`), đăng nhập `admin` và `quantritong` bằng `Admin@123456`:**
  - Trước khi đăng nhập:
    ```text
    UserName    Prefix   Len
    ----------- -------- ---
    admin       mBaziiyX 44
    quantritong dhEb9P6L 44
    ```
  - Đăng nhập `admin` (`http://localhost:5000/Auth/Login`) → `302 Found` (`Location: /PhanQuyen/MaTranPhanQuyen`).
  - Đăng nhập `quantritong` (`http://localhost:5129/Auth/Login`) → `302 Found` (`Location: /PhanQuyen/MaTranPhanQuyen`).
  - Kết quả truy vấn sau khi đăng nhập trên `ERP_HRM_CMS_DB`:
    ```text
    UserName    Prefix   Len
    ----------- -------- ---
    admin       AQAAAAIA 84
    quantritong AQAAAAIA 84
    ```
- **D5. Dừng ứng dụng, khởi động lại (`Seed:Demo = True`), kiểm tra bảo toàn hash và log `User.UserName`:**
  - Kết quả truy vấn sau khi khởi động lại:
    ```text
    UserName    Prefix   Len
    ----------- -------- ---
    admin       AQAAAAIA 84
    quantritong AQAAAAIA 84
    ```
  - Kiểm tra log khởi động: **0 cảnh báo** `The property 'User.UserName' was first mapped explicitly and then ignored`.
- **D6. Đăng ký tài khoản thử `khachtest02` qua `POST /Auth/Register`:**
  - Phản hồi HTTP: `302 Found` → `/Auth/Login`.
  - Kết quả truy vấn trên `ERP_HRM_CMS_DB`:
    ```text
    UserName    IsActive Code
    ----------- -------- --------
    khachtest02 1        CUSTOMER
    ```
- **D7. Kiểm tra toàn vẹn dữ liệu trên `ERP_HRM_CMS_DB`:**
  ```text
  UsersCount AspNetUsersCount FkToUsersCount
  ---------- ---------------- --------------
  12         13               0
  ```
  - Bảng `Users` giữ nguyên **12 dòng** (không tăng).
  - Bảng `AspNetUsers` tăng từ 12 lên **13 dòng** (tăng đúng 1 dòng của `khachtest02`).
  - Số khóa ngoại trỏ vào `dbo.Users`: **0**.

---

## TỔNG QUAN KẾT QUẢ ĐẠT ĐƯỢC (CẬP NHẬT TẠI PHIÊN C1.1)

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
| **Phiên C1.1** | **Đóng C1: Lưu Hash Identity v3, Bảo Toàn Hash Khi Seed Demo, Ánh Xạ `UserName`, Luồng Đăng Nhập & Kiểm Chứng `ERP_HRM_CMS_DB`** | **Hoàn tất** | **263 / 263 ca thực thi XANH 100% (0 Warning, 0 Error)** |
| **Phiên C2** | **Tách Khu Vực `Admin`, `Staff`, `Shop` (Giữ Nguyên Route) & Đóng `RISK-AUTH-02`** | **Hoàn tất** | **270 / 270 ca thực thi XANH 100% (0 Warning, 0 Error)** |

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
