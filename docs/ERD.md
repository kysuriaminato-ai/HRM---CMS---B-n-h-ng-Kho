# ERD — Sơ đồ quan hệ thực thể

> Cập nhật: Phiên 4 (2026-11-04)
> Quy ước: int PK (Identity) cho bảng thường, long PK cho bảng dữ liệu lớn.
> Tất cả bảng nghiệp vụ có: CreatedAt, UpdatedAt, IsDeleted, CreatedBy, UpdatedBy.

---

## 1. Nhóm Hệ thống (System)

```mermaid
erDiagram
    Users {
        int Id PK
        string Username UK
        string PasswordHash
        string PasswordSalt
        bool IsActive
        bool IsSuperAdmin
        string SecurityStamp
        datetime LastLoginAt
        int EmployeeId FK "nullable"
    }
    Roles {
        int Id PK
        string Code UK
        string Name
        string Description
        bool IsSystem
        bool IsActive
    }
    Modules {
        int Id PK
        string Code UK
        string Name
        string Icon
        int OrderIndex
        bool IsActive
    }
    Permissions {
        int Id PK
        string Code UK
        string Name
        int ModuleId FK
        int OrderIndex
    }
    UserRoles {
        int UserId PK_FK
        int RoleId PK_FK
        datetime AssignedAt
    }
    RolePermissions {
        int RoleId PK_FK
        int PermissionId PK_FK
    }
    UserPermissions {
        int UserId PK_FK
        int PermissionId PK_FK
        bool IsGranted
    }
    AuditLogs {
        long Id PK
        int UserId
        string Username
        string Action
        string EntityName
        string RecordId
        string OldValues "JSON"
        string NewValues "JSON"
        datetime CreatedAt
    }
    Notifications {
        long Id PK
        int ReceiverUserId
        string TargetRole
        string Title
        string Content
        string Type
        bool IsRead
    }
    NotificationReads {
        long NotificationId PK_FK
        int UserId PK_FK
        datetime ReadAt
    }
    SiteSettings {
        int Id PK
        string SettingKey UK
        string SettingValue
        string SettingGroup
    }
    LoginAttempts {
        int Id PK
        string Username
        string IpAddress
        int FailedCount
    }

    Users ||--o{ UserRoles : ""
    Roles ||--o{ UserRoles : ""
    Users ||--o{ UserPermissions : ""
    Permissions ||--o{ UserPermissions : ""
    Roles ||--o{ RolePermissions : ""
    Permissions ||--o{ RolePermissions : ""
    Modules ||--o{ Permissions : ""
    Notifications ||--o{ NotificationReads : ""
```

---

## 2. Nhóm HRM (Nhân sự)

