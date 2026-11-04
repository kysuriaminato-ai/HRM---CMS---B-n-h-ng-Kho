# Tiến độ dự án

Cập nhật lần cuối: 2026-11-04 (phiên 5 — đang tiến hành)

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

## Nợ kỹ thuật đã ghi nhận

Chi tiết trong `docs/ANALYSIS.md`, mục 6–9.
