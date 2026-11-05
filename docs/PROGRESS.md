# Tiến độ dự án

Cập nhật lần cuối: 2026-11-05 (hoàn tất PHIÊN 4: Hồ Sơ Nhân Sự 360°, Sơ Đồ Tổ Chức Tương Tác, Quyết Định Nhân Sự & Tài Sản Cấp Phát — 205/205 test xanh 100%)

## Phiên 1: phân tích hiện trạng (đã xong)

**Đã làm**
- Đọc cấu trúc repo, `README.md`, `ERP_HRM_CMS.csproj`, `Program.cs`, `AppDbContext`, `DbInitializer`, cấu hình, migration, view, JS, test.
- Lập bảng hiện trạng module, phát hiện bảo mật, toàn vẹn dữ liệu, hiệu năng, code smell.
- Đối chiếu với chuẩn doanh nghiệp và các quy tắc của dự án.
- Xếp hạng P0/P1/P2 kèm ước lượng và lộ trình các phiên sau.

**Kết quả:** `docs/ANALYSIS.md`

**Chưa làm (theo yêu cầu, không sửa code)**
- Chưa build, chưa chạy test (môi trường không có .NET SDK và SQL Server).
- Chưa sửa bất kỳ file mã nguồn nào.

**Trạng thái:** chờ xác nhận của người dùng trước khi sang phiên 2.

## Cách chạy (theo README)

```
dotnet run --project ERP_HRM_CMS.csproj --launch-profile http
dotnet test tests/ERP_HRM_CMS.Tests
```

Yêu cầu: .NET 8 SDK, SQL Server ở `localhost` với Windows Authentication.

## Phiên 2, bước 2a: tài liệu kiến trúc (đã xong, chưa commit)

**Đã làm**
- Viết `docs/ARCHITECTURE.md`: kiến trúc mục tiêu 4 tầng, quy tắc phụ thuộc, trách nhiệm từng project, cấu hình dùng chung, quy ước đặt tên.
- Viết `docs/adr/ADR-004-identity-va-vai-tro.md`: chuyển sang ASP.NET Core Identity, giữ mật khẩu cũ bằng `PasswordHasher` tùy chỉnh, ánh xạ vai trò (giữ bảng cũ, thêm `ACCOUNTANT`).

**Chưa làm**
- Chưa đổi mã nguồn. Chưa build, chưa chạy test (môi trường không có .NET SDK, không truy cập được NuGet).
- Quyết định giữ .NET 8 đã chốt (ADR-004 mục 2.1).

**Trạng thái git:** hai file mới chưa commit. Commit `dabd99b` (tài liệu phiên 1) chưa push, vì kết nối GitHub chưa hoạt động.

## Việc tiếp theo (phiên 2, chờ xác nhận)

1. Chạy `dotnet build` và `dotnet test` để có mốc cơ sở.
2. Sửa các P0 nhóm bảo mật khẩn cấp: P0-02, P0-03, P0-04, P0-07, P0-13.
4. Các quyết định cần người dùng xác nhận: xem mục 14 của `docs/ANALYSIS.md`.

## Phiên 3: Theme Admin, Layout và Hiệu ứng (đã xong)

**Đã làm**
- Đổi font sang **Be Vietnam Pro** (hỗ trợ tiếng Việt) trong `enterprise-theme.css`.
- Viết lại `Views/Shared/_Layout.cshtml`: sidebar thu gọn + topbar + breadcrumb + dark mode.
- Tạo `wwwroot/css/admin-components.css` (~500 dòng): sidebar, topbar, stat card, data table, floating label form, button loading, confirm modal, toast, badges, tabs, skeleton, empty state, responsive.
- Tạo `wwwroot/css/admin-animations.css` (~260 dòng): fade-in, slide-up, stagger, hover-lift, ripple, scroll reveal, chart animation, toast animation, pulse, `prefers-reduced-motion` support.
- Tạo `wwwroot/js/admin-ui.js` (~310 dòng): sidebar toggle (localStorage), active highlight, ripple effect, count-up animation (IntersectionObserver), scroll reveal, toast auto-dismiss, `ERP.btnLoading()`, `ERP.confirm()`, `ERP.toast()`.
- Cập nhật `Views/Home/Index.cshtml` (Dashboard): 6 stat cards với count-up, stagger animation, chart container, data table, activity feed, empty state, tất cả dùng component class mới.
- Viết `docs/UI-GUIDE.md`: bảng màu, font, layout, component, animation, JS API, file structure, responsive, quy ước đặt tên, accessibility.

**File đã thay đổi**
- `wwwroot/css/enterprise-theme.css` — đổi font
- `Views/Shared/_Layout.cshtml` — viết lại hoàn toàn
- `wwwroot/css/admin-components.css` — MỚI
- `wwwroot/css/admin-animations.css` — MỚI
- `wwwroot/js/admin-ui.js` — MỚI
- `Views/Home/Index.cshtml` — cập nhật dùng component class
- `docs/UI-GUIDE.md` — MỚI
- `docs/PROGRESS.md` — cập nhật

**Cách kiểm tra**
1. Mở dự án trong VS 2022, chạy (`F5` hoặc `dotnet run`).
2. Đăng nhập → Dashboard: sidebar bên trái, topbar trên cùng, 6 stat card có count-up.
3. Click nút thu gọn sidebar (góc dưới trái) → sidebar thu lại 68px, tooltip hiện tên menu.
4. Click nút mặt trăng (topbar) → dark mode, refresh không mất.
5. Resize trình duyệt < 992px → sidebar ẩn, nút hamburger xuất hiện.
6. Kiểm tra `docs/UI-GUIDE.md` để xem quy ước class.

**Trạng thái git:** tất cả thay đổi phiên 3 chưa commit. Cần áp patch trên máy local.

## Phiên 4: Thiết kế và Triển khai Cơ sở dữ liệu (đã xong)

**Đã làm**

1. **Thiết kế ERD** — 53 bảng chia 4 nhóm:
   - System (12): Users, Roles, Permissions, UserRoles, RolePermissions, UserPermissions, AuditLogs, Notifications, NotificationReads, LoginAttempts, SiteLayouts, SiteSettings
   - HRM (18): Companies, Branches, Departments, Positions, Employees, Contracts, LeaveRequests, **LeaveTypes**, PayrollPeriods, SalarySlips, **JobPostings**, **Candidates**, **Trainings**, **TrainingParticipants**, **KpiReviews**, **KpiCriteria**, PayrollSettings, SeedHistory
   - CMS (7): Contents, Categories, Menus, Banners, **Tags**, **ContentTags**, **Media**, **SeoMeta**
   - SCM (16): Products, Inventories, InventoryTransactions, Orders, OrderDetails, CartItems, Suppliers, PurchaseOrders, PurchaseOrderDetails, DebtRecords, DebtPayments, StocktakingSheets, StocktakingDetails, **Customers**, **Units**, **Warehouses**, **Invoices**, **Payments**
   - (**in đậm** = bảng mới phiên 4, tổng 13 bảng mới)

2. **Entity classes** — tạo 4 file entity mới:
   - `Models/Entities/AuditableEntity.cs` — interface `IAuditable`
   - `Models/Entities/HRMRecruitmentEntities.cs` — LeaveType, JobPosting, Candidate, Training, TrainingParticipant, KpiReview, KpiCriteria
   - `Models/Entities/CMSExtendedEntities.cs` — Tag, ContentTag, MediaFile, SeoMeta
   - `Models/Entities/SCMExtendedEntities.cs` — Customer, Unit, Warehouse, Invoice, Payment

3. **Fluent API** — cập nhật `Data/AppDbContext.cs`:
   - 16 DbSet mới
   - Unique index trên Code/Name/Slug
   - Composite PK cho TrainingParticipant, ContentTag
   - FK relationships với DeleteBehavior phù hợp
   - Global query filter cho soft delete (IAuditable)

4. **SQL Migration Script** — `db/scripts/V4_01_NewTables_And_AuditColumns.sql`:
   - Thêm cột audit (IsDeleted, UpdatedAt, UpdatedBy, CreatedBy) cho 14 bảng hiện có
   - Tạo 13 bảng mới với PK, FK, indexes
   - Thêm FK mới: Products.UnitId, Orders.CustomerId, DebtRecords.CustomerId, LeaveRequests.LeaveTypeId
   - Idempotent (kiểm tra tồn tại trước khi tạo)

5. **Seed Data** — `db/scripts/V4_02_SeedData.sql`:
   - 1 Company, 3 Branches, 8 Roles, 10 Departments, 10 Positions
   - 50 Employees (phân bố 3 chi nhánh)
   - 10 Units, 7 LeaveTypes, 10 Categories, 100 Products
   - 4 Warehouses, 20 Customers, 8 Tags
   - Idempotent (kiểm tra tồn tại trước khi INSERT)

6. **Tài liệu**:
   - `docs/ERD.md` — 4 Mermaid ER diagram
   - `db/README.md` — hướng dẫn tạo DB, migration, seed, backup

**File đã thay đổi / tạo mới**
- `Models/Entities/AuditableEntity.cs` — MỚI
- `Models/Entities/HRMRecruitmentEntities.cs` — MỚI
- `Models/Entities/CMSExtendedEntities.cs` — MỚI
- `Models/Entities/SCMExtendedEntities.cs` — MỚI
- `Models/Entities/HRMAdvancedEntities.cs` — thêm LeaveTypeId FK
- `Models/Entities/InventoryEntities.cs` — thêm UnitId, CustomerId FK
- `Models/Entities/SCMEntities.cs` — thêm CustomerId FK
- `Models/Entities/CMSEntities.cs` — thêm ContentTags collection
- `Data/AppDbContext.cs` — 16 DbSet mới, Fluent API, soft delete filter
- `db/scripts/V4_01_NewTables_And_AuditColumns.sql` — MỚI
- `db/scripts/V4_02_SeedData.sql` — MỚI
- `db/README.md` — MỚI
- `docs/ERD.md` — MỚI
- `docs/PROGRESS.md` — cập nhật

**Cách kiểm tra**
1. Áp patch lên máy local.
2. Mở SSMS, chạy `V4_01_NewTables_And_AuditColumns.sql` trên database hiện có.
3. Chạy `V4_02_SeedData.sql` để có dữ liệu mẫu.
4. Mở VS 2022, build project (`Ctrl+Shift+B`) để xác nhận entity không lỗi.
5. Chạy app (`F5`) → kiểm tra Dashboard có dữ liệu từ seed.

## Phiên 5: Module Nhân Sự — Hồ Sơ & Tổ Chức (đang tiến hành)

**Mục tiêu**: Xây dựng module HR hoàn chỉnh gồm: quản lý hồ sơ nhân viên nâng cao, cơ cấu tổ chức dạng cây, hợp đồng lao động, lịch sử thay đổi, dashboard HR, phân quyền, unit test.

### Đã hoàn thành

1. **5 Entity mới** — `Models/Entities/HRMProfileEntities.cs`:
   - `EmployeeRelative` — người thân / phụ thuộc
   - `EmployeeSkill` — kỹ năng (Beginner → Expert)
   - `EmployeeCertificate` — chứng chỉ / bằng cấp (có hạn)
   - `EmployeeDocument` — tài liệu đính kèm (upload file)
   - `EmployeeChangeHistory` — lịch sử thay đổi (Department, Position, Salary, Status)

2. **Cập nhật Entity gốc** — `Models/Entities/HRMEntities.cs`:
   - `Department` thêm `Children` navigation (cây đệ quy)
   - `Employee` thêm 5 navigation collections mới

3. **Cập nhật DbContext** — `Data/AppDbContext.cs`:
   - 5 DbSet mới
   - Fluent API: self-referencing Department, cascade/restrict delete, composite index

4. **Service Layer**:
   - `IEmployeeService` + `EmployeeService` — CRUD, phân trang, hồ sơ mở rộng, dashboard, avatar upload, change tracking
   - `IDepartmentService` + `DepartmentService` — cây đệ quy, flat list, toggle active, move
   - `PaginatedList<T>` — phân trang server-side dùng chung

5. **Controllers** (refactored với Service Injection):
   - `NhanSuController` — inject IEmployeeService, IDepartmentService; phân trang, lọc, sắp xếp; AJAX upload avatar; CRUD người thân, kỹ năng, chứng chỉ, tài liệu; Dashboard HR
   - `PhongBanController` — inject IDepartmentService; cây đệ quy; toggle active; thêm/sửa/xóa phòng ban

