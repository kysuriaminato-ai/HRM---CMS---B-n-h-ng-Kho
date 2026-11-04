using ERP_HRM_CMS.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP_HRM_CMS.Data
{
    /// <summary>
    /// Dùng riêng cho lệnh dotnet ef (ví dụ: dotnet ef migrations add InitialBaseline).
    /// Nhờ factory này, công cụ EF không chạy Program.cs và không kích hoạt DbInitializer khi khởi động.
    /// Lệnh EF chỉ dựng model, không mở kết nối tới database nên không cần dữ liệu thật.
    /// Chuỗi kết nối lấy từ biến môi trường ConnectionStrings__DefaultConnection nếu có;
    /// nếu không có thì dùng chuỗi localdb mặc định cho máy phát triển (không chứa mật khẩu).
    /// </summary>
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        private const string FallbackConnectionString =
            "Server=(localdb)\\mssqllocaldb;Database=ERP_HRM_CMS_DEV;Trusted_Connection=True;TrustServerCertificate=True;";

        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = FallbackConnectionString;
            }

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new AppDbContext(options, new EphemeralDataProtectionProvider());
        }
    }
}
