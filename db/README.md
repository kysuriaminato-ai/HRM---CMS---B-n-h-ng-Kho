# Database Guide — ERP HRM CMS

## Tổng quan

Hệ thống sử dụng **SQL Server 2019+** với **EF Core 8 Code-First** kết hợp Clean Architecture 4 tầng.  
Toàn bộ schema được đồng bộ qua Migration và cung cấp bộ SQL Script Idempotent chạy trực tiếp độc lập trên **SQL Server Management Studio (SSMS)**.

---

## 1. Tạo Database & Thứ tự chạy Script trên SSMS

### Cách 1: Dùng SQL Scripts thủ công trên SSMS (Khuyên dùng cho DBA)

1. Mở **SSMS** (SQL Server Management Studio), kết nối tới `localhost` (Windows Authentication).
2. Tạo database mới (nếu chưa có):
   ```sql
   CREATE DATABASE ERP_HRM_CMS;
   GO
   ```
3. Chạy lần lượt các script trong thư mục `db/scripts/` theo đúng thứ tự phiên bản:
   - **`V4_01_NewTables_And_AuditColumns.sql`**: Schema nền tảng 53 bảng + 4 cột audit (`IsDeleted`, `UpdatedAt`, `UpdatedBy`, `CreatedBy`).
   - **`V4_02_SeedData.sql`**: Dữ liệu mẫu ban đầu (Công ty, Chi nhánh, Phòng ban, Chức vụ, Sản phẩm...).
   - **`V5_01_ProfileTables.sql`**: 5 bảng hồ sơ mở rộng (Người thân, Kỹ năng, Chứng chỉ, Tài liệu, Lịch sử biến động).
   - **`V6_00_Security_And_Architecture.sql`**: Bảo mật P0 (`Users.MustChangePassword`, `LeaveRequests.CreatedByUserId`), vai trò `BRANCH_MANAGER`, `DEPARTMENT_HEAD`.
   - **`V6_01_EnterpriseHRM_Schema_And_Indexes.sql`**: Schema Enterprise HRM khép kín vòng đời nhân sự (9 bảng mới + cột `Slug` chuẩn SEO + Check Constraints + Covering Indexes + Extended Properties).
   - **`V6_02_EnterpriseHRM_SeedData.sql`**: Dữ liệu mẫu Enterprise HRM phong phú (Quỹ phép năm 2026, 6 tin tuyển dụng chuẩn SEO, 18 ứng viên đủ 6 cột Kanban, lịch phỏng vấn, danh mục & cấp phát tài sản, quyết định nhân sự, khóa chấm công, đơn giải trình, checklist Onboarding/Offboarding).

> **Lưu ý quan trọng**: Mọi script trong `db/scripts/` đều được viết theo nguyên tắc **Idempotent** (`IF OBJECT_ID(...) IS NULL`, `IF NOT EXISTS...`), bọc trong giao dịch `SET XACT_ABORT ON`, đảm bảo chạy lại nhiều lần an toàn tuyệt đối không lỗi, không nhân bản dữ liệu.

---

### Cách 2: Dùng EF Core Migration CLI

```bash
# Áp dụng toàn bộ migration lên database
dotnet ef database update --project src/ERP.Infrastructure/ERP.Infrastructure.csproj --startup-project ERP_HRM_CMS.csproj
```

---

## 2. Connection String

Cấu hình chuỗi kết nối trong `appsettings.json` hoặc User Secrets:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ERP_HRM_CMS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

---

## 3. Danh mục Bảng Schema Doanh nghiệp (67 Bảng)

| Phân hệ | Số lượng | Các bảng tiêu biểu |
|---|---|---|
| **System** | 12 | `Users`, `Roles`, `Permissions`, `Modules`, `UserRoles`, `RolePermissions`, `UserPermissions`, `AuditLogs`, `Notifications`, `NotificationReads`, `LoginAttempts`, `SiteSettings` |
| **HRM Core & Profile** | 15 | `Companies`, `Branches`, `Departments`, `Positions`, `Employees`, `EmployeeDetails`, `Contracts`, `Shifts`, `AttendanceDailies`, `EmployeeRelatives`, `EmployeeSkills`, `EmployeeCertificates`, `EmployeeDocuments`, `EmployeeChangeHistories`, `SeedHistory` |
| **HRM Payroll & Leave** | 7 | `LeaveTypes`, `LeaveRequests`, `EmployeeLeaveBalances`, `AttendanceLocks`, `AttendanceAdjustmentRequests`, `PayrollPeriods`, `SalarySlips`, `PayrollAllowanceTypes`, `EmployeeDependents` |
| **HRM Recruitment, Training, KPI** | 9 | `JobPostings`, `Candidates`, `InterviewSchedules`, `Trainings`, `TrainingParticipants`, `KpiReviews`, `KpiCriteria`, `HrDecisions`, `OnboardingChecklists`, `OnboardingTasks` |
| **HRM Asset Management** | 2 | `CompanyAssets`, `EmployeeAssetAssignments` |
| **CMS** | 7 | `Contents`, `Categories`, `Menus`, `Banners`, `Tags`, `ContentTags`, `MediaFiles`, `SeoMetas` |
| **SCM & Bán hàng** | 16 | `Products`, `Inventories`, `InventoryTransactions`, `Orders`, `OrderDetails`, `CartItems`, `Suppliers`, `PurchaseOrders`, `PurchaseOrderDetails`, `DebtRecords`, `DebtPayments`, `StocktakingSheets`, `StocktakingDetails`, `Customers`, `Units`, `Warehouses`, `Invoices`, `Payments` |

---

## 4. Quy ước Kỹ thuật Database

1. **Khóa chính**: `int IDENTITY(1,1)` cho bảng nghiệp vụ, `bigint IDENTITY(1,1)` cho bảng phát sinh lớn (`AttendanceDailies`, `AuditLogs`, `Notifications`).
2. **Khóa ngoại & Xóa dữ liệu**: Cấu hình `ON DELETE RESTRICT` (hoặc `NO ACTION` trong SQL Server) cho các liên kết nhân sự/lịch sử để tránh lỗi chu trình hoặc đa đường xóa lan truyền (*multiple cascade paths*).
3. **Audit Columns & Soft Delete**: Các bảng `IAuditable` có 4 cột: `IsDeleted` (bit), `UpdatedAt` (datetime2), `UpdatedBy` (nvarchar 100), `CreatedBy` (nvarchar 100). EF Core tự động áp dụng Global Query Filter `!e.IsDeleted`.
4. **Tiền tệ & Số thập phân**: Mọi trường tiền tệ quy định rõ `decimal(18,2)`. Tỷ lệ, hệ số, ngày phép quy định `decimal(5,2)`. Giờ làm việc quy định `decimal(4,2)`.
5. **Múi giờ**: Database lưu UTC (`SYSUTCDATETIME()`), ứng dụng chuyển đổi sang giờ Việt Nam qua `VietnamTimeConverter` cho các cột thời điểm kết thúc bằng `...At`, `PaymentDate`, `CheckDate`, `ScheduledTime`.
6. **Mã hóa dữ liệu nhạy cảm**: Số CCCD (`IdentityCard`) và Số tài khoản ngân hàng (`BankAccountNumber`) được mã hóa qua ASP.NET Core Data Protection và che mờ (`SensitiveDataMasker`) khi ghi vào `AuditLogs`.
