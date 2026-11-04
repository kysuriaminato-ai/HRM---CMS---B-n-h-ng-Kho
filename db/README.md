# Database Guide — ERP HRM CMS

## Tổng quan

Hệ thống sử dụng **SQL Server** với **EF Core 8 Code-First**.  
Toàn bộ schema được quản lý qua Migration + script SQL thủ công cho các thay đổi lớn.

---

## 1. Tạo Database

### Cách 1: Dùng EF Core Migration (khuyên dùng)

```bash
# Từ thư mục gốc project
dotnet ef database update --project . --startup-project .
```

### Cách 2: Dùng SQL Script thủ công

1. Mở **SSMS** (SQL Server Management Studio)
2. Tạo database mới (nếu chưa có):
   ```sql
   CREATE DATABASE ERP_HRM_CMS;
   ```
3. Chạy lần lượt các script trong `db/scripts/` theo thứ tự đánh số:
   - `V4_01_NewTables_And_AuditColumns.sql` — Tạo bảng mới + cột audit
   - `V4_02_SeedData.sql` — Dữ liệu mẫu

---

## 2. Connection String

**Không hardcode** connection string. Sử dụng một trong các cách sau:

### Development (User Secrets)

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ERP_HRM_CMS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ERP_HRM_CMS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### Production (biến môi trường)

```bash
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=ERP_HRM_CMS;..."
```

---

## 3. Chạy Migration

### Tạo migration mới

```bash
dotnet ef migrations add <TenMigration> --project . --startup-project .
```

### Áp dụng migration

```bash
dotnet ef database update
```

### Xuất SQL script từ migration

```bash
dotnet ef migrations script --idempotent -o db/scripts/migration_output.sql
```

---

## 4. Chạy Seed Data

### Cách 1: Qua ứng dụng

Seed data tự động chạy khi khởi động app (nếu đã cấu hình trong `Program.cs`).

### Cách 2: Chạy SQL Script

Mở SSMS, kết nối tới database, chạy file:

```
db/scripts/V4_02_SeedData.sql
```

> **Lưu ý**: Script seed data là **idempotent** — kiểm tra tồn tại trước khi INSERT, có thể chạy nhiều lần an toàn.

---

## 5. Backup & Restore

### Backup

```sql
BACKUP DATABASE [ERP_HRM_CMS]
TO DISK = N'C:\Backup\ERP_HRM_CMS_backup.bak'
WITH FORMAT, COMPRESSION, STATS = 10;
```

### Restore

```sql
RESTORE DATABASE [ERP_HRM_CMS]
FROM DISK = N'C:\Backup\ERP_HRM_CMS_backup.bak'
WITH REPLACE, STATS = 10;
```

### Backup tự động (SQL Server Agent)

1. Mở SQL Server Agent → Jobs → New Job
2. Đặt schedule (ví dụ: hàng ngày lúc 2:00 AM)
3. Step: chạy lệnh BACKUP ở trên

---

## 6. Cấu trúc thư mục

```
db/
├── README.md              ← File này
└── scripts/
    ├── V4_01_NewTables_And_AuditColumns.sql   ← Schema Phase 4
    └── V4_02_SeedData.sql                      ← Dữ liệu mẫu
```

---

## 7. Quy ước Database

| Quy ước                | Chi tiết                                                |
|------------------------|---------------------------------------------------------|
| PK                     | `int IDENTITY(1,1)` — `bigint` cho bảng lượng lớn      |
| Audit columns          | `IsDeleted`, `UpdatedAt`, `UpdatedBy`, `CreatedBy`      |
| Soft delete            | Global query filter `WHERE IsDeleted = 0`               |
| Tiền tệ (VND)         | `decimal(18,2)`                                         |
| Mã code                | Unique index trên cột `Code`, `SKU`, `POCode`...        |
| FK naming              | `FK_<Bảng>_<BảngĐích>`                                  |
| Index naming           | `IX_<Bảng>_<Cột>`                                       |
| Delete behavior        | `CASCADE` cho detail, `RESTRICT` cho cross-ref, `SET NULL` cho optional |
| Timezone               | Lưu UTC trong DB, hiển thị VN time trong app            |

---

## 8. ERD

Xem diagram đầy đủ tại: [`docs/ERD.md`](../docs/ERD.md)

- **System**: 12 bảng (Users, Roles, Permissions, AuditLogs, Notifications...)
- **HRM**: 18 bảng (Employees, Contracts, LeaveRequests, Payroll, KPI, Training...)
- **CMS**: 7 bảng (Contents, Categories, Tags, Media, SeoMeta...)
- **SCM**: 16 bảng (Products, Orders, Inventory, PurchaseOrders, Customers, Warehouses, Invoices...)

Tổng: **53 bảng**
