using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

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

        /// <summary>Mật khẩu quản trị chỉ dùng trong kiểm thử (không phải bí mật thật).</summary>
        public const string AdminPasswordForTests = "Test@Admin12345";

        private static readonly object ResetLock = new();
        private static bool _databaseReset;
        private static bool _enterpriseHrmScriptsExecuted;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
            builder.UseSetting("SeedAdmin:Password", AdminPasswordForTests);
            // Kiểm thử cần dữ liệu mẫu (nhân viên, lương) để chạy các test in ấn và thông báo
            builder.UseSetting("Seed:Demo", "true");

            lock (ResetLock)
            {
                if (!_databaseReset)
                {
                    ResetTestDatabase();
                    _databaseReset = true;
                }
            }
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            EnsureEnterpriseHrmApplied();
            return host;
        }

        private static void EnsureEnterpriseHrmApplied()
        {
            lock (ResetLock)
            {
                if (_enterpriseHrmScriptsExecuted) return;

                var dir = AppContext.BaseDirectory;
                while (dir != null && !File.Exists(Path.Combine(dir, "ERP_HRM_CMS.sln")))
                {
                    dir = Directory.GetParent(dir)?.FullName;
                }
                if (dir == null) return;

                var v601Path = Path.Combine(dir, "db", "scripts", "V6_01_EnterpriseHRM_Schema_And_Indexes.sql");
                var v602Path = Path.Combine(dir, "db", "scripts", "V6_02_EnterpriseHRM_SeedData.sql");

                using var conn = new SqlConnection(TestConnectionString);
                conn.Open();

                ExecuteSqlScript(conn, v601Path);
                ExecuteSqlScript(conn, v602Path);

                _enterpriseHrmScriptsExecuted = true;
            }
        }

        private static void ExecuteSqlScript(SqlConnection connection, string scriptPath)
        {
            if (!File.Exists(scriptPath)) return;
            var scriptText = File.ReadAllText(scriptPath);
            var batches = System.Text.RegularExpressions.Regex.Split(
                scriptText,
                @"^\s*GO\s*$",
                System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                using var cmd = connection.CreateCommand();
                cmd.CommandText = trimmed;
                cmd.ExecuteNonQuery();
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
