using Xunit;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services.Kho;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>Kiểm thử tồn kho khi đồng thời (chạy trên SQL Server thật của project test).</summary>
    public class InventoryConcurrencyTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public InventoryConcurrencyTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        private AppDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(TestAppFactory.TestConnectionString)
                .Options;
            return new AppDbContext(options, _factory.Services.GetRequiredService<IDataProtectionProvider>());
        }

        private static string MaNgauNhien() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        // Mỗi test dùng chi nhánh riêng để không bị ảnh hưởng bởi dữ liệu của test khác
        private static async Task<int> TaoChiNhanhAsync(AppDbContext db, string ma)
        {
            var congTy = await db.Companies.OrderBy(c => c.Id).FirstOrDefaultAsync();
            if (congTy == null)
            {
                congTy = new Company { Code = "CT" + ma, Name = "Công ty kiểm thử " + ma };
                db.Companies.Add(congTy);
                await db.SaveChangesAsync();
            }

            var chiNhanh = new Branch { CompanyId = congTy.Id, Code = "CN" + ma, Name = "Chi nhánh kiểm thử " + ma };
            db.Branches.Add(chiNhanh);
            await db.SaveChangesAsync();
            return chiNhanh.Id;
        }

        private static async Task<int> TaoSanPhamAsync(AppDbContext db, decimal giaVon)
        {
            string ma = MaNgauNhien();
            var danhMuc = new Category { Code = "DM" + ma, Name = "Danh mục kiểm thử " + ma, Slug = "dm-" + ma.ToLowerInvariant() };
            db.Categories.Add(danhMuc);
            await db.SaveChangesAsync();

            var sanPham = new Product
            {
                CategoryId = danhMuc.Id,
                SKU = "SKU" + ma,
                Name = "Sản phẩm " + ma,
                Slug = "sp-" + ma.ToLowerInvariant(),
                CostPrice = giaVon,
                SalePrice = giaVon * 2
            };
            db.Products.Add(sanPham);
            await db.SaveChangesAsync();
            return sanPham.Id;
        }

        private static async Task TaoTonAsync(AppDbContext db, int branchId, int productId, int soLuong)
        {
            db.Inventories.Add(new Inventory
            {
                BranchId = branchId,
                ProductId = productId,
                StockQuantity = soLuong,
                MinStockAlert = 5,
                MaxStockLimit = 1000
            });
            await db.SaveChangesAsync();
        }

        private async Task<int> DocTonAsync(int branchId, int productId)
        {
            await using var db = NewContext();
            return await db.Inventories.AsNoTracking()
                .Where(i => i.BranchId == branchId && i.ProductId == productId)
                .Select(i => i.StockQuantity)
                .SingleAsync();
        }

        /// <summary>Một giao dịch xuất: chờ tín hiệu xuất phát, rồi trừ tồn trong transaction.</summary>
        private static async Task<bool> XuatAsync(AppDbContext db, int branchId, int productId, int soLuong, Task batDau)
        {
            await batDau;
            await using var giaoDich = await db.Database.BeginTransactionAsync();
            bool thanhCong = await InventoryStockService.TruTonAsync(db, branchId, productId, soLuong, VietnamTime.Now);
            if (thanhCong)
            {
                await giaoDich.CommitAsync();
            }
            else
            {
                await giaoDich.RollbackAsync();
            }
            return thanhCong;
        }

        [Fact]
        public async Task HaiGiaoDichXuatDongThoi_CungSanPham_ChiMotGiaoDichThanhCong()
        {
            int branchId, productId;
            await using (var setup = NewContext())
            {
                branchId = await TaoChiNhanhAsync(setup, MaNgauNhien());
                productId = await TaoSanPhamAsync(setup, 1000m);
                await TaoTonAsync(setup, branchId, productId, 10);
            }

            // Hai xuất 7 đơn vị trên tồn 10: tổng 14 > 10, chỉ được một giao dịch thành công
            await using var dbA = NewContext();
            await using var dbB = NewContext();
            var batDau = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var taskA = XuatAsync(dbA, branchId, productId, 7, batDau.Task);
            var taskB = XuatAsync(dbB, branchId, productId, 7, batDau.Task);
            batDau.SetResult();
            bool[] ketQua = await Task.WhenAll(taskA, taskB);

            Assert.Equal(1, ketQua.Count(k => k));
            Assert.Equal(3, await DocTonAsync(branchId, productId));
        }

        [Fact]
        public async Task TruTon_KhongDuHang_KhongThayDoiTon()
        {
            await using var db = NewContext();
            int branchId = await TaoChiNhanhAsync(db, MaNgauNhien());
            int productId = await TaoSanPhamAsync(db, 1000m);
            await TaoTonAsync(db, branchId, productId, 5);

            bool thanhCong = await InventoryStockService.TruTonAsync(db, branchId, productId, 6, VietnamTime.Now);

            Assert.False(thanhCong);
            Assert.Equal(5, await DocTonAsync(branchId, productId));
        }

        [Fact]
        public async Task DatTonTuyetDoi_TonDaDoi_KhongGhiDe()
        {
            await using var db = NewContext();
            int branchId = await TaoChiNhanhAsync(db, MaNgauNhien());
            int productId = await TaoSanPhamAsync(db, 1000m);
            await TaoTonAsync(db, branchId, productId, 10);
            await InventoryStockService.TruTonAsync(db, branchId, productId, 3, VietnamTime.Now); // tồn = 7

            // Kiểm kê còn đọc số 10 (cũ): không được ghi đè
            bool ghiSoCu = await InventoryStockService.DatTonTuyetDoiAsync(db, branchId, productId, 10, 4, VietnamTime.Now);
            Assert.False(ghiSoCu);
            Assert.Equal(7, await DocTonAsync(branchId, productId));

            // Kiểm kê đọc đúng số hiện tại (7): được ghi
            bool ghiSoMoi = await InventoryStockService.DatTonTuyetDoiAsync(db, branchId, productId, 7, 4, VietnamTime.Now);
            Assert.True(ghiSoMoi);
            Assert.Equal(4, await DocTonAsync(branchId, productId));
        }

        [Fact]
        public async Task TongHopTonKho_TinhDungTrenDatabase()
        {
            await using var db = NewContext();
            int branchId = await TaoChiNhanhAsync(db, MaNgauNhien());

            var truoc = await InventoryStockService.TongHopTonKhoAsync(db, branchId);
            Assert.Equal(0, truoc.SoMatHang);
            Assert.Equal(0m, truoc.GiaTriTonKho);

            int spA = await TaoSanPhamAsync(db, 1000m);
            await TaoTonAsync(db, branchId, spA, 4);   // tồn thấp (4 <= 5), giá trị 4 * 1000
            int spB = await TaoSanPhamAsync(db, 2500m);
            await TaoTonAsync(db, branchId, spB, 20);  // an toàn, giá trị 20 * 2500

            var sau = await InventoryStockService.TongHopTonKhoAsync(db, branchId);
            Assert.Equal(2, sau.SoMatHang);
            Assert.Equal(1, sau.SoMatHangCanhBao);
            Assert.Equal(54000m, sau.GiaTriTonKho);
        }
    }
}
