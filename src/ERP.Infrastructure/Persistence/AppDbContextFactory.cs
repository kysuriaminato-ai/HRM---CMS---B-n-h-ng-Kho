using ERP_HRM_CMS.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP_HRM_CMS.Data
{
    /// <summary>
    /// Dùng riêng cho lệnh dotnet ef (ví dụ: dotnet ef migrations add V6_00_Security_And_Architecture).
    /// Nhờ factory này, công cụ EF không chạy Program.cs và không kích hoạt DbInitializer khi khởi động.
    /// Chuỗi kết nối lấy từ biến môi trường ConnectionStrings__DefaultConnection nếu có;
    /// nếu không có thì dùng chuỗi localdb mặc định cho máy phát triển (chỉ phục vụ design-time EF tools).
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
                .UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                .Options;

            return new AppDbContext(options, new EphemeralDataProtectionProvider());
        }
    }
}