6. **Razor Views**:
   - `Views/NhanSu/Index.cshtml` — danh sách nhân viên có phân trang, lọc, sắp xếp, avatar
   - `Views/NhanSu/Details.cshtml` — trang chi tiết với 4 tabs: Thông tin, Hợp đồng, Lịch sử, Tài liệu; upload avatar AJAX; quản lý người thân, kỹ năng, chứng chỉ, tài liệu
   - `Views/NhanSu/Create.cshtml` — giữ nguyên (đã tương thích)
   - `Views/NhanSu/Edit.cshtml` — MỚI, form chỉnh sửa đầy đủ, cấp tài khoản
   - `Views/NhanSu/Dashboard.cshtml` — MỚI, thống kê tổng quan + Chart.js (phòng ban, giới tính, trạng thái), NV mới, HĐ sắp hết hạn
   - `Views/PhongBan/Index.cshtml` — cây phòng ban đệ quy, toggle active, chọn parent

7. **SQL Migration** — `db/scripts/V5_01_ProfileTables.sql`:
   - 5 bảng mới: EmployeeRelatives, EmployeeSkills, EmployeeCertificates, EmployeeDocuments, EmployeeChangeHistories
   - Thêm cột ParentId vào Departments (nếu chưa có)
   - FK, indexes, idempotent

8. **Đăng ký DI** — `Program.cs`:
   - `IEmployeeService` → `EmployeeService`
   - `IDepartmentService` → `DepartmentService`

### Chưa hoàn thành

- [ ] Excel import/export nhân viên (template + báo lỗi từng dòng)
- [ ] PDF export hồ sơ nhân viên
- [ ] Phân quyền chi tiết (HR can edit; Employee can only view/edit own info)
- [ ] Unit test cho EmployeeService
- [ ] Integration test cho flow tạo nhân viên

**File đã thay đổi / tạo mới (Phiên 5)**
- `Models/Entities/HRMProfileEntities.cs` — MỚI
- `Models/Entities/HRMEntities.cs` — thêm navigation properties
- `Data/AppDbContext.cs` — thêm 5 DbSet + Fluent API
- `Models/ViewModels/PaginatedList.cs` — MỚI
- `Services/HRM/IEmployeeService.cs` — MỚI
- `Services/HRM/EmployeeService.cs` — MỚI
- `Services/HRM/IDepartmentService.cs` — MỚI
- `Services/HRM/DepartmentService.cs` — MỚI
- `Controllers/NhanSuController.cs` — refactored
- `Controllers/PhongBanController.cs` — refactored
- `Views/NhanSu/Index.cshtml` — cập nhật (PaginatedList)
- `Views/NhanSu/Details.cshtml` — viết lại (tabs, profile mở rộng)
- `Views/NhanSu/Edit.cshtml` — MỚI
- `Views/NhanSu/Dashboard.cshtml` — MỚI
- `Views/PhongBan/Index.cshtml` — cập nhật (cây đệ quy)
- `db/scripts/V5_01_ProfileTables.sql` — MỚI
- `Program.cs` — thêm DI registration
- `docs/PROGRESS.md` — cập nhật

**Cách kiểm tra**
1. Áp patch lên máy local.
2. Chạy `db/scripts/V5_01_ProfileTables.sql` trên SSMS.
3. Build project (`Ctrl+Shift+B`).
4. Chạy app → vào `/NhanSu` kiểm tra danh sách có phân trang.
5. Click vào nhân viên → kiểm tra tabs Details (Thông tin, Hợp đồng, Lịch sử, Tài liệu).
6. Vào `/NhanSu/Dashboard` → kiểm tra biểu đồ Chart.js.
7. Vào `/PhongBan` → kiểm tra cây phòng ban đệ quy.

## Phiên 1 (Thực thi Nền móng): Clean Architecture 4 Tầng, Phân Quyền Phạm Vi Dữ Liệu & Vá Bảo Mật P0 (đã hoàn tất 100%)

### 1. Kiến trúc Clean Architecture 4 Tầng (Chiến lược Strangler Fig — Không làm gãy 76 Razor Views hiện có)
- **Tổ chức lại `ERP_HRM_CMS.sln`** thành 4 tầng phụ thuộc một chiều nghiêm ngặt + dự án kiểm thử:
  - `src/ERP.Domain`: Chứa toàn bộ 16 cụm thực thể nghiệp vụ, `WorkflowStatuses`, `DataScopeLevel`, `DomainException`, `TonKhoKhongDuException`, `VietnamTime`, `SensitiveDataMasker` (giữ tương thích ngược namespace `ERP_HRM_CMS.Models.Entities` để 100% Razor Views và query hiện có hoạt động ổn định). Bổ sung `User.MustChangePassword` (`MustChangePasswordOnFirstLogin`) và `LeaveRequest.CreatedByUserId`.
  - `src/ERP.Application` (`AddApplication()`):
    - `Result` và `Result<T>` chuẩn hóa kết quả nghiệp vụ thành công/thất bại kèm mã lỗi.
    - `PagedResult<T>` dùng chung cho phân trang phía Server.
    - `ICurrentUserService` + `ErpClaimTypes`: Đọc chính xác `UserId`, `EmployeeId`, `BranchId`, `DepartmentId`, `Roles`, `IsSuperAdmin`, `MustChangePassword` từ Claims xác thực; **xóa bỏ triệt để 100% mẫu `Session.GetInt32("UserId") ?? 1`** trên toàn bộ các Controller & `AppDbContext`.
    - `IDataScopeService` + `DataScopeService`: Áp đặt phân quyền phạm vi dữ liệu tự động theo 4 cấp `Company / Branch / Department / Self` (`SuperAdmin` & `HR_MANAGER` thấy toàn công ty; `BRANCH_MANAGER` thấy chi nhánh mình; `DEPARTMENT_HEAD` thấy phòng ban mình; `EMPLOYEE` chỉ thấy/thao tác trên hồ sơ, đơn từ, bảng lương cá nhân).
    - Chính sách miền cốt lõi: `LeaveApprovalPolicy` (chống tự duyệt đơn nghỉ phép) và `PosDiscountPolicy` (giới hạn trần giảm giá POS theo vai trò).
  - `src/ERP.Infrastructure` (`AddInfrastructure()`, `UseInfrastructure()`, `UseMustChangePasswordEnforcement()`, `MapInfrastructureEndpoints()`):
    - **Serilog**: Ghi log có cấu trúc ra Console và Rolling File theo ngày (`logs/erp-.log`) kèm `CorrelationId` (`CorrelationIdMiddleware` phản hồi header `X-Correlation-ID`) và `UserId` thực tế từ Claims.
    - **Health Checks**: Endpoint `/health` (`SqlServerDbHealthCheck`) kiểm tra trực tiếp kết nối SQL Server, trả JSON trạng thái chuẩn.
    - **Centralized `IMemoryCache`**: `IAppCacheService` / `MemoryAppCacheService` quản lý tập trung cache Quyền (`PermissionService` + cầu nối `PhanQuyenHelper`), `SiteSettings` (`SiteSettingReader`), `DynamicMenu` và `DynamicBanner`, tự động xóa cache theo tiền tố ngay trong `AppDbContext.SaveChanges` / `SaveChangesAsync` khi cập nhật quyền/menu/cấu hình.
    - **Hiệu năng & Reverse Proxy**: Tích hợp `ResponseCompression` (Brotli & Gzip) và `ForwardedHeaders` an toàn (chỉ tin cậy Loopback hoặc danh sách `KnownProxies` cấu hình tường minh).
  - `ERP_HRM_CMS` (Tầng trình diễn Web MVC): Tinh gọn `Program.cs`, ánh xạ qua `AddApplication()` / `AddInfrastructure()`, cập nhật `DbInitializer` bổ sung vai trò `BRANCH_MANAGER` và `DEPARTMENT_HEAD`.

### 2. Khắc phục triệt để các điểm nóng Bảo mật P0 (`docs/ANALYSIS.md`)
- **`HomeController.Index` (Dashboard)**:
  - Kiểm tra quyền thực tế (`order.view`, `hrm.employee.view`, `hrm.leave.approve`, `inventory.view`, `scm.debt.view`, `system.audit.view`) **trước khi** truy vấn SQL và hiển thị thẻ KPI.
  - Tài khoản vai trò `EMPLOYEE` được điều hướng hiển thị **Không gian làm việc cá nhân (Self-Service)** với ngày công, đơn chờ duyệt và phép năm của chính mình — tuyệt đối không lộ doanh thu, đơn hàng, công nợ hay biến động nhân sự toàn công ty.
  - Tối ưu các truy vấn tuần tự: gom 12 câu truy vấn doanh thu từng tháng thành 1 truy vấn `GROUP BY Year, Month` duy nhất và chuyển tính tổng công nợ sang `SumAsync` trực tiếp trên SQL Server.
- **`HomeController.GlobalQuickSearch`**:
  - Lọc chặt chẽ theo từng quyền (`hrm.employee.view`, `product.view`, `order.view`, `scm.po.view`, `hrm.leave.view` / `hrm.self.service`) kết hợp `IDataScopeService`.
  - Loại bỏ hoàn toàn việc tìm kiếm theo SĐT/Email và không trả bất kỳ trường PII nhạy cảm (`Phone`, `Email`, `CCCD`) nào trong JSON phản hồi.
- **`NhanSuController.CapTaiKhoanNhanh` & `AuthController.ChangePassword`**:
  - Chấm dứt hoàn toàn việc lưu/hiển thị mật khẩu dạng rõ trong `TempData` hoặc log; sinh mật khẩu tạm ngẫu nhiên mật mã học 14 ký tự (`RandomNumberGenerator`), trả về 1 lần duy nhất (`Cache-Control: no-store`) cho Modal sao chép tại `Views/NhanSu/Details.cshtml`.
  - Gắn cờ `MustChangePassword = true` (`MustChangePasswordOnFirstLogin = true`), cưỡng chế bởi `MustChangePasswordMiddleware` chuyển hướng mọi request về màn hình **Đổi mật khẩu cá nhân** (`GET/POST /Auth/ChangePassword`, `Views/Auth/ChangePassword.cshtml`), đồng thời xoay vòng `SecurityStamp` thu hồi phiên cũ.
- **`DonTuController`, `BanHangController` & các Endpoint nội bộ**:
  - `DonTuController`: Chặn người tạo đơn / chủ đơn tự bấm duyệt hoặc từ chối đơn của chính mình (`LeaveApprovalPolicy`), ngoại trừ `SuperAdmin` được phép xử lý khẩn cấp nhưng bắt buộc ghi rõ hành động `SELF_APPROVE_BY_SUPERADMIN` / `SELF_REJECT_BY_SUPERADMIN` vào `AuditLogs`.
  - `BanHangController`: Áp đặt trần giảm giá hợp lệ phía Server (`PosDiscountPolicy`: tối đa 10% cho thu ngân `SALES_STAFF`, 30% cho quản lý `SALES_MANAGER`, bắt buộc nhập lý do kiểm toán khi giảm > 5%, chặn âm hoặc vượt tổng tiền hàng); gắn `[KiemTraQuyen("pos.use", "order.create", "product.view")]` bảo vệ `TimKiemSanPhamPos`.
  - Bổ sung kiểm tra quyền trên `SupplierController.GetSupplier`, `KhoController.LayThongTinTonKho` và chuyển cộng tồn kho trong `PurchaseOrderController.NhanHang` sang `InventoryStockService.CongTonAsync` nguyên tử.

### 3. Cơ sở dữ liệu, Kiểm chứng Build & Bộ Kiểm thử Tự động
- **EF Core Migration & Script SQL Idempotent**:
  - Tạo migration `src/ERP.Infrastructure/Migrations/20261104175015_V6_00_Security_And_Architecture.cs` (đồng thời xử lý triệt để ràng buộc `DeleteBehavior.Restrict` cho `Warehouses` tránh lỗi đa đường xóa lan truyền trên SQL Server).
  - Tạo script SQL idempotent `db/scripts/V6_00_Security_And_Architecture.sql` chạy trực tiếp nhiều lần an toàn trên SSMS (bổ sung `Users.MustChangePassword`, `LeaveRequests.CreatedByUserId`, FK, Index và vai trò `BRANCH_MANAGER`, `DEPARTMENT_HEAD`).
- **Kết quả Build & Test**:
  - `dotnet build ERP_HRM_CMS.sln`: **Thành công — 0 Error(s)**.
  - `dotnet test tests/ERP_HRM_CMS.Tests`: **183 / 183 bài kiểm thử chạy XANH 100%** (bao gồm toàn bộ 173 test hiện hữu + 10 test kiến trúc & bảo mật P0 mới trong `Phase1ArchitectureAndSecurityTests.cs`).

## Phiên 2 (Cơ Sở Dữ Liệu Chuyên Sâu): Hoàn Thiện Cơ Sở Dữ Liệu Enterprise HRM Trên SSMS & EF Core (đã hoàn tất 100%)