```mermaid
erDiagram
    Companies {
        int Id PK
        string Code UK
        string Name
        string TaxCode
        string Phone
        bool IsActive
    }
    Branches {
        int Id PK
        string Code UK
        string Name
        int CompanyId FK
        bool IsActive
    }
    Departments {
        int Id PK
        string Code UK
        string Name
        int BranchId FK
        int ParentId FK "nullable, self-ref"
        bool IsActive
    }
    Positions {
        int Id PK
        string Code UK
        string Name
        decimal BaseSalary "18,2"
        decimal AllowanceRate "5,2"
        bool IsActive
    }
    Employees {
        int Id PK
        string EmployeeCode UK
        string FullName
        datetime DateOfBirth
        string Gender
        string Email UK
        string Phone
        string IdentityCard "encrypted"
        string Status
        datetime HireDate
        int DepartmentId FK
        int PositionId FK
    }
    EmployeeDetails {
        int EmployeeId PK_FK
        string BankAccountNumber "encrypted"
        string BankName
        string TaxIdentificationNumber
        string SocialInsuranceNumber
        string EducationLevel
    }
    Contracts {
        int Id PK
        string ContractNumber UK
        string ContractType
        datetime StartDate
        datetime EndDate
        decimal BasicSalary "18,2"
        string Status
        int EmployeeId FK
    }
    Shifts {
        int Id PK
        string Code UK
        string Name
        time StartTime
        time EndTime
        decimal WorkHours "4,2"
    }
    AttendanceDailies {
        long Id PK
        datetime WorkDate
        time FirstInTime
        time LastOutTime
        int LateMinutes
        decimal WorkHourCount "4,2"
        decimal OvertimeHours "4,2"
        string AttendanceStatus
        int EmployeeId FK
        int ShiftId FK
    }
    LeaveTypes {
        int Id PK
        string Code UK
        string Name
        int MaxDaysPerYear
        bool IsPaid
        bool IsActive
    }
    LeaveRequests {
        int Id PK
        string RequestCode UK
        int LeaveTypeId FK "NEW"
        datetime FromDate
        datetime ToDate
        decimal TotalDays "5,2"
        string Reason
        string Status
        int EmployeeId FK
    }
    PayrollPeriods {
        int Id PK
        string Name
        int Month
        int Year
        int StandardWorkDays
        string Status
        decimal TotalGross "18,2"
        decimal TotalNet "18,2"
    }
    SalarySlips {
        int Id PK
        decimal BasicSalary "18,2"
        decimal ActualWorkDays "5,2"
        decimal NetSalary "18,2"
        string Status
        int PayrollPeriodId FK
        int EmployeeId FK
    }
    EmployeeDependents {
        int Id PK
        string FullName
        string Relationship
        datetime DateOfBirth
        int EmployeeId FK
    }
    PayrollAllowanceTypes {
        int Id PK
        string Code UK
        string Name
        decimal DefaultAmount "18,2"
        bool IsTaxExempt
    }
    JobPostings {
        int Id PK
        string Code UK
        string Title
        int DepartmentId FK
        int PositionId FK
        int Vacancies
        string Description
        string Requirements
        string Status
        datetime OpenDate
        datetime CloseDate
    }
    Candidates {
        int Id PK
        string Code UK
        string FullName
        string Email
        string Phone
        string ResumeUrl
        string Status
        int JobPostingId FK
    }
    Trainings {
        int Id PK
        string Code UK
        string Title
        string TrainerName
        string Location
        datetime StartDate
        datetime EndDate
        decimal Budget "18,2"
        string Status
    }
    TrainingParticipants {
        int TrainingId PK_FK
        int EmployeeId PK_FK
        string Result
        decimal Score "5,2"
    }
    KpiReviews {
        int Id PK
        int EmployeeId FK
        int ReviewerId FK
        int PeriodMonth
        int PeriodYear
        decimal TotalScore "5,2"
        string Grade
        string Status
    }
    KpiCriteria {
        int Id PK
        int KpiReviewId FK
        string CriteriaName
        decimal Weight "5,2"
        decimal Score "5,2"
        string Comment
    }

    Companies ||--o{ Branches : ""
    Branches ||--o{ Departments : ""
    Departments ||--o{ Employees : ""
    Positions ||--o{ Employees : ""
    Employees ||--|| EmployeeDetails : ""
    Employees ||--o{ Contracts : ""
    Employees ||--o{ AttendanceDailies : ""
    Employees ||--o{ LeaveRequests : ""
    Employees ||--o{ SalarySlips : ""
    Employees ||--o{ EmployeeDependents : ""
    Employees ||--o{ KpiReviews : "được đánh giá"
    LeaveTypes ||--o{ LeaveRequests : ""
    PayrollPeriods ||--o{ SalarySlips : ""
    Shifts ||--o{ AttendanceDailies : ""
    Departments ||--o{ JobPostings : ""
    Positions ||--o{ JobPostings : ""
    JobPostings ||--o{ Candidates : ""
    Trainings ||--o{ TrainingParticipants : ""
    Employees ||--o{ TrainingParticipants : ""
    KpiReviews ||--o{ KpiCriteria : ""
```

---

## 3. Nhóm CMS (Nội dung)

