using Xunit;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>Mọi cột decimal đều có kiểu lưu trữ rõ ràng; cột tiền là decimal(18,2).</summary>
    public class DecimalPrecisionTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public DecimalPrecisionTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void MoiCotDecimal_DeuCoKieuLuuTru()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var thieu = new List<string>();
            foreach (var kieu in db.Model.GetEntityTypes())
            {
                foreach (var thuocTinh in kieu.GetProperties())
                {
                    if ((thuocTinh.ClrType == typeof(decimal) || thuocTinh.ClrType == typeof(decimal?))
                        && thuocTinh.GetColumnType() == null)
                    {
                        thieu.Add($"{kieu.ClrType.Name}.{thuocTinh.Name}");
                    }
                }
            }

            Assert.Empty(thieu);
        }

        [Fact]
        public void CotGiaVon_LaDecimal182()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cot = db.Model.FindEntityType(typeof(Product))!.FindProperty(nameof(Product.CostPrice))!;
            Assert.Equal("decimal(18,2)", cot.GetColumnType());
        }
    }
}