### 1. Phân tích & Bổ sung Entities khép kín vòng đời Nhân sự (Hire-to-Retire)
- Tạo mới file thực thể `src/ERP.Domain/Entities/HRMEnterpriseEntities.cs` kế thừa `IAuditable` và bổ sung các thực thể còn thiếu:
  - `EmployeeLeaveBalance` (Quỹ phép năm nhân sự): Quản lý theo `EmployeeId`, `Year`, `LeaveTypeId`, `StandardDays` (mặc định 12 ngày), `SeniorityBonusDays` (+1 ngày mỗi 5 năm thâm niên theo Bộ luật Lao động VN), `CarriedOverDays` (phép tồn năm trước), `UsedDays`, `RemainingDays` và `ExpiredCarryOverDate` (thời hạn dùng phép tồn, thường là 31/03 hoặc 30/06 năm sau).
  - `AttendanceLock` (Khóa kỳ công): Quản lý khóa sổ dữ liệu chấm công theo tháng/năm/chi nhánh/phòng ban (`Year`, `Month`, `BranchId`, `DepartmentId`, `IsLocked`, `LockedById`, `LockedAt`, `Reason`) trước khi chuyển tính lương.
  - `AttendanceAdjustmentRequest` (Đơn giải trình chấm công): Nhân viên giải trình đi trễ/về sớm, quên quẹt thẻ (`MissedCheckIn`, `MissedCheckOut`, `DeviceError`, `OffsiteWork`), kèm giờ gốc, giờ đề xuất điều chỉnh, tài liệu minh chứng (`AttachmentUrl`) và quy trình phê duyệt cấp quản lý.
  - `HrDecision` (Quyết định Nhân sự điện tử): Quản lý vòng đời nhân sự pháp lý với số quyết định chuẩn hóa `DecisionNumber` (ví dụ `QD-2026-001`), loại quyết định (`Appointment`, `Transfer`, `SalaryAdjustment`, `Reward`, `Discipline`, `Termination`), ngày hiệu lực, liên kết phòng ban/chức vụ/mức lương cũ & mới, số tiền thưởng/phạt (`Amount` tự động tích hợp kỳ tính lương) và chữ ký số/người ký duyệt.
  - `CompanyAsset` & `EmployeeAssetAssignment`: Quản lý danh mục tài sản cấp phát cho nhân sự (`Laptop`, `Monitor`, `AccessCard`, `Uniform`, `Vehicle`...), số serial, thông số kỹ thuật, ngày mua, nguyên giá, tình trạng giao/thu hồi và biên bản bàn giao (`HandoverDocumentUrl`) phục vụ chặt chẽ khi Onboarding và Offboarding.
  - `OnboardingChecklist` & `OnboardingTask`: Quy trình hội nhập nhân viên mới (`Onboarding`) và bàn giao thôi việc (`Offboarding`), phân rã chi tiết từng đầu việc theo vai trò chịu trách nhiệm (`HR`, `IT`, `DepartmentHead`, `Employee`), thời hạn hoàn thành và trạng thái.
  - `InterviewSchedule`: Quản lý lịch phỏng vấn ứng viên theo từng vòng (`RoundNumber`), hình thức (`Offline`/`Online`), địa điểm/link họp, người phỏng vấn (`InterviewerEmployeeId`, `InterviewerUserId`), điểm số đánh giá (0-100), nhận xét chi tiết và kết quả.
  - Bổ sung trường `Slug` (chuẩn SEO tuyển dụng tối đa 350 ký tự) trên bảng `JobPostings` và hỗ trợ quan hệ 1-N giữa `Candidate` và `InterviewSchedule`.
  - Cập nhật `src/ERP.Domain/Entities/HRMEntities.cs` bổ sung `Employee.IsDeleted`, `UpdatedAt`, `UpdatedBy`, `CreatedBy` và 5 navigation collections (`LeaveBalances`, `AttendanceAdjustmentRequests`, `HrDecisions`, `AssetAssignments`, `OnboardingChecklists`).

### 2. Cấu hình EF Core Fluent API Chuẩn Doanh Nghiệp (`src/ERP.Infrastructure/Persistence/AppDbContext.cs`)
- Đăng ký 9 `DbSet` mới: `EmployeeLeaveBalances`, `AttendanceLocks`, `AttendanceAdjustmentRequests`, `HrDecisions`, `CompanyAssets`, `EmployeeAssetAssignments`, `OnboardingChecklists`, `OnboardingTasks`, `InterviewSchedules`.
- Thiết lập toàn diện quan hệ khóa ngoại với `DeleteBehavior.Restrict` (NO ACTION) trên toàn bộ các bảng lịch sử nhân sự và tài sản để triệt tiêu lỗi đa đường xóa lan truyền (multiple cascade paths) trên SQL Server (chỉ áp dụng `Cascade` cho `OnboardingTasks` theo `Checklist` và `InterviewSchedules` theo `Candidate`).
- Cấu hình độ chính xác số tiền tệ `decimal(18,2)` và điểm số `decimal(5,2)` đồng bộ trên model EF Core.
- Mở rộng bộ chuyển đổi thời gian `VietnamTimeConverter` cho mọi trường `DateTime` thời điểm nghiệp vụ (bao gồm `ScheduledTime`).
- Áp dụng Global Query Filter tự động `e.IsDeleted == false` thông qua interface `IAuditable` cho toàn bộ 9 thực thể mới.

### 3. Bộ Script SQL Chuyên Nghiệp Cho SQL Server Management Studio (SSMS)
- **`db/scripts/V6_01_EnterpriseHRM_Schema_And_Indexes.sql`**:
  - Viết chuẩn T-SQL chạy 100% trên SSMS, hoàn toàn **Idempotent** (`IF OBJECT_ID(...) IS NULL`, `IF COL_LENGTH(...) IS NULL`, `IF NOT EXISTS`).
  - Thiết lập đầy đủ 13 **CHECK CONSTRAINTS** bảo vệ toàn vẹn nghiệp vụ:
    + `CK_LeaveRequests_DateRange` (`[ToDate] >= [FromDate]`) & `CK_LeaveRequests_TotalDays` (`[TotalDays] > 0`).
    + `CK_EmployeeLeaveBalances_Year` (`[Year] BETWEEN 2000 AND 2100`) & `CK_EmployeeLeaveBalances_Days` (tất cả các trường ngày phép >= 0).
    + `CK_AttendanceLocks_Month` (`[Month] BETWEEN 1 AND 12` và `[Year] BETWEEN 2000 AND 2100`).
    + `CK_AttendanceAdjustmentRequests_Times` (`[AdjustedCheckOut] >= [AdjustedCheckIn]`).
    + `CK_HrDecisions_Amount` (`[Amount] >= 0`).
    + `CK_CompanyAssets_Price` (`[PurchasePrice] >= 0`).
    + `CK_EmployeeAssetAssignments_Dates` (`[ReturnedDate] >= [AssignedDate]`).
    + `CK_OnboardingChecklists_Dates` (`[CompletedDate] >= [StartDate]`).
    + `CK_InterviewSchedules_Score`, `CK_Candidates_EvaluationScore`, `CK_KpiReviews_TotalScore` (điểm từ 0 đến 100).
  - Bổ sung bộ **Non-Clustered & Covering Indexes** tối ưu truy vấn nóng:
    + `IX_AttendanceDailies_EmployeeId_WorkDate` INCLUDE (`CheckInTime`, `CheckOutTime`, `WorkHours`, `Status`).
    + `IX_LeaveRequests_Status_FromDate_ToDate` INCLUDE (`EmployeeId`, `TotalDays`, `RequestType`).
    + `IX_Employees_DepartmentId_Status_IsDeleted` INCLUDE (`EmployeeCode`, `FullName`, `PositionId`, `BranchId`).
    + `IX_Candidates_JobPostingId_Status` INCLUDE (`FullName`, `Email`, `Phone`, `AppliedDate`).
    + `IX_JobPostings_Slug` (Unique WHERE `IsDeleted = 0`).
    + `IX_Contents_Slug` (Covering SEO Content).
    + Các index nghiệp vụ trên `EmployeeLeaveBalances`, `AttendanceLocks`, `AttendanceAdjustmentRequests`, `HrDecisions`, `CompanyAssets`, `EmployeeAssetAssignments`, `OnboardingChecklists`, `OnboardingTasks`, `InterviewSchedules`.
  - Thiết lập **Extended Properties** (`sp_addextendedproperty` / `sp_updateextendedproperty`) mô tả tiếng Việt chuẩn doanh nghiệp cho tất cả các bảng mới để hiển thị trực tiếp trên SSMS Database Diagram và công cụ quản trị CSDL.
- **`db/scripts/V6_02_EnterpriseHRM_SeedData.sql`**:
  - Nạp bộ dữ liệu mẫu thực tế, phong phú, chuẩn tiếng Việt (hoàn toàn idempotent, chạy nhiều lần an toàn):
    + 6 loại nghỉ phép tiêu chuẩn theo Bộ luật Lao động.
    + Khởi tạo Quỹ phép năm 2026 cho toàn bộ nhân sự hiện hữu.
    + 6 tin tuyển dụng chuẩn SEO đa phòng ban kèm Slug tiếng Việt không dấu chuẩn URL.
    + 18 hồ sơ ứng viên mẫu phủ kín toàn bộ 6 cột của Kanban tuyển dụng (`Applied`, `Screening`, `Interview`, `Offered`, `Hired`, `Rejected`).
    + Lịch phỏng vấn chi tiết kèm link họp trực tuyến, người phỏng vấn và tiêu chí chấm điểm.
    + Danh mục tài sản công ty (Laptop Dell, ThinkPad, Màn hình LG, Thẻ từ) kèm biên bản bàn giao đang hiệu lực.
    + Quyết định nhân sự điện tử: Quyết định bổ nhiệm Trưởng phòng, Điều chuyển chi nhánh, Khen thưởng hoàn thành dự án xuất sắc.
    + Khóa kỳ chấm công tháng 09/2026 & đơn giải trình bổ sung giờ vào-ra có minh chứng.
    + Quy trình Onboarding mẫu hoàn chỉnh kèm 10 đầu việc phân vai chặt chẽ cho HR, IT, Quản lý trực tiếp và Nhân viên.

### 4. Đồng bộ Migration, Tài liệu & Kiểm thử Tự động
- **EF Core Migration**:
  - Tạo migration `src/ERP.Infrastructure/Migrations/20261104191312_V6_01_EnterpriseHRM_Schema_And_Indexes.cs` (42 KB, 813 dòng) đồng bộ cấu trúc 9 bảng mới, cột `Slug`, cột audit và các chỉ mục lên CSDL.
- **Cập nhật Tài liệu Kiến trúc**:
  - `docs/ERD.md`: Cập nhật sơ đồ Mermaid hoàn chỉnh gồm toàn bộ 67 bảng CSDL phân tách 4 phân hệ rõ ràng (System: 12 bảng, HRM: 27 bảng, CMS: 7 bảng, SCM: 21 bảng).
  - `db/README.md`: Hướng dẫn chi tiết thứ tự chạy script trên SSMS (từ V1_00 đến V6_02), danh mục bảng và quy ước thiết kế CSDL.
- **Bộ Kiểm thử Tự động (`Phase2EnterpriseHrmDatabaseTests.cs`)**:
  - Bổ sung 6 bài kiểm thử toàn diện: cấu hình 9 DbSet, kiểm tra Check Constraints & Covering Indexes trên SQL Server, kiểm tra Extended Properties tiếng Việt, kiểm tra dữ liệu mẫu Hire-to-Retire, kiểm tra Soft Delete qua `IAuditable`, và kiểm tra bắt buộc vi phạm Check Constraint khi khoảng ngày không hợp lệ.
  - Tích hợp tự động thực thi script idempotent trong `TestAppFactory.CreateHost` đảm bảo môi trường kiểm thử luôn sở hữu đầy đủ ràng buộc CSDL và dữ liệu chuẩn.
- **Kết quả Build & Test**:
  - `dotnet build ERP_HRM_CMS.sln`: **Thành công — 0 Error(s)**.
  - `dotnet test tests/ERP_HRM_CMS.Tests`: **189 / 189 bài kiểm thử XANH 100%** (173 test gốc + 10 test Phiên 1 + 6 test CSDL Phiên 2).

## Phiên 3 (UI/UX Chuyên Sâu): Nâng Cấp Theme "Nordic People & Indigo Slate", Hiệu Ứng Động/Tĩnh Toàn Diện & Hệ Thống CSS HRM (đã hoàn tất 100%)

