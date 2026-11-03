using ERP_HRM_CMS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Kiểm tra vai trò sau khi seed: không còn alias, có SALES_DIRECTOR và EMPLOYEE, và phạm vi quyền đúng.
    /// </summary>
    public class RoleSeedTests : IClassFixture<TestAppFactory>
    {
        /// <summary>Các mã alias đã bị bỏ. Không được xuất hiện lại sau khi seed.</summary>
        private static readonly string[] AliasCodes = { "QTVP", "SalesStaff", "SaleDirector", "WarehouseKeeper" };

        private readonly TestAppFactory _factory;

        public RoleSeedTests(TestAppFactory factory) => _factory = factory;

        [Fact]
        public async Task Seeded_Roles_Do_Not_Contain_Legacy_Alias_Codes()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var codes = await db.Roles.Select(r => r.Code).ToListAsync();

            foreach (var alias in AliasCodes)
            {
                Assert.DoesNotContain(alias, codes, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task Seeded_Roles_Contain_Sales_Director_And_Employee()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var codes = await db.Roles.Select(r => r.Code).ToListAsync();

            Assert.Contains("SALES_DIRECTOR", codes);
            Assert.Contains("EMPLOYEE", codes);
        }

        [Fact]
        public async Task Employee_Role_Has_Only_The_Self_Service_Permission()
        {
            var codes = await PermissionCodesOfAsync("EMPLOYEE");

            Assert.Equal(new[] { "hrm.self.service" }, codes);
        }

        [Fact]
        public async Task Sales_Director_Role_Manages_Orders_But_Has_No_HR_Permission()
        {
            var codes = await PermissionCodesOfAsync("SALES_DIRECTOR");

            Assert.Contains(codes, c => c.StartsWith("order.", StringComparison.Ordinal));
            Assert.Contains("report.sales", codes);
            Assert.DoesNotContain(codes, c => c.StartsWith("hrm.", StringComparison.Ordinal));
        }

        private async Task<List<string>> PermissionCodesOfAsync(string roleCode)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await db.RolePermissions
                .Where(rp => rp.Role.Code == roleCode)
                .Select(rp => rp.Permission.Code)
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}