```mermaid
erDiagram
    Contents {
        int Id PK
        string Title
        string Slug UK
        string Summary
        string BodyHtml
        string ThumbnailUrl
        string Status
        int ViewCount
        int AuthorId FK
    }
    Tags {
        int Id PK
        string Name UK
        string Slug UK
        int UsageCount
    }
    ContentTags {
        int ContentId PK_FK
        int TagId PK_FK
    }
    Media {
        long Id PK
        string FileName
        string OriginalName
        string MimeType
        long FileSize
        string Url
        string ThumbnailUrl
        string AltText
        string Folder
        int UploadedBy FK
    }
    SeoMeta {
        int Id PK
        string EntityType
        int EntityId
        string MetaTitle
        string MetaDescription
        string MetaKeywords
        string CanonicalUrl
        string OgImage
    }
    Menus {
        int Id PK
        string Title
        string LinkType
        string CustomUrl
        string Position
        int OrderIndex
        int ParentId FK "nullable"
    }
    Banners {
        int Id PK
        string Title
        string ImageUrl
        string LinkUrl
        string Position
        int OrderIndex
        bool IsActive
    }
    SiteLayouts {
        int Id PK
        string LayoutName
        string BlockKey
        string BlockTitle
        int OrderIndex
    }

    Contents ||--o{ ContentTags : ""
    Tags ||--o{ ContentTags : ""
    Users ||--o{ Contents : "AuthorId"
    Users ||--o{ Media : "UploadedBy"
```

---

## 4. Nhóm Kho & Bán hàng (SCM)

```mermaid
erDiagram
    Categories {
        int Id PK
        string Code UK
        string Name
        string Slug UK
        int ParentId FK "nullable"
        int OrderIndex
        bool IsActive
    }
    Units {
        int Id PK
        string Code UK
        string Name
        bool IsActive
    }
    Products {
        int Id PK
        string SKU UK
        string Barcode
        string Name
        string Slug UK
        decimal CostPrice "18,2"
        decimal SalePrice "18,2"
        string ImageUrl
        bool IsActive
        int CategoryId FK
        int UnitId FK "NEW"
    }
    Customers {
        int Id PK
        string Code UK
        string FullName
        string Phone UK
        string Email
        string Address
        string TaxCode
        decimal TotalDebt "18,2"
        bool IsActive
    }
    Warehouses {
        int Id PK
        string Code UK
        string Name
        string Address
        int BranchId FK
        bool IsActive
    }
    Inventories {
        int Id PK
        int ProductId FK
        int WarehouseId FK "NEW thay BranchId"
        int StockQuantity
        int MinStockAlert
        int MaxStockLimit
    }
    InventoryTransactions {
        long Id PK
        string TransactionCode UK
        string TransactionType "IN/OUT/TRANSFER"
        int Quantity
        decimal UnitPrice "18,2"
        int SourceWarehouseId FK "nullable"
        int DestWarehouseId FK "nullable"
        int ProductId FK
        int CreatedBy FK
    }
    Suppliers {
        int Id PK
        string Code UK
        string Name
        string TaxCode
        string Phone
        decimal TotalDebt "18,2"
        bool IsActive
    }
    PurchaseOrders {
        long Id PK
        string POCode UK
        decimal TotalAmount "18,2"
        decimal PaidAmount "18,2"
        decimal DebtAmount "18,2"
        string Status
        int SupplierId FK
        int BranchId FK
    }
    PurchaseOrderDetails {
        long Id PK
        int Quantity
        decimal UnitPrice "18,2"
        long PurchaseOrderId FK
        int ProductId FK
    }
    Orders {
        long Id PK
        string OrderCode UK
        decimal TotalAmount "18,2"
        decimal DiscountAmount "18,2"
        decimal FinalAmount "18,2"
        string OrderStatus
        string PaymentStatus
        string PaymentMethod
        int CustomerId FK "NEW nullable"
        int BranchId FK
        int CreatedBy FK
    }
    OrderDetails {
        long Id PK
        int Quantity
        decimal UnitPrice "18,2"
        long OrderId FK
        int ProductId FK
    }
    Invoices {
        long Id PK
        string InvoiceCode UK
        string InvoiceType "SALES/PURCHASE"
        decimal TotalAmount "18,2"
        decimal TaxAmount "18,2"
        decimal GrandTotal "18,2"
        datetime InvoiceDate
        long OrderId FK "nullable"
        long PurchaseOrderId FK "nullable"
        int CustomerId FK "nullable"
        int SupplierId FK "nullable"
    }
    Payments {
        long Id PK
        string PaymentCode UK
        decimal Amount "18,2"
        string PaymentMethod
        datetime PaymentDate
        string ReferenceType "ORDER/INVOICE/DEBT"
        long ReferenceId
        int CreatedBy FK
    }
    DebtRecords {
        long Id PK
        string DebtType "RECEIVABLE/PAYABLE"
        string PartnerType
        decimal TotalAmount "18,2"
        decimal PaidAmount "18,2"
        decimal RemainingAmount "18,2"
        string Status
        int SupplierId FK "nullable"
        int CustomerId FK "nullable"
    }
    DebtPayments {
        long Id PK
        string PaymentCode UK
        decimal Amount "18,2"
        string PaymentMethod
        long DebtRecordId FK
    }
    StocktakingSheets {
        long Id PK
        string Code UK
        datetime CheckDate
        string Status
        int WarehouseId FK "NEW thay BranchId"
    }
    StocktakingDetails {
        long Id PK
        int SystemQuantity
        int ActualQuantity
        int DifferenceQuantity
        decimal DifferenceValue "18,2"
        long StocktakingSheetId FK
        int ProductId FK
    }

    Categories ||--o{ Products : ""
    Units ||--o{ Products : ""
    Products ||--o{ Inventories : ""
    Products ||--o{ OrderDetails : ""
    Products ||--o{ PurchaseOrderDetails : ""
    Warehouses ||--o{ Inventories : ""
    Branches ||--o{ Warehouses : ""
    Customers ||--o{ Orders : ""
    Orders ||--o{ OrderDetails : ""
    Orders ||--o{ Invoices : ""
    Suppliers ||--o{ PurchaseOrders : ""
    PurchaseOrders ||--o{ PurchaseOrderDetails : ""
    PurchaseOrders ||--o{ Invoices : ""
    DebtRecords ||--o{ DebtPayments : ""
    StocktakingSheets ||--o{ StocktakingDetails : ""
```