### 1. Tinh chỉnh Theme "Nordic People & Indigo Slate" Chuyên Biệt Cho HRM (`enterprise-theme.css`, `enterprise-theme.js`)
- **Tối ưu độ tương phản màu sắc đạt chuẩn WCAG AA**:
  - Light mode: `--erp-text-main: #0F172A`, `--erp-text-secondary: #334155`, `--erp-text-muted: #475569` (tương phản 5.9:1 trên nền trắng `#FFFFFF` và 5.4:1 trên `#F8FAFC`).
  - Dark mode: `--erp-text-main: #F8FAFC`, `--erp-text-secondary: #CBD5E1`, `--erp-text-muted: #94A3B8` (tương phản 5.8:1 trên nền `#111827` và `#0B0F19`).
  - Tokens văn bản trạng thái ngữ nghĩa WCAG AA: `--erp-success-text` (`#065F46` / `#A7F3D0`), `--erp-warning-text` (`#92400E` / `#FDE68A`), `--erp-danger-text` (`#991B1B` / `#FECACA`), `--erp-info-text` (`#075985` / `#BAE6FD`).
- **Tabular Nums cho số liệu (`tabular-nums`)**:
  - Cấu hình `font-variant-numeric: tabular-nums; font-feature-settings: "tnum" 1;` cho mọi con số tiền tệ, giờ công, số ngày phép, tỷ lệ KPI, mã nhân viên và ngày tháng để căn thẳng hàng dọc trên toàn bộ các bảng dữ liệu.
- **Table & UI Density Switcher (`Comfortable` vs `Compact`)**:
  - Nút chuyển đổi độ giãn cách trực tiếp trên Topbar (`#densityToggleBtn`), lưu vào `localStorage('erp_density')` và cookie `erp_density`:
    + `Comfortable` (Mặc định): Chiều cao dòng chuẩn 48px, khoảng đệm rộng rãi phục vụ thao tác chuột thông thường.
    + `Compact`: Chiều cao dòng 32px-34px, font-size `0.8125rem`, tối ưu cho chuyên viên nhân sự và kế toán theo dõi bảng công, bảng lương nhiều cột.
  - Hỗ trợ cuộn ngang cố định:
    + `sticky-thead`: Cố định tiêu đề bảng khi cuộn dọc.
    + `sticky-col-1` / `sticky-col-2`: Cố định cột Mã NV và Họ tên khi cuộn ngang màn hình.
    + `sticky-col-actions`: Cố định cột Thao tác ở mép phải bảng dữ liệu.

### 2. Xây dựng Bộ CSS & JS Component Chuyên Sâu Cho HRM (`wwwroot/css/hrm-workspace.css`, `wwwroot/css/admin-print.css`, `admin-ui.js`)
- **Bento Dashboard Grid**:
  - Lưới thẻ 12 cột đáp ứng (`.erp-bento-grid`, `.bento-col-12`, `.bento-col-8`, `.bento-col-6`, `.bento-col-4`, `.bento-col-3`).
  - Hiệu ứng hover nâng nhẹ (`.hover-lift`), viền sáng tinh tế (`.border-glow`) và khung skeleton shimmer (`.bento-shimmer`) khi đang tải dữ liệu.
- **Interactive Org Chart (Sơ đồ cây tổ chức trực quan)**:
  - Vẽ đường nối nhánh cây phân cấp phòng ban hoàn toàn bằng CSS (`.org-tree`).
  - Thẻ lãnh đạo phòng ban (`.org-node-card`) hiển thị avatar, tên phòng, chức danh lãnh đạo, số lượng nhân sự biên chế (`bi-people-fill`).
  - Nút thu gọn / mở rộng nhánh cây (`.org-toggle-btn`) có hiệu ứng chuyển cảnh mượt mà.
  - Khung nhìn tương tác (`.erp-org-viewport`, `.erp-org-canvas`) hỗ trợ phóng to/thu nhỏ (`zoom-in`, `zoom-out`, `zoom-reset`) và kéo chuột di chuyển (drag-to-pan). API khởi tạo: `ERP.orgChart.init()`.
- **ATS Kanban Board (Pipeline tuyển dụng ứng viên)**:
  - Cột pipeline cuộn độc lập (`.kanban-column-body`), thẻ ứng viên (`.kanban-card`) hiển thị điểm đánh giá (0-100), tag vị trí tuyển dụng, avatar và ngày nộp.
  - Tương tác kéo-thả HTML5 Drag & Drop / Pointer Events mượt mà kèm hiệu ứng nhấc thẻ nghiêng nhẹ (`.is-dragging`: `transform: rotate(2.5deg) scale(1.02); box-shadow: var(--erp-shadow-xl);`).
  - Tự động đếm và cập nhật số lượng hồ sơ (`.kanban-counter`) trên đầu mỗi cột. API khởi tạo: `ERP.kanban.init()`.
- **Calendar & Shift Matrix Grid**:
  - Ma trận lịch tháng/tuần theo dõi ngày công thực tế theo từng ngày trong tháng (`.erp-shift-matrix-container`).
  - Ô ca làm việc trực quan (`.erp-shift-cell`) với mã màu trạng thái chuẩn WCAG AA: Đúng giờ (`shift-ontime`), Đi muộn (`shift-late`), Nghỉ phép (`shift-leave`), Tăng ca (`shift-ot`), Công tác (`shift-trip`), Vắng mặt (`shift-absent`), Cuối tuần (`shift-weekend`).
  - Thanh chú giải ký hiệu trực quan (`.shift-legend-bar`).
- **Nâng cấp Glassmorphism Command Palette (`Ctrl+K`)**:
  - Khung modal làm mờ hậu cảnh (`backdrop-filter: blur(16px); background: rgba(255, 255, 255, 0.85);` / Dark: `rgba(17, 24, 39, 0.82);`).
  - Phân nhóm kết quả tìm kiếm rõ ràng: 👤 Hồ sơ Nhân sự, ⚡ Lối tắt nhanh, 📝 Đơn từ & Nghỉ phép, 🎯 Tuyển dụng, 📦 Hàng hóa & Đơn hàng.
  - Điều hướng bằng phím mũi tên Lên/Xuống + `Enter`, debounce mượt mà.
- **Print & PDF Stylesheet (`wwwroot/css/admin-print.css`)**:
  - Định dạng in chuẩn khổ A4 dọc và A4 ngang sắc nét cho Phiếu lương, Hợp đồng lao động, Quyết định nhân sự, Sơ yếu lý lịch.
  - Ẩn 100% thanh bên (sidebar), thanh trên (topbar), nút bấm, phân trang, thông báo toast khi nhấn In (`@media print`).
  - Khung chữ ký chuẩn 3 bên (`.print-signatures`: Người lập biểu, Kế toán trưởng, Giám đốc / Người lao động) và chống ngắt trang giữa hàng (`break-inside: avoid;`).

### 3. Phủ Đồng Bộ Hiệu Ứng Tĩnh & Động Cho Các View HRM
- **Chuyển đổi 100% các View còn dùng HTML Bootstrap thô sang bộ 12 Shared TagHelpers**:
  - `Views/NhanSu/Index.cshtml`: Chuyển sang `<erp-page-header>`, 4 `<erp-stat-card>`, `<erp-filter-bar>`, `<table class="table erp-table sticky-thead">`, `<erp-status-badge>`, `<erp-avatar>`, `<erp-empty-state>`.
  - `Views/PhongBan/Index.cshtml`: Tích hợp `<erp-page-header>`, `<erp-tabs>` gồm 3 tab (Sơ đồ cây tổ chức Org Chart tương tác, Danh sách phòng ban dạng cây đệ quy, Danh mục chức danh), loại bỏ form thô.
  - `Views/HopDong/Index.cshtml`: Chuyển sang `<erp-page-header>`, `<erp-stat-card>`, bảng hợp đồng có `sticky-col`, thẻ cảnh báo hết hạn có `tabular-nums`, `<erp-status-badge>`, `<erp-empty-state>`.
  - `Views/ChamCong/BangCongThang.cshtml`: Nâng cấp thành lưới Shift Matrix Grid hoàn chỉnh với cột Mã NV và Họ tên cố định khi cuộn ngang, các cell chấm công phân màu và thanh chú giải trạng thái.
  - `Views/ChamCong/Index.cshtml`: Chuyển sang `<erp-page-header>`, `<erp-filter-bar>`, `<erp-status-badge>`, `tabular-nums`.
  - `Views/DonTu/Index.cshtml`: Chuyển sang `<erp-page-header>`, 4 `<erp-stat-card>`, `<erp-filter-bar>`, `<erp-status-badge>`, `tabular-nums`.
  - `Views/BangLuong/Index.cshtml` & `Views/BangLuong/PhieuLuong.cshtml`: Chuyển sang `<erp-page-header>`, 4 `<erp-stat-card>`, `<erp-filter-bar>`, `tabular-nums` trên toàn bộ các cột tiền tệ, tích hợp in ấn chuẩn A4 qua `admin-print.css`.
- **Cập nhật Sidebar trong `_Layout.cshtml`**:
  - Bổ sung gọn gàng, chuyên nghiệp hệ sinh thái HRM: Cổng cá nhân ESS, Hồ sơ nhân sự, Sơ đồ tổ chức Org Chart, Hợp đồng lao động, Chấm công ngày, Bảng công & Shift Matrix, Heatmap chấm công, Định nghĩa ca làm, Đơn từ & Phê duyệt, People Analytics & Báo cáo.
- **Mở rộng `/UI/Styleguide`**:
  - Thêm Section 7 trình diễn chuyên biệt các thành phần HRM Workspace: Density Switcher & Dense Table, Interactive Org Chart Node, ATS Kanban Board Drag & Drop, Calendar & Shift Matrix Cells, Bento Dashboard Grid.

### 4. Kiểm Thử Tự Động & Bàn Giao
- Bổ sung 6 bài kiểm thử tự động chuyên sâu tại `tests/ERP_HRM_CMS.Tests/UiPhase3HrmThemeTests.cs`:
  - `EnterpriseTheme_Contains_WcagTokens_TabularNums_And_DensityClasses`
  - `HrmWorkspace_Css_Contains_Bento_OrgChart_Kanban_And_ShiftMatrix`
  - `AdminPrint_Css_Contains_A4_PrintRules_And_SignatureBoxes`
  - `ClientScripts_Contain_Density_OrgChart_And_Kanban_Handlers`
  - `Layout_Includes_HrmWorkspaceCss_DensityBtn_And_HrmSidebar`
  - `Styleguide_Renders_HrmWorkspaceSection_For_SuperAdmin`
- **Kết quả Build & Test**:
  - `dotnet build ERP_HRM_CMS.sln`: **Thành công — 0 Error(s)**.
  - `dotnet test tests/ERP_HRM_CMS.Tests`: **195 / 195 bài kiểm thử XANH 100%** (173 test gốc + 10 test Phiên 1 + 6 test CSDL Phiên 2 + 6 test UI Phiên 3).

## PHIÊN 4: Hồ Sơ Nhân Sự 360°, Sơ Đồ Tổ Chức Tương Tác, Quyết Định Nhân Sự & Tài Sản Cấp Phát (ĐÃ HOÀN TẤT)

### 1. Tách Triệt Để `AppDbContext` Khỏi Controllers (Clean Architecture Decoupling)
- **100% Decoupled**: `NhanSuController`, `PhongBanController`, `HopDongController` không còn trực tiếp inject hoặc tham chiếu đến `AppDbContext`.
- Đã di chuyển toàn bộ logic truy vấn dữ liệu, giao tác (transactions), lọc phân trang và mapping DTO sang các Application Services chuyên biệt:
  + `IDepartmentService` & `DepartmentService` (quản lý cơ cấu tổ chức, phòng ban, drawer chi tiết, chức vụ).
  + `IContractService` & `ContractService` (quản lý hợp đồng, cảnh báo hết hạn, in hợp đồng chuẩn A4).
  + `IHrDecisionService` & `HrDecisionService` (quản lý quyết định bổ nhiệm, điều chuyển, điều chỉnh lương, thôi việc, in quyết định chuẩn hành chính).
  + `IAssetManagementService` & `AssetManagementService` (danh mục thiết bị, cấp phát bàn giao, thu hồi, kiểm tra tài sản chưa trả trước khi cho thôi việc).
  + `IEmployeeService` & `EmployeeService` (hồ sơ 360°, ClosedXML Excel import/export, template mẫu, smart alerts, in sơ yếu lý lịch chuẩn A4).
- Áp dụng triệt để `IDataScopeService` cho phân quyền dữ liệu phạm vi (Company / Branch / Department / Self) và `.AsNoTracking()` cho toàn bộ các thao tác đọc.

