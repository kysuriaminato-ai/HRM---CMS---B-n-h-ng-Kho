using Xunit;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>Gán quyền mặc định một lần: quyền admin đã gỡ không được khôi phục khi khởi động lại.</summary>
    public class PermissionSeedOnceTests : IClassFixture<TestAppFactory>
    {
        private const string VaiTro = "ADMIN_KHO";
        private const string QuyenMau = "inventory.view";
        private const string KhoaLichSu = "QUYEN:ADMIN_KHO:inventory.view";

        private readonly TestAppFactory _factory;

        public PermissionSeedOnceTests(TestAppFactory factory)
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

        private static async Task<(int RoleId, int PermissionId)> LayIdAsync(AppDbContext db)
        {
            var role = await db.Roles.FirstAsync(r => r.Code == VaiTro);
            var perm = await db.Permissions.FirstAsync(p => p.Code == QuyenMau);
            return (role.Id, perm.Id);
        }

        [Fact]
        public async Task QuyenDaGo_KhongDuocKhoiPhucKhiGanMacDinhChayLai()
        {
            await using var db = NewContext();
            var (roleId, permId) = await LayIdAsync(db);

            var rp = await db.RolePermissions.FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permId);
            Assert.NotNull(rp); // điều kiện đầu: quyền đã được gán mặc định

            db.RolePermissions.Remove(rp!);
            await db.SaveChangesAsync();
            try
            {
                await DbInitializer.GanQuyenMacDinhMotLanAsync(db);

                Assert.False(await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permId));
            }
            finally
            {
                if (!await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permId))
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task QuyenMoi_DuocGanMotLan_SauDoKhongGanLaiKhiDaGo()
        {
            await using var db = NewContext();
            var (roleId, permId) = await LayIdAsync(db);
            bool coRp = await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permId);

            // Giả lập quyền mới: xóa lịch sử và quyền hiện có của cặp này
            var lichSu = await db.SeedHistories.FirstOrDefaultAsync(h => h.SeedKey == KhoaLichSu);
            if (lichSu != null) db.SeedHistories.Remove(lichSu);
            var rp = await db.RolePermissions.FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permId);
            if (rp != null) db.RolePermissions.Remove(rp);
            await db.SaveChangesAsync();

            try
            {
                // Lần 1: cặp chưa từng xét nên được gán
                await DbInitializer.GanQuyenMacDinhMotLanAsync(db);
                Assert.True(await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permId));
                Assert.True(await db.SeedHistories.AnyAsync(h => h.SeedKey == KhoaLichSu));

                // Quản trị viên gỡ quyền; lần 2 không được gán lại
                var dangCo = await db.RolePermissions.FirstAsync(x => x.RoleId == roleId && x.PermissionId == permId);
                db.RolePermissions.Remove(dangCo);
                await db.SaveChangesAsync();

                await DbInitializer.GanQuyenMacDinhMotLanAsync(db);
                Assert.False(await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permId));
            }
            finally
            {
                // Khôi phục trạng thái ban đầu của dữ liệu test
                if (!await db.SeedHistories.AnyAsync(h => h.SeedKey == KhoaLichSu))
                {
                    db.SeedHistories.Add(new SeedHistory { SeedKey = KhoaLichSu });
                }
                if (coRp && !await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permId))
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
                }
                await db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task SuperAdmin_CoTatCaQuyen_SauKhiGanMacDinh()
        {
            await using var db = NewContext();
            await DbInitializer.GanQuyenMacDinhMotLanAsync(db);

            int soQuyen = await db.Permissions.CountAsync();
            int soQuyenSuperAdmin = await db.RolePermissions.CountAsync(rp => rp.Role.Code == "SUPERADMIN");

            Assert.Equal(soQuyen, soQuyenSuperAdmin);
        }
    }
}
