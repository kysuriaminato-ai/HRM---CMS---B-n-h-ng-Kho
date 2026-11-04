# Tiến độ dự án

Cập nhật lần cuối: 2026-11-04 (cuối phiên 1)

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

## Nợ kỹ thuật đã ghi nhận

Chi tiết trong `docs/ANALYSIS.md`, mục 6–9.