### 2. Nghiệp Vụ Quyết Định Nhân Sự & Kiểm Soát Thôi Việc (HR Decisions & Termination)
- **Transaction Nguyên Tử (Atomic Execution)**:
  + Khi duyệt quyết định loại `Appointment`: Hệ thống tự động chuyển đổi chức vụ nhân viên (`PositionId`), cập nhật mức lương mới (nếu có), tự động ghi nhận nhật ký vào `EmployeeChangeHistories`.
  + Khi duyệt quyết định loại `Transfer`: Hệ thống tự động chuyển đổi phòng ban (`DepartmentId`), ghi nhận nhật ký thay đổi phòng ban.
  + Khi duyệt quyết định loại `SalaryAdjustment`: Hệ thống cập nhật lương cơ bản vào hợp đồng lao động đang hiệu lực (`BasicSalary`), ghi nhận lịch sử điều chỉnh lương.
  + Khi duyệt quyết định loại `Termination`: Hệ thống tự động chuyển trạng thái nhân viên thành `Resigned`, đóng các hợp đồng đang hiệu lực (`Terminated`), thu hồi quyền đăng nhập tài khoản người dùng (`User.IsActive = false`, đổi `SecurityStamp` để vô hiệu hóa cookie tức thì), và gửi cảnh báo realtime qua SignalR.
- **Kiểm soát Tài sản khi Thôi việc**: Tích hợp kiểm tra `HasUnreturnedAssetsAsync(employeeId)`. Nếu nhân viên còn tài sản đang mượn (Laptop, Thẻ từ...), hệ thống hiển thị cảnh báo đỏ và danh sách chi tiết các thiết bị chưa thu hồi ngay trên giao diện phê duyệt quyết định.

### 3. Nghiệp Vụ Cấp Phát & Thu Hồi Tài Sản (Asset Management)
- Xây dựng module quản lý thiết bị `TaiSanNhanSuController` với đầy đủ tính năng:
  + Danh mục thiết bị doanh nghiệp (`/TaiSanNhanSu/Index`): lọc theo loại thiết bị (Laptop, Màn hình, Thẻ từ...), trạng thái (Trong kho, Đang cấp phát, Bảo trì, Đã thanh lý) và chi nhánh.
  + Thêm mới tài sản (`/TaiSanNhanSu/CreateAsset`): quản lý mã tài sản, số serial, nguyên giá, hạn bảo hành, cấu hình kỹ thuật.
  + Bàn giao cấp phát (`/TaiSanNhanSu/CapPhat`): chọn tài sản trong kho, nhân viên tiếp nhận, ghi nhận tình trạng lúc giao.
  + Thu hồi tài sản (`/TaiSanNhanSu/ThuHoi`): ghi nhận tình trạng khi nhận lại, tự động chuyển trạng thái tài sản về `Available`, `Maintenance` hoặc `Liquidated`.
  + Chi tiết & lịch sử tài sản (`/TaiSanNhanSu/Details`): timeline các lần bàn giao và thu hồi.

### 4. Hồ Sơ Nhân Sự 360° & Sơ Đồ Cây Tổ Chức Tương Tác
- **Hồ sơ 360° (`Views/NhanSu/Details.cshtml`)**:
  + Thiết kế 7 tab trượt hiện đại với sliding indicator:
    1. `tab-info`: Thông tin cá nhân, định danh, liên hệ, người liên hệ khẩn cấp, học vấn & ngân hàng.
    2. `tab-contracts`: Danh sách hợp đồng lao động, mức lương, thời hạn và nút "In Hợp đồng (A4)".
    3. `tab-decisions`: Dòng thời gian biến động nhân sự `<erp-timeline>` thể hiện toàn bộ các quyết định bổ nhiệm, điều chuyển, khen thưởng, kỷ luật.
    4. `tab-dependants`: Danh sách người phụ thuộc & công cụ tính nhanh giảm trừ gia cảnh thuế TNCN.
    5. `tab-skills`: Ma trận kỹ năng, ngoại ngữ & chứng chỉ chuyên môn.
    6. `tab-assets`: Danh sách thiết bị và tài sản công ty đang giao cho nhân viên sử dụng.
    7. `tab-documents`: Kho hồ sơ số hóa đính kèm (CCCD, bằng cấp, hợp đồng scan, giấy khám sức khỏe).
- **Sơ đồ Cây Tổ chức (`Views/PhongBan/Index.cshtml`)**:
  + Tích hợp ngăn kéo tương tác `<erp-drawer id="deptDetailDrawer">` mở mượt mà từ cạnh phải khi click vào bất kỳ node phòng ban nào trên cây sơ đồ hoặc bảng danh sách.
  + Hiển thị thông tin lãnh đạo bộ phận, thanh tiến độ định biên nhân sự (`Biên chế: actual / quota`), danh sách nhân viên trực thuộc kèm avatar và chức danh.

### 5. Cảnh Báo Nhân Sự Thông Minh & Import/Export Excel Chuyên Nghiệp
- **Cảnh báo Thông minh (`Views/NhanSu/CanhBao.cshtml`)**:
  + 4 thẻ Bento thống kê: Hợp đồng sắp hết hạn (30/15/7 ngày phân cấp độ khẩn Critical/Warning/Normal), Thử việc sắp kết thúc, Chứng chỉ sắp hết hạn, Sinh nhật nhân sự trong tháng.
- **Excel ClosedXML Import/Export**:
  + Xuất Excel danh sách nhân sự đa tiêu chí theo bộ lọc hiện tại với định dạng font chữ, màu viền bảng biểu chuyên nghiệp.
  + Tải template Excel mẫu với đầy đủ tiêu đề cột và hướng dẫn nhập liệu.
  + Import hàng loạt nhân sự từ Excel có kiểm tra trùng mã NV, email, định dạng ngày tháng và báo cáo chi tiết từng dòng lỗi.

### 6. In Ấn Văn Bản Hành Chính Chuẩn Việt Nam Khổ A4 (`admin-print.css`)
- **In Sơ yếu lý lịch (`/NhanSu/InSoYeuLyLich`)**: Khổ A4 chuẩn biểu mẫu lý lịch tự thuật nhân sự tiếng Việt.
- **In Hợp đồng lao động (`/HopDong/InHopDong`)**: Khổ A4 chuẩn theo quy định Bộ luật Lao động 2019 Điều 21, đầy đủ thông tin bên A, bên B, điều khoản công việc, lương, bảo hiểm, quyền hạn và khung chữ ký 2 bên.
- **In Quyết định nhân sự (`/QuyetDinhNhanSu/InQuyetDinh`)**: Khổ A4 chuẩn văn bản hành chính nhà nước (Quốc hiệu, Tiêu ngữ, Căn cứ pháp lý, Điều khoản thi hành, Nơi nhận, Chữ ký đóng dấu).

### 7. Tự Động Hóa Kiểm Thử & Xác Nhận 100% Green
- Bổ sung 10 bài kiểm thử tích hợp chuyên sâu tại `tests/ERP_HRM_CMS.Tests/Phase4CoreHrm360Tests.cs`:
  1. `HrDecision_Appointment_Updates_Employee_Position_And_Logs_History`
  2. `HrDecision_Transfer_Updates_Employee_Department`
  3. `HrDecision_Termination_Sets_Resigned_And_Deactivates_User_Account`
  4. `HrDecision_Approve_AlreadyApproved_Returns_Failure`
  5. `Asset_Assign_And_Return_Cycle_Works_Correctly`
  6. `SmartAlerts_Returns_Expiring_Contracts_And_Probations`
  7. `EmployeeService_GenerateImportTemplate_Returns_Valid_Excel_Bytes`
  8. `EmployeeService_ExportToExcel_Returns_Valid_Excel_Bytes`
  9. `ContractService_GetPrintModel_Returns_Accurate_Labor_Details`
  10. `EmployeeService_GetPrintableResume_Returns_Full_360_Profile`
- **Kết quả Build & Test**:
  + `dotnet build ERP_HRM_CMS.sln`: **Thành công — 0 Error(s)**.
  + `dotnet test tests/ERP_HRM_CMS.Tests`: **205 / 205 test XANH 100%** (0 Failed).

## PHIÊN 5: Chấm Công Thông Minh, Phân Ca (Shift Roster), Quỹ Phép Năm & Duyệt Đơn Từ Đa Cấp (ĐÃ HOÀN TẤT)

### 1. Tách Triệt Để `AppDbContext` Khỏi Phân Hệ Chấm Công & Đơn Từ (Clean Architecture Decoupling)
- **100% Decoupled**: `ChamCongController` và `DonTuController` đã được tái cấu trúc hoàn toàn, loại bỏ 100% việc inject trực tiếp `AppDbContext` cũng như các truy vấn `.ToListAsync()` không giới hạn.
- Đóng gói toàn bộ logic truy vấn, phân trang, giao tác nghiệp vụ vào 3 Application Services chuẩn mực:
  + `IShiftScheduleService` & `ShiftScheduleService`: Quản lý danh mục ca làm việc (ca hành chính, ca gãy, ca đêm qua ngày `IsOvernight`), thời gian ân hạn đi muộn (`GracePeriodMinutes = 15`), hệ số tính công (`WorkFactor = 1.0m`), lập bảng phân ca trực quan theo tuần (`GetWeeklyRosterAsync`), gán ca cá nhân / hàng loạt, tính năng 1-click sao chép lịch phân ca tuần trước sang tuần sau (`CopyWeekScheduleAsync`).
  + `IAttendanceService` & `AttendanceService`: Chấm công nhanh vào/ra có ân hạn, ma trận bảng chấm công tháng 31 ngày (`GetMonthlyTimesheetAsync`), API xem chi tiết ngày công qua Drawer (`GetDayAttendanceDetailAsync`), xuất Excel bảng chấm công chuẩn A4 có chữ ký (`ExportMonthlyTimesheetToExcelAsync`), import dữ liệu máy chấm công vân tay/khuôn mặt từ Excel/CSV (`ImportAttendanceLogAsync`) tự động đối soát ca để tính phút đi muộn, về sớm, giờ tăng ca OT; Cơ chế khóa kỳ công (`LockPeriodAsync`, `UnlockPeriodAsync`) tích hợp bắt buộc lý do mở khóa và ghi nhận vào `AuditLogs`.
  + `ILeaveManagementService` & `LeaveManagementService`: Tính phép thâm niên theo Điều 114 Bộ luật Lao động VN (+1 ngày cho mỗi 5 năm làm việc), khởi tạo/tính lại quỹ phép năm (`InitializeOrRecalculateBalanceAsync`, `RecalculateAllBalancesAsync`), tạo đơn từ với validation chặt chẽ (kiểm tra vượt số dư phép năm, trùng lặp khoảng thời gian nghỉ, bắt buộc chứng từ đính kèm theo loại nghỉ); Quy trình phê duyệt đơn từ đa cấp (`ApproveLeaveRequestAsync`: Trưởng phòng `TP` -> Giám đốc/HR `GD`), thực thi chính sách chống tự phê duyệt đơn từ của chính mình (`LeaveApprovalPolicy`), tự động trừ quỹ phép năm và đồng bộ trạng thái sang `AttendanceDailies` (kèm SignalR notification), từ chối đơn (`RejectLeaveRequestAsync`), duyệt hàng loạt (`BatchApproveAsync`); Quản lý đơn giải trình chấm công (`AttendanceAdjustmentRequest`) và tự động chuẩn hóa ngày công khi được phê duyệt.
- Áp dụng nghiêm ngặt `IDataScopeService` cho phân quyền phạm vi dữ liệu (Chi nhánh / Phòng ban / Cá nhân) và `.AsNoTracking()` cho toàn bộ truy vấn đọc.

### 2. Ma Trận Bảng Chấm Công Tháng & Ngăn Kéo Chi Tiết (Drawer)
- **Ma trận 31 ngày (`Views/ChamCong/BangCongThang.cshtml`)**:
  + Thiết kế theo chuẩn giao diện `Compact Density`, cố định 2 cột đầu tiên (Mã nhân viên, Họ và tên) khi cuộn ngang (`sticky-col`).
  + 31 cột ngày trong tháng tự động tính toán và hiển thị badge trạng thái màu sắc trực quan: `X` (Đi làm đủ), `P` (Nghỉ phép có lương), `M` (Đi muộn / Về sớm), `OT` (Tăng ca), `KL` (Nghỉ không lương / Vắng mặt), `-` (Ngày nghỉ cuối tuần / Chưa có dữ liệu).
  + Cột tổng kết tự động tính: Tổng ngày công, Tổng số phút đi muộn/về sớm, Số giờ làm thêm OT, Số ngày nghỉ phép.
  + **Tương tác Micro-interaction**: Click vào bất kỳ ô ngày công nào trên bảng sẽ kích hoạt `<erp-drawer id="dayDetailDrawer">` trượt mượt mà từ cạnh phải, nạp dữ liệu chi tiết qua AJAX gồm: Giờ check-in đầu tiên, Giờ check-out cuối cùng, Ca làm việc, Phút trễ/sớm, Giờ OT, Tọa độ GPS/Thiết bị, và liên kết đến các đơn nghỉ phép / giải trình tương ứng.
  + Nút "Xuất Excel Bảng công" tải về file bảng chấm công A4 với font chữ chuẩn, viền bảng và các ô ký nhận (Người lập biểu, Trưởng phòng, Giám đốc).
  + Tích hợp Modal "Khóa kỳ công" và "Mở khóa kỳ công" yêu cầu nhập lý do kiểm toán.

