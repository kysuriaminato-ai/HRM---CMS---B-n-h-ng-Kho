using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Chạy ứng dụng thật với database riêng ERP_HRM_CMS_TEST (không đụng dữ liệu phát triển).
    /// Mỗi lần chạy kiểm thử, database này được làm mới một lần rồi seed tự động.
    /// </summary>
    public class TestAppFactory : WebApplicationFactory<Program>
    {
        private const string TestDatabaseName = "ERP_HRM_CMS_TEST";

        public const string TestConnectionString =
            "Server=localhost;Database=ERP_HRM_CMS_TEST;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        private static readonly object ResetLock = new();
        private static bool _databaseReset;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);

            lock (ResetLock)
            {
                if (!_databaseReset)
                {
                    ResetTestDatabase();
                    _databaseReset = true;
                }
            }
        }

        /// <summary>Xóa database kiểm thử (chỉ ERP_HRM_CMS_TEST) để mỗi lần chạy bắt đầu từ trạng thái sạch.</summary>
        private static void ResetTestDatabase()
        {
            using var connection = new SqlConnection(
                "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
IF DB_ID(N'{TestDatabaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{TestDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{TestDatabaseName}];
END";
            command.ExecuteNonQuery();
        }
    }
}