---

## 5. Tổng hợp số lượng bảng

| Nhóm              | Số bảng | Bảng mới (Phiên 4) |
|--------------------|---------|---------------------|
| Hệ thống          | 12      | 0                   |
| HRM                | 18      | +6 (LeaveType, JobPosting, Candidate, Training, TrainingParticipant, KpiReview, KpiCriteria) |
| CMS                | 7       | +3 (Tag, ContentTag, Media, SeoMeta) |
| Kho & Bán hàng    | 16      | +4 (Customer, Unit, Warehouse, Invoice, Payment) |
| **Tổng**           | **53**  | **+13 bảng mới**    |

## 6. Thay đổi quan trọng trên bảng hiện có

| Bảng hiện có        | Thay đổi                                                    |
|----------------------|--------------------------------------------------------------|
| **Tất cả bảng**      | Thêm `IsDeleted`, `UpdatedAt`, `UpdatedBy` (nếu chưa có)    |
| `Products`           | Thêm `UnitId FK` → bảng `Units`                             |
| `Inventories`        | `BranchId` → `WarehouseId` (FK đến `Warehouses`)            |
| `Orders`             | Thêm `CustomerId FK` → bảng `Customers` (nullable)          |
| `LeaveRequests`      | Thêm `LeaveTypeId FK` → bảng `LeaveTypes`                   |
| `StocktakingSheets`  | `BranchId` → `WarehouseId` (FK đến `Warehouses`)            |
| `InventoryTransactions` | Thêm `SourceWarehouseId`, `DestWarehouseId` cho chuyển kho |
| `DebtRecords`        | Thêm `CustomerId FK` → bảng `Customers` (nullable)          |