### 3. Phân Ca Trực Quan & Sao Chép Lịch Tuần 1-Click
- **Giao diện Phân ca Tuần (`Views/ChamCong/PhanCa.cshtml`)**:
  + Trình bày lưới lịch 7 ngày trong tuần từ Thứ 2 đến Chủ nhật theo từng nhân viên và phòng ban.
  + Hỗ trợ bộ lọc phòng ban, tuần làm việc (Tuần hiện tại / Tuần trước / Tuần sau).
  + Modal "Gán ca làm việc" nhanh chóng cho từng nhân viên hoặc nhiều nhân viên trong một ngày.
  + Modal "Sao chép lịch tuần": Cho phép sao chép nguyên vẹn toàn bộ lịch trực / phân ca của tuần trước sang tuần được chọn chỉ bằng 1 thao tác, tiết kiệm tối đa thời gian cho Quản lý chi nhánh / Trưởng phòng.

### 4. Quỹ Phép Năm & Quản Lý Đơn Từ Đa Cấp
- **Quản lý Quỹ phép năm (`Views/DonTu/QuyPhepNam.cshtml`)**:
  + Bảng theo dõi chi tiết quỹ phép của từng nhân viên: Thâm niên công tác (năm), Phép tiêu chuẩn (12 ngày), Phép thâm niên (Điều 114), Phép tồn năm trước, Tổng phép được nghỉ, Phép đã dùng, Số ngày phép còn lại.
  + Nút "Tính lại toàn bộ quỹ phép" cho phép HR cập nhật đồng loạt theo năm mới.
- **Trung tâm Phê duyệt Đơn từ (`Views/DonTu/Index.cshtml`)**:
  + Thống kê 4 Bento KPIs: Chờ duyệt, Trưởng phòng đã duyệt, Đã duyệt chính thức, Đã từ chối.
  + Bảng dữ liệu có phân trang `PaginatedList`, bộ lọc theo phòng ban, loại nghỉ, khoảng thời gian.
  + **Duyệt hàng loạt (Bulk Approval)**: Cột checkbox chọn nhiều đơn cùng lúc kèm thanh tác vụ nổi và nút "Duyệt các đơn đã chọn", xử lý qua AJAX và cập nhật thông báo `ERP.toast`.
  + Phê duyệt từng đơn với modal xác nhận hoặc từ chối có bắt buộc lý do.
- **Đơn Giải Trình Chấm Công (`Views/DonTu/GiaiTrinh.cshtml`)**:
  + Quản lý các đề xuất giải trình: Quên quẹt thẻ vào, Quên quẹt thẻ ra, Đi công tác đột xuất, Thiết bị lỗi.
  + Khi đơn giải trình được phê duyệt: Hệ thống tự động đồng bộ giờ vào/ra thực tế vào `AttendanceDailies`, tính toán lại số phút đi muộn = 0 và chuyển trạng thái công về `Present`.

### 5. Tự Động Hóa Kiểm Thử & Xác Nhận Toàn Hệ Thống 100% Green
- Xây dựng 16 bài kiểm thử chuyên sâu tại `tests/ERP_HRM_CMS.Tests/Phase5AttendanceAndLeaveTests.cs`:
  1. `CalculateSeniorityBonus_Follows_LaborCode_Article114` (Theory 8 kịch bản thâm niên: 1, 4, 5, 9, 10, 14, 15, 20 năm).
  2. `InitializeOrRecalculateBalance_Calculates_Standard_Seniority_And_Remaining` (Khởi tạo quỹ phép +2 ngày thâm niên cho nhân viên 11 năm).
  3. `CreateLeaveRequest_Fails_When_Days_Exceed_Remaining_Balance` (Chặn tạo đơn khi số ngày nghỉ vượt quá quỹ phép còn lại).
  4. `CreateLeaveRequest_Fails_When_Overlapping_With_Existing_Request` (Chặn tạo đơn khi khoảng thời gian trùng lặp với đơn đã có).
  5. `FinalApproval_Deducts_Leave_Balance_And_Updates_AttendanceDailies` (Quy trình duyệt 2 cấp TP -> GD: tự động trừ số ngày phép tồn, chuyển trạng thái chấm công sang Leave).
  6. `AttendanceLock_Blocks_Approval_And_Unlocks_With_Audit` (Khóa kỳ công chặn tạo/duyệt đơn lùi ngày; mở khóa ghi nhận AuditLog).
  7. `ImportAttendanceLog_Calculates_Late_Early_And_Overtime` (Import file Excel máy quẹt thẻ, tự động đối soát ca tính đi muộn 30 phút).
  8. `AttendanceAdjustment_Approval_Normalizes_Daily_Attendance` (Duyệt giải trình quên quẹt thẻ, chuẩn hóa giờ ra và đưa trạng thái về Present).
  9. `ShiftSchedule_CopyWeekSchedule_Replicates_Shifts_Accurately` (Sao chép lịch phân ca tuần nguồn sang tuần đích chuẩn xác).
- **Kết quả Build & Test Toàn Hệ Thống**:
  + `dotnet build ERP_HRM_CMS.sln`: **Thành công — 0 Error(s)**.
  + `dotnet test tests/ERP_HRM_CMS.Tests`: **221 / 221 bài kiểm thử XANH 100% (0 Failed, 0 Skipped)**.

## PHIÊN 6: Cổng Tự Phục Vụ Nhân Viên (ESS — Employee Self-Service Portal) (ĐÃ HOÀN TẤT)

### 1. Bảo Mật Dữ Liệu Tuyệt Đối (Zero IDOR) & Điều Hướng Thông Minh
- **Nguyên tắc Zero IDOR**: Toàn bộ các API và Controller Actions trong `SelfServiceController` tại route `/CongNhanVien` chỉ sử dụng danh tính nhân viên đọc trực tiếp từ `ICurrentUserService.EmployeeId` (gốc từ Claims xác thực). Tuyệt đối không chấp nhận tham số `employeeId` từ Query string hay Form POST, chặn đứng 100% nguy cơ rò rỉ dữ liệu hoặc xem trộm phiếu lương/hồ sơ của đồng nghiệp.
- **Điều hướng thông minh khi Đăng nhập (`AuthController.RedirectBasedOnRole`)**:
  + Khi tài khoản mang vai trò `EMPLOYEE` đăng nhập thành công vào `/Auth/Login`, hệ thống tự động điều hướng mặc định thẳng vào `/CongNhanVien`.
  + Tài khoản chỉ có vai trò `EMPLOYEE` khi cố truy cập các màn hình quản trị nhân sự tổng hợp (`/NhanSu/Index`, `/BangLuong/Index`, `/DonTu/Duyet`) bị chặn nghiêm ngặt bởi `KiemTraQuyenAttribute` và điều hướng về trang từ chối quyền (403 / KhongCoQuyen).

### 2. Thiết Kế Không Gian Cổng Nhân Viên Theo Chuẩn Modern Bento Workspace
- **Giao diện Responsive Desktop & Mobile (`Views/CongNhanVien/Index.cshtml`)**:
  + **Bento Header Banner**: Thẻ hồ sơ cá nhân màu gradient Indigo hiện đại, hiển thị Họ tên, Mã nhân viên, Chức vụ, Phòng ban, Chi nhánh, Ngày vào làm, Số năm thâm niên và 5 tab chuyển đổi mượt mà không cần tải lại trang:
    1. `overview`: Bento Workspace tổng quan (đồng hồ số, widget check-in, vòng tròn quỹ phép, công tháng, đơn từ gần đây, phiếu lương mới nhất).
    2. `calendar`: Lịch chấm công tháng cá nhân trực quan.
    3. `requests`: Trung tâm đơn từ cá nhân (nghỉ phép, OT, công tác, giải trình).
    4. `payslips`: Tra cứu phiếu lương bảo mật (xác thực mật khẩu).
    5. `profile`: Hồ sơ cá nhân, tài sản bàn giao, hợp đồng, KPI, đào tạo & cập nhật thông tin liên hệ.
  + **Widget Chấm công Trực tuyến (Web Check-in / Check-out)**:
    - Đồng hồ số thời gian thực chuẩn Giờ Việt Nam (GMT+7) với font `JetBrains Mono` cập nhật liên tục từng giây.
    - Hiển thị ca làm việc hôm nay, thời gian bắt đầu - kết thúc và thời gian ân hạn (`GracePeriodMinutes`).
    - Nút bấm "Check-in Ngay" có hiệu ứng sóng lan tỏa (`btn-checkin-pulse`).
    - Tự động ghi nhận địa chỉ IP, thiết bị (User-Agent) và tính toán số phút đi muộn / đúng giờ ngay khi thực hiện.
    - Nút "Check-out Kết Thúc Ca" tự động tính số giờ làm việc thực tế (trừ giờ nghỉ trưa) và giờ làm thêm OT.
  + **Quỹ Phép Năm (SVG Circular Progress Ring)**:
    - Vòng tròn tiến độ SVG hiển thị tỷ lệ phần trăm số ngày phép đã sử dụng so với tổng quỹ phép.
    - Phân tích chi tiết: Phép tiêu chuẩn (12 ngày), Phép thâm niên (+1 ngày mỗi 5 năm theo Điều 114 BLLĐ), Phép tồn năm trước, Số ngày đã dùng và Số ngày phép còn lại.
  + **Lịch Chấm Công Cá Nhân & Thống Kê Tháng**:
    - Lưới 31 ô ngày trong tháng với màu sắc trạng thái chuẩn hóa: `X` (Đủ công), `P` (Nghỉ phép), `M` (Đi muộn/về sớm), `OT` (Tăng ca), `KL` (Vắng mặt), `-` (Cuối tuần).
    - Thống kê tự động: Ngày công thực tế, Số giờ tăng ca, Số lần đi muộn, Tổng số phút trễ.
    - **Tính năng 1-Click Giải trình**: Trên các ô ngày công có sự cố (quên quẹt thẻ, vắng mặt trong quá khứ), nhân viên bấm ngay nút "Giải trình", hệ thống tự động mở Drawer `#adjustmentDrawer` và điền sẵn ngày vào form giải trình.

### 3. Tra Cứu Phiếu Lương Bảo Mật (Secure Payslip Viewer)
- **Cơ chế Xác thực Mật khẩu Đăng nhập (Bank-grade Session Security)**:
  + Dữ liệu tài chính và tiền lương được bảo vệ nghiêm ngặt: khi nhân viên truy cập tab Phiếu Lương hoặc gọi API `/CongNhanVien/ChiTietPhieuLuong/{id}`, hệ thống yêu cầu xác thực lại mật khẩu tài khoản qua modal `#unlockPayslipModal`.
  + Gửi yêu cầu AJAX đến `/CongNhanVien/XacThucMatKhauPhieuLuong`: đối soát hash mật khẩu bằng `MatKhauHelper.Verify`. Khi đúng mật khẩu, hệ thống cấp phiên mở khóa an toàn có hiệu lực đúng **5 phút** trong Session (`ESS_PAYSLIP_UNLOCK_EXPIRY`). Sau 5 phút, hệ thống tự động khóa lại.
  + Nếu chưa mở khóa phiên, API trả về `403 Forbidden` kèm flag `{ requiresUnlock = true }`.
  + **Phân rã thu nhập minh bạch (Drawer `#payslipDetailDrawer`)**: Lương cơ bản, Lương ngày công thực tế, Tiền làm thêm giờ (OT 150%), Phụ cấp chức vụ, Phụ cấp ăn trưa/đi lại, Thưởng KPI, Tổng thu nhập (Gross), Các khoản bảo hiểm (BHXH 8%, BHYT 1.5%, BHTN 1%), Giảm trừ gia cảnh bản thân, Giảm trừ người phụ thuộc, Thuế TNCN lũy tiến, Phạt đi muộn và Thực lĩnh chuyển khoản (Net).
  + **Bản In Phiếu Lương A4 Chuẩn Mực (`/CongNhanVien/InPhieuLuong/{id}`)**: Thiết kế bản in sạch đẹp, chuyên nghiệp, ẩn các thanh điều khiển khi in bằng `@media print`.

### 4. Quản Lý Đơn Từ Cá Nhân & Hồ Sơ 360° Bản Thân
- **Tạo đơn đa năng (`<erp-drawer id="createLeaveDrawer">`)**: Nghỉ phép năm, Nghỉ ốm đau, Nghỉ thai sản, Nghỉ không lương, Đăng ký làm thêm giờ (OT), Đăng ký đi công tác kèm đính kèm tài liệu chứng từ/ảnh.
- **Rút lại (Hủy) đơn tự phục vụ**: Cho phép nhân viên chủ động bấm "Rút lại đơn" đối với các đơn đang ở trạng thái `PendingApproval` (`CHO_DUYET`). Đơn được chuyển trạng thái sang `Rejected` với ghi chú nhân viên tự rút lại, đồng thời ngăn chặn tuyệt đối nhân viên khác can thiệp (Zero IDOR).
- **Hồ sơ Cá nhân & Phát triển Bản thân**:
  + Xem CCCD, Mã số thuế, Mã BHXH, Tài khoản ngân hàng, Thông tin liên hệ.
  + Danh sách tài sản/thiết bị công ty đang bàn giao (Laptop, Serial number, Ngày bàn giao, Tình trạng).
  + Hợp đồng lao động, Lương cơ bản, Thời hạn.
  + Đánh giá KPI và các khóa đào tạo nội bộ.
  + Modal cập nhật thông tin liên hệ (`#editContactModal`) và Modal đăng ký người phụ thuộc giảm trừ gia cảnh (`#registerDependentModal`).

### 5. Tự Động Hóa Kiểm Thử & Nghiệm Thu 100% Green
- Xây dựng 7 bài kiểm thử tích hợp chuyên sâu tại `tests/ERP_HRM_CMS.Tests/Phase6SelfServicePortalTests.cs`:
  1. `Employee_Login_Redirects_Default_To_CongNhanVien` (Kiểm tra điều hướng tài khoản EMPLOYEE vào `/CongNhanVien`).
  2. `Employee_Cannot_Access_Management_Pages_Returns_Forbidden` (Kiểm tra chặn tài khoản EMPLOYEE truy cập trang quản trị HR/Lương/Duyệt đơn).
  3. `Employee_Can_Access_CongNhanVien_Portal_Returns_200` (Kiểm tra truy cập cổng ESS Portal thành công).
  4. `WebCheckIn_And_CheckOut_Workflow_Succeeds_And_Prevents_Duplicate` (Kiểm tra quy trình Check-in, chặn check-in trùng, Check-out tính giờ công).
  5. `SecurePayslip_Requires_Password_Unlock_And_Blocks_IDOR` (Kiểm tra mở khóa mật khẩu 5 phút, chặn mật khẩu sai, và chặn đứng tấn công IDOR xem phiếu lương của người khác).
  6. `LeaveRequest_Can_Be_Cancelled_By_Owner_And_Blocks_Other_Employee` (Kiểm tra rút lại đơn chờ duyệt và chặn IDOR nhân viên khác hủy đơn).
  7. `ContactUpdate_And_DependentRegistration_Succeeds` (Kiểm tra cập nhật thông tin liên hệ và đăng ký người phụ thuộc).
- **Kết quả Build & Test Toàn Hệ Thống**:
  + `dotnet build ERP_HRM_CMS.sln`: **Thành công — 0 Error(s)**.
  + `dotnet test tests/ERP_HRM_CMS.Tests`: **228 / 228 bài kiểm thử XANH 100% (0 Failed, 0 Skipped)**.

## PHIÊN 7: Tuyển Dụng ATS (Kanban Pipeline), Onboarding/Offboarding & Cổng Tuyển Dụng Công Khai Chuẩn SEO (ĐÃ HOÀN TẤT)

### 1. Phân Hệ Quản Trị Tuyển Dụng Nội Bộ (ATS) & Onboarding/Offboarding (`/TuyenDung` — Clean Architecture)
- **Tầng Nghiệp vụ Application (`IRecruitmentService`, `IOnboardingService` & `RecruitmentService`, `OnboardingService`)**:
  + **Quản lý Tin tuyển dụng (`JobPosting`)**: Tạo/sửa tin tuyển dụng kèm tự sinh `Slug` chuẩn SEO (loại bỏ dấu tiếng Việt `SlugHelper.GenerateSlug`, chống trùng lặp slug), định biên cần tuyển (`Vacancies`), khoảng lương, mô tả, yêu cầu, hạn nộp hồ sơ và tự động đồng bộ thẻ `SeoMeta` (MetaTitle, MetaDescription, MetaKeywords, OgImage, CanonicalUrl).
  + **Kanban Pipeline Ứng viên 6 Cột Trực Quan (`/TuyenDung/Kanban`)**:
    - 6 cột trạng thái tuyển dụng chuẩn: `Applied` (Ứng tuyển mới) -> `Screening` (Sàng lọc) -> `Interview` (Phỏng vấn) -> `Offered` (Đề xuất nhận việc) -> `Hired` (Trúng tuyển) -> `Rejected` (Từ chối).
    - Hỗ trợ kéo - thả thẻ ứng viên giữa các cột với cập nhật tức thời qua AJAX API `/TuyenDung/CapNhatTrangThai`.
    - Bộ lọc nhanh theo Tin tuyển dụng, Từ khóa tìm kiếm và Điểm đánh giá (1-100 điểm).
    - **Drawer Xem Hồ Sơ & CV Trực Tuyến (`_CandidateDrawerPartial`)**: Cho phép HR xem thông tin ứng viên, đọc thư giới thiệu, xem trực tiếp file PDF/DOCX bằng iframe preview mà không cần tải file về máy.
  + **Lên Lịch Phỏng Vấn & Đánh Giá (`InterviewSchedule`)**:
    - Tạo lịch phỏng vấn đa vòng (Vòng 1, 2, 3), hình thức Online (Google Meet/Teams) hoặc Trực tiếp tại văn phòng.
    - Phân công người phỏng vấn (`InterviewerUserId`) kèm tích hợp bắn thông báo thời gian thực qua **SignalR** (`_notificationService.SendToUserAsync`).
    - Chấm điểm chuyên môn thang điểm 100, kết luận kết quả (`Passed`, `Failed`, `Consider`, `Pending`) và lưu nhận xét chi tiết, tự động cập nhật ngược lại điểm số ứng viên trên thẻ Kanban.
  + **Quy Trình 1-Click Chuyển Ứng Viên Thành Nhân Viên (Golden Path — `/TuyenDung/TiepNhanNhanSu/{candidateId}`)**:
    - Khi ứng viên ở cột `Offered` hoặc `Hired`, bấm nút "Tiếp nhận nhân sự" -> Hệ thống tự động tạo hồ sơ nhân sự mới (`Employee`) với mã nhân viên chuẩn (`NV-yyMMdd-xxx`), trạng thái Thử việc (`Probation`), thông tin liên hệ và phòng ban/vị trí.
    - Tự động tạo bản thảo Hợp đồng thử việc (`Contract`) với thời hạn thử việc (mặc định 2 tháng theo luật lao động VN) và mức lương thử việc quy định.
    - Cập nhật trạng thái ứng viên thành `Hired` kèm ghi chú liên kết mã nhân sự.
    - **Tự động kích hoạt Quy trình Hội nhập (Onboarding Checklist)** gồm 4 đầu việc chuẩn mẫu:
      1. IT: Cấp tài khoản email và quyền truy cập hệ thống.
      2. IT: Bàn giao máy tính xách tay và thiết bị làm việc.
      3. HR: Đào tạo nội quy doanh nghiệp và văn hóa công ty.
      4. DepartmentHead: Giao việc tuần đầu và chỉ định Mentor hướng dẫn.
  + **Quy Trình Onboarding & Offboarding (`/TuyenDung/Onboarding`)**:
    - Theo dõi tiến độ hội nhập nhân viên mới và thủ tục thôi việc / thu hồi tài sản (`Offboarding`).
    - Checkbox cập nhật tiến độ tức thời qua AJAX (`/TuyenDung/CapNhatTienDoTask`), tính toán thanh tiến độ phần trăm % tự động (0% -> 25% -> 50% -> 75% -> 100%) và tự động đổi trạng thái Checklist sang `Completed` khi hoàn tất.

### 2. Cổng Tuyển Dụng Công Khai (`/tuyen-dung`) & Theme Employer Branding
- **Trang chủ tuyển dụng `/tuyen-dung`**:
  + Hero Section phong cách Nordic Indigo sang trọng, thông điệp văn hóa thu hút nhân tài, các chỉ số thống kê định biên tuyển dụng.
  + 4 khối phúc lợi và văn hóa nổi bật: Đãi ngộ cạnh tranh, Lộ trình thăng tiến rõ ràng, Đào tạo liên tục, Môi trường hứng khởi.
  + Bộ lọc tìm kiếm đa tiêu chí: Từ khóa, Phòng ban, Chi nhánh và danh sách thẻ vị trí tuyển dụng đang mở.
- **Trang chi tiết công việc `/tuyen-dung/{slug}`**:
  + Trình bày cấu trúc rõ ràng: Mô tả công việc, Yêu cầu ứng viên, Quyền lợi, Mức lương minh bạch, Hạn nộp.
  + **Form ứng tuyển trực tuyến trực tiếp**: Nhập họ tên, email, số điện thoại, thư giới thiệu và đính kèm CV.
  + **Bảo Mật File Upload & Chống Giả Mạo**:
    - `FileUploadValidator.ValidateCvFile`: Kiểm tra **Magic Bytes** chuẩn xác của file tải lên (chỉ chấp nhận PDF với `%PDF-` [0x25, 0x50, 0x44, 0x46] và Word DOCX với `PK\x03\x04` [0x50, 0x4B, 0x03, 0x04]). Chặn đứng 100% các file thực thi `.exe`, `.bat` đổi đuôi thành `.pdf`!
    - Giới hạn dung lượng tập tin <= 10MB.
    - **Anti-spam Protection**: Honeypot field ẩn chống bot tự động + Rate Limiting theo địa chỉ IP (tối đa 5 lượt nộp / 10 phút lưu trên `IMemoryCache`).
    - Nộp hồ sơ thành công -> Tự động sinh `Candidate` mã `UV-yyMM-xxxx` tại cột `Applied` trên ATS Kanban nội bộ.

### 3. Chuẩn Hóa Technical SEO Toàn Diện & Core Web Vitals
- **TagHelper `<erp-seo-head>`**:
  + Đọc dữ liệu từ `SeoMeta` và `SiteSettings` để sinh chuẩn xác Open Graph (`og:site_name`, `og:title`, `og:description`, `og:url`, `og:type`, `og:image`), Twitter Cards (`twitter:card`, `twitter:title`, `twitter:description`, `twitter:image`), thẻ canonical URL tuyệt đối và meta robots `index, follow`.
- **Schema.org Structured Data (`application/ld+json`)**:
  + Tự động sinh cấu trúc **Google for Jobs (`JobPosting` schema)** cho mọi trang chi tiết `/tuyen-dung/{slug}` (title, description, datePosted, validThrough, employmentType, hiringOrganization, jobLocation, baseSalary VND/MONTH).
  + Tự động sinh `NewsArticle` schema cho trang bài viết `/bai-viet/{slug}`.
  + Tự động sinh `Organization` schema cho trang chủ.
- **Dynamic Sitemap (`/sitemap.xml`)**:
  + Tự động quét và sinh XML sitemap hợp lệ theo chuẩn `sitemaps.org/schemas/sitemap/0.9`: Trang chủ, Giới thiệu, Tin tức, Tuyển dụng, Liên hệ, toàn bộ bài viết CMS đã duyệt (`/bai-viet/{slug}`) và các tin tuyển dụng đang mở (`/tuyen-dung/{slug}`).
  + Cấu hình tần suất cập nhật (`changefreq`) và độ ưu tiên (`priority`). Lưu đệm trên bộ nhớ (`IMemoryCache`) với thời gian hết hạn 30 phút.
- **Dynamic Robots.txt (`/robots.txt`)**:
  + Chỉ định liên kết `Sitemap: https://.../sitemap.xml`.
  + Cho phép bot tìm kiếm quét các trang công khai (`/`, `/tuyen-dung`, `/bai-viet/`, `/Public/`).
  + Chặn triệt để bot thu thập dữ liệu quản trị nội bộ: `Disallow: /admin`, `/NhanSu`, `/BangLuong`, `/ChamCong`, `/DonTu`, `/HopDong`, `/CongNhanVien`, `/TuyenDung/`, `/Auth/`, `/TaiKhoan`, `/PhanQuyen`.

### 4. Kiểm Thử Tích Hợp Tự Động (Integration Tests)
- Xây dựng 11 bài kiểm thử tại `tests/ERP_HRM_CMS.Tests/Phase7RecruitmentAndSeoTests.cs`:
  1. `Sitemap_Returns_Valid_Xml_With_Careers_And_Pages`: Trả về 200 OK, MIME type XML, chứa thẻ `<urlset>` và link tuyển dụng.
  2. `RobotsTxt_Allows_Public_And_Disallows_Admin_Routes`: Trả về 200 OK, chứa `Sitemap:` và chặn các đường dẫn admin.
  3. `Public_Careers_Index_Returns_Success_And_Lists_Open_Jobs`: Trang chủ tuyển dụng hoạt động mượt mà.
  4. `Public_Job_Detail_Contains_GoogleForJobs_LdJson_And_OpenGraph`: Kiểm tra chứa schema Google for Jobs và Open Graph.
  5. `ValidateCvFile_Accepts_Valid_Pdf_With_Magic_Bytes`: Chấp nhận file PDF thật chứa header `%PDF-`.
  6. `ValidateCvFile_Rejects_Spoofed_Pdf_With_Invalid_Magic_Bytes`: Từ chối file giả mạo extension.
  7. `ValidateCvFile_Accepts_Valid_Docx_With_Zip_Magic_Bytes`: Chấp nhận file Word DOCX thật.
  8. `ValidateCvFile_Rejects_Spoofed_Docx_With_Invalid_Magic_Bytes`: Từ chối file DOCX giả mạo.
  9. `ValidateCvFile_Rejects_Unsupported_Extensions`: Từ chối file định dạng không hỗ trợ.
  10. `Candidate_Can_Be_Scheduled_For_Interview_And_Evaluated`: Lên lịch phỏng vấn và chấm điểm thành công.
  11. `GoldenPath_ConvertCandidateToEmployee_Creates_Employee_Contract_And_Onboarding_Checklist`: 1-Click chuyển ứng viên thành nhân viên, tạo hợp đồng thử việc, tạo Onboarding Checklist 4 task và kiểm tra cập nhật tiến độ % (0% -> 25% -> 50% -> 75% -> 100%).
- **Kết quả Build & Test Toàn Hệ Thống**:
  + `dotnet build ERP_HRM_CMS.sln`: **Thành công — 0 Error(s)**.
  + `dotnet test tests/ERP_HRM_CMS.Tests`: **239 / 239 bài kiểm thử XANH 100% (0 Failed, 0 Skipped)**.

## PHIÊN 8: Đánh Giá Hiệu Suất KPI / 9-Box Talent, Đào Tạo Nội Bộ (LMS) & Khép Kín Chuỗi Tính Lương C&B (ĐÃ HOÀN TẤT)

### 1. Phân Hệ Đánh Giá Hiệu Suất KPI & Ma Trận Nhân Tài 9-Box (`/DanhGiaKpi` — Clean Architecture)
- **Tầng Ứng dụng & Dịch vụ (`IPerformanceReviewService` & `PerformanceReviewService`)**:
  + **Thiết lập Kỳ & Tiêu chí KPI**: Tạo phiếu đánh giá theo Tháng/Năm, giao bộ tiêu chí `KpiCriteria` theo trọng số. Tích hợp `ValidateCriteriaWeights`: kiểm tra nghiêm ngặt **tổng trọng số các tiêu chí bắt buộc phải bằng 100%** (chặn tạo nếu tổng trọng số != 100%).
  + **Quy trình Đánh giá 3 Cấp Khép Kín**:
    1. *Bước 1 (Nhân viên tự chấm)*: Tự chấm điểm từng tiêu chí và nhập minh chứng, ghi chú thành tích cá nhân đạt được (hỗ trợ thực hiện ngay tại cổng tự phục vụ ESS).
    2. *Bước 2 (Quản lý trực tiếp chấm)*: Quản lý trực tiếp đối soát, chấm điểm thực tế từng tiêu chí, tự động tính điểm tổng hợp theo trọng số (`TotalScore = Sum(Weight * Score) / 100`), đồng thời chấm **Điểm Tiềm năng (Potential Score 0-100)** và ghi nhận xét. Chuyển trạng thái sang `Submitted`.
    3. *Bước 3 (Ban Giám Đốc / HR Phê duyệt)*: Cấp quyền chốt Xếp loại sau cùng (`Grade`: `A+`, `A`, `B`, `C`, `D`), phê duyệt phiếu KPI (`Approved`) và phát thông báo Real-time qua **SignalR** tới nhân viên.
  + **Biểu Đồ Radar Năng Lực (Chart.js Radar Competence)**:
    - Trực quan hóa đối soát trực tiếp giữa *Điểm Nhân viên tự chấm* và *Điểm Quản lý đánh giá* trên đồ thị mạng nhện đa chiều.
  + **Ma Trận Nhân Tài 9-Box (Talent Matrix — `/DanhGiaKpi/NineBox`)**:
    - Mô hình 3x3 đối chiếu giữa **Hiệu Suất (Performance)** và **Tiềm Năng (Potential)**:
      * *Hàng 1 (Tiềm năng Cao)*: Rough Diamond (Enigma), Growth Talent, Star (Ngôi sao / Kế thừa).
      * *Hàng 2 (Tiềm năng Trung bình)*: Dilemma, Core Employee (Nòng cốt), High Performer (Chuyên gia).
      * *Hàng 3 (Tiềm năng Thấp)*: Under Performer (Rủi ro / Cảnh báo), Steady Performer, Specialist (Lành nghề).
    - Phân bổ nhân sự trực quan bằng thẻ nhân sự, avatar, điểm P/Pot và liên kết nhanh đến phiếu đánh giá chi tiết.

### 2. Phân Hệ Đào Tạo & Phát Triển Năng Lực Nội Bộ LMS (`/DaoTao` — Clean Architecture)
- **Tầng Ứng dụng & Dịch vụ (`ITrainingService` & `TrainingService`)**:
  + **Quản lý Danh mục Khóa học & Chuyên đề (`Training`)**: CRUD khóa đào tạo, mã khóa học, giảng viên, phòng học/online, thời gian, ngân sách dự toán, trạng thái (`Planned`, `InProgress`, `Completed`, `Cancelled`).
  + **Gán Học Viên Tham Gia (`TrainingParticipant`)**: Gán theo từng nhân sự hoặc gán hàng loạt theo toàn bộ phòng ban.
  + **Sát Hạch & Cơ Chế Nâng Bậc Kỹ Năng / Cấp Chứng Chỉ Tự Động**:
    - Chấm điểm bài thi/sát hạch thang điểm 100 và ghi nhận kết quả (`Passed`, `Failed`, `Attended`).
    - **Smart Automation**: Khi học viên đạt kết quả `Passed` (Điểm >= 60):
      1. Tự động thêm hoặc nâng cấp bậc kỹ năng (`Level`: `Beginner` -> `Intermediate` -> `Advanced` -> `Expert`) trong bảng `EmployeeSkills` của hồ sơ nhân viên.
      2. Tự động phát hành và ghi nhận Chứng nhận hoàn thành nội bộ vào bảng `EmployeeCertificates` kèm ngày cấp, hạn 2 năm và đơn vị cấp bằng.
    - Ngược lại, nếu học viên trượt (`Failed`, điểm < 60), hệ thống tuyệt đối không cấp chứng chỉ hay nâng bậc kỹ năng.

### 3. Nâng Cấp & Khép Kín Phân Hệ Tiền Lương C&B (`PayrollService` & `BangLuongController`)
- **Tự Động Hóa Tích Hợp Đa Phân Hệ Vào Bảng Lương**:
  + **Kiểm tra Khóa kỳ chấm công (`AttendanceLock`)**: Cảnh báo tức thì trên giao diện và log hệ thống nếu kỳ công chưa khóa sổ trước khi tính lương.
  + **Đồng bộ Thưởng Hiệu Suất KPI**: Tự động đọc kết quả đánh giá KPI `Approved` của nhân viên trong kỳ để tính thưởng theo quy định:
    - Loại `A+`: +20% Lương cơ bản.
    - Loại `A`: +10% Lương cơ bản.
    - Loại `B`: +5% Lương cơ bản.
    - Loại `C`, `D`: 0%.
  + **Đồng bộ Quyết Định Khen Thưởng & Kỷ Luật (`HrDecision`)**: Tự động tính các khoản tiền thưởng (`Reward`) vào thu nhập chịu thuế và trừ các khoản tiền phạt kỷ luật (`Discipline`) trước khi ra Thực lĩnh (Net).
- **Quy Trình Quản Trị Bảng Lương Chuẩn Doanh Nghiệp**:
  + **So Sánh Biến Động Kỳ Trước (`GetPayrollComparisonAsync`)**: Thanh công cụ so sánh nhanh MoM (Month-over-Month) giữa kỳ hiện tại và kỳ trước: Biến động tổng quỹ Net (số tiền và % tăng giảm), Biến động số lượng nhân sự.
  + **Khóa Kỳ Lương (`KhoaBangLuong`)**: Chuyển trạng thái sang `Locked`, khóa cứng không cho tính lại tự động, ghi nhận nhật ký kiểm toán `AuditLog` và thời điểm khóa `LockedAt`.
  + **Chi Trả & Phát Hành Hàng Loạt (`ChuyenChiTra`)**: Chuyển trạng thái sang `Paid`, tự động kích hoạt phát hành phiếu lương và gửi thông báo Real-time qua **SignalR** tới toàn bộ nhân sự có trong kỳ lương.
- **Xuất Báo Cáo Tài Chính & Ủy Nhiệm Chi (Bank Transfer UNC Excel)**:
  + Xuất file Excel Bảng thanh toán lương tổng hợp toàn công ty (`GeneratePayrollExcel`).
  + Xuất file Excel Bảng kê trích nộp Bảo hiểm phần Người sử dụng lao động 21.5% (`GenerateEmployerInsuranceExcel`).
  + **Xuất Ủy Nhiệm Chi Chuyển Khoản Ngân Hàng (`XuatUyNhiemChi`)**:
    - Tự động giải mã bảo mật số tài khoản ngân hàng (`enc:v1:...` qua Data Protection).
    - **Bảo mật phân quyền nghiêm ngặt**: Chỉ tài khoản có vai trò `SUPERADMIN` hoặc `ADMIN_BANGLUONG` mới được phép tải file. Nhân viên bình thường khi gọi Action này lập tức bị chặn bằng `403 Forbidden` và tự động ghi sự kiện vi phạm bảo mật vào `AuditLogs` với action `SECURITY_VIOLATION`.
    - Mọi thao tác xuất file thành công đều được ghi nhận chi tiết vào `AuditLogs` với action `EXPORT_BANK_TRANSFER`.

### 4. Kiểm Thử Tự Động Toàn Diện (Unit & Integration Tests)
- Xây dựng 8 bài kiểm thử chuyên sâu tại `tests/ERP_HRM_CMS.Tests/Phase8PerformanceLmsAndPayrollTests.cs`:
  1. `KpiCriteriaWeightValidation_RejectsSumNot100_Accepts100`: Kiểm tra bộ kiểm tra trọng số KPI từ chối tổng != 100% và chấp nhận tổng == 100%.
  2. `KpiThreeStepEvaluation_LifecycleAndNineBoxCategory_Succeeds`: Kiểm tra vòng đời 3 bước đánh giá KPI, tính điểm trọng số, duyệt A+ và tự động xếp loại 9-Box vào nhóm "Star".
  3. `LmsParticipantGrading_PassGrantsSkillAndCertificate_FailDoesNot`: Kiểm tra học viên Đạt (>= 60) tự động nâng cấp kỹ năng và nhận chứng chỉ; học viên Trượt (< 60) không được cấp chứng chỉ.
  4. `PayrollCalculator_IntegratesKpiBonusAndDecisions_CorrectNetSalary`: Kiểm tra bộ tính lương tích hợp Thưởng KPI A+ (+20%), Khen thưởng đột xuất và Phạt kỷ luật.
  5. `PayrollPeriod_LockAndPayTransitions_UpdatesStatusAndAudit`: Kiểm tra chuyển trạng thái kỳ lương sang Locked -> Paid và ghi nhận AuditLog.
  6. `BankTransferUNC_UnauthorizedUser_Returns403ForbiddenAndLogsAudit`: Kiểm tra tài khoản không có quyền xuất UNC bị chặn 403 Forbidden và ghi log vi phạm bảo mật.
  7. `BankTransferUNC_AuthorizedAdmin_ReturnsExcelFileAndLogsAudit`: Kiểm tra tài khoản ADMIN_BANGLUONG tải file UNC Excel thành công và ghi log kiểm toán.
  8. `PayrollComparison_CalculatesMonthOverMonthMetrics_Correctly`: Kiểm tra tính toán các chỉ số so sánh biến động lương và nhân sự so với kỳ trước.
- **Kết quả Build & Test Toàn Hệ Thống**:
  + `dotnet build ERP_HRM_CMS.sln`: **Thành công — 0 Error(s)**.
  + `dotnet test tests/ERP_HRM_CMS.Tests`: **247 / 247 bài kiểm thử XANH 100% (0 Failed, 0 Skipped)**.

## Nợ kỹ thuật đã ghi nhận

Chi tiết trong `docs/ANALYSIS.md`, mục 6–9.
