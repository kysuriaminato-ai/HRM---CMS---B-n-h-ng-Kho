using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using ERP_HRM_CMS.Controllers;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class PhaseC2AreasAndSecurityTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public PhaseC2AreasAndSecurityTests(TestAppFactory factory) => _factory = factory;

        private async Task<(string Username, string Password)> CreateRoleUserAsync(string roleCode)
        {
            var username = $"{roleCode.ToLowerInvariant()}_{Guid.NewGuid():N}"[..16];
            const string password = "UserTest@12345";

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            var role = await db.Roles.FirstAsync(r => r.Code == roleCode);
            var user = new User
            {
                Username = username,
                PasswordSalt = MatKhauHelper.GenerateSalt(),
                IsActive = true,
                IsSuperAdmin = false,
                MustChangePassword = false,
                CreatedAt = VietnamTime.Now
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            user.UserRoles.Add(new UserRole { RoleId = role.Id });

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return (username, password);
        }

        [Fact]
        public async Task Area_Admin_Blocks_Customer()
        {
            var adminControllers = new[]
            {
                typeof(QuanTriTaiKhoanController),
                typeof(PhanQuyenController),
                typeof(ContentController),
                typeof(SanPhamController),
                typeof(DanhMucController),
                typeof(UIController),
                typeof(VaiTroController),
                typeof(MenuController),
                typeof(LayoutConfigController),
                typeof(AuditLogController)
            };

            foreach (var ctrlType in adminControllers)
            {
                var areaAttr = ctrlType.GetCustomAttribute<AreaAttribute>();
                Assert.NotNull(areaAttr);
                Assert.Equal("Admin", areaAttr!.RouteValue);
            }

            var (customerUser, customerPass) = await CreateRoleUserAsync("CUSTOMER");
            using var client = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(client, customerUser, customerPass);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            var adminUrls = new[]
            {
                "/QuanTriTaiKhoan",
                "/PhanQuyen/MaTranPhanQuyen",
                "/Content",
                "/SanPham",
                "/DanhMuc"
            };

            foreach (var url in adminUrls)
            {
                using var res = await client.GetAsync(url);
                Assert.NotEqual(HttpStatusCode.OK, res.StatusCode);
                Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
                Assert.Contains("/Home/KhongCoQuyen", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task Area_Staff_Blocks_Customer()
        {
            var staffControllers = new[]
            {
                typeof(BanHangController),
                typeof(KhoController),
                typeof(CongNoController),
                typeof(DonHangController),
                typeof(PurchaseOrderController),
                typeof(NhanSuController),
                typeof(ChamCongController),
                typeof(DonTuController),
                typeof(BangLuongController),
                typeof(DaoTaoController),
                typeof(TuyenDungController),
                typeof(DanhGiaKpiController),
                typeof(QuyetDinhNhanSuController),
                typeof(SelfServiceController),
                typeof(PhieuInController),
                typeof(PhongBanController),
                typeof(HopDongController),
                typeof(TaiSanNhanSuController),
                typeof(SupplierController)
            };

            foreach (var ctrlType in staffControllers)
            {
                var areaAttr = ctrlType.GetCustomAttribute<AreaAttribute>();
                Assert.NotNull(areaAttr);
                Assert.Equal("Staff", areaAttr!.RouteValue);
            }

            var (customerUser, customerPass) = await CreateRoleUserAsync("CUSTOMER");
            using var client = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(client, customerUser, customerPass);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            var staffUrls = new[]
            {
                "/BanHang/POS",
                "/Kho",
                "/CongNo",
                "/DonHang",
                "/PurchaseOrder",
                "/NhanSu",
                "/ChamCong/BangCongThang",
                "/ChamCong/Heatmap",
                "/DonTu",
                "/BangLuong",
                "/DaoTao",
                "/TuyenDung",
                "/DanhGiaKpi",
                "/QuyetDinhNhanSu",
                "/CongNhanVien"
            };

            foreach (var url in staffUrls)
            {
                using var res = await client.GetAsync(url);
                Assert.NotEqual(HttpStatusCode.OK, res.StatusCode);
                Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
                Assert.Contains("/Home/KhongCoQuyen", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task Area_Shop_Public_Pages_Anonymous_Ok()
        {
            var shopControllers = new[]
            {
                typeof(PublicController),
                typeof(GioHangController),
                typeof(AuthController)
            };

            foreach (var ctrlType in shopControllers)
            {
                var areaAttr = ctrlType.GetCustomAttribute<AreaAttribute>();
                Assert.NotNull(areaAttr);
                Assert.Equal("Shop", areaAttr!.RouteValue);
            }

            using var anonClient = TestHelpers.NewClient(_factory);
            var publicUrls = new[]
            {
                "/",
                "/Public/Index",
                "/Public/TinTuc",
                "/Public/GioiThieu",
                "/Public/LienHe",
                "/tuyen-dung",
                "/GioHang",
                "/Auth/Login",
                "/Auth/Register",
                "/sitemap.xml",
                "/robots.txt"
            };

            foreach (var url in publicUrls)
            {
                using var res = await anonClient.GetAsync(url);
                Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            }

            // Trang tài khoản (đổi mật khẩu) yêu cầu đăng nhập
            using var anonChangePwd = await anonClient.GetAsync("/Auth/ChangePassword");
            Assert.Equal(HttpStatusCode.Redirect, anonChangePwd.StatusCode);
            Assert.Contains("/Auth/Login", anonChangePwd.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Routes_Unchanged_For_Listed_Urls()
        {
            var protectedUrls = new[]
            {
                // Khu vực Admin
                "/QuanTriTaiKhoan",
                "/PhanQuyen/MaTranPhanQuyen",
                "/Content",
                "/SanPham",
                "/DanhMuc",
                "/UI/Styleguide",
                "/VaiTro",
                "/Menu",
                "/LayoutConfig",
                "/AuditLog",
                // Khu vực Staff
                "/BanHang/POS",
                "/Kho",
                "/CongNo",
                "/DonHang",
                "/PurchaseOrder",
                "/NhanSu",
                "/ChamCong/BangCongThang",
                "/ChamCong/Heatmap",
                "/DonTu",
                "/BangLuong",
                "/DaoTao",
                "/TuyenDung",
                "/DanhGiaKpi",
                "/QuyetDinhNhanSu",
                "/CongNhanVien",
                "/PhieuIn/PhieuLuong/1",
                "/PhongBan",
                "/HopDong",
                "/TaiSanNhanSu",
                "/Supplier",
                // Khu vực Shop (yêu cầu đăng nhập)
                "/Auth/ChangePassword",
                // Giữ ở gốc Controllers/
                "/Home/Index"
            };

            // 1. Khi chưa đăng nhập: toàn bộ URL bảo vệ đều redirect về /Auth/Login (giữ nguyên mã trạng thái 302)
            using (var anonClient = TestHelpers.NewClient(_factory))
            {
                foreach (var url in protectedUrls)
                {
                    using var res = await anonClient.GetAsync(url);
                    Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
                    Assert.Contains("/Auth/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }

            // 2. Khi đăng nhập bằng admin: toàn bộ URL trên đều trả về 200 OK tại đúng đường dẫn gốc (không có tiền tố /Admin hay /Staff)
            using (var adminClient = TestHelpers.NewClient(_factory))
            {
                var loginRes = await TestHelpers.LoginAsync(adminClient, "admin", TestAppFactory.AdminPasswordForTests);
                Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

                foreach (var url in protectedUrls)
                {
                    using var res = await adminClient.GetAsync(url);
                    Assert.True(
                        res.StatusCode == HttpStatusCode.OK,
                        $"URL '{url}' trả về {(int)res.StatusCode} ({res.Headers.Location}) thay vì 200 OK.");
                }
            }
        }

        [Fact]
        public async Task Login_Redirects_By_Role_Admin_Employee_Customer()
        {
            // 1. Admin -> /PhanQuyen/MaTranPhanQuyen
            using (var adminClient = TestHelpers.NewClient(_factory))
            {
                var res = await TestHelpers.LoginAsync(adminClient, "admin", TestAppFactory.AdminPasswordForTests);
                Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
                Assert.Equal("/PhanQuyen/MaTranPhanQuyen", res.Headers.Location?.ToString());
            }

            // 2. Employee -> /CongNhanVien
            var (empUser, empPass) = await CreateRoleUserAsync("EMPLOYEE");
            using (var empClient = TestHelpers.NewClient(_factory))
            {
                var res = await TestHelpers.LoginAsync(empClient, empUser, empPass);
                Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
                Assert.Equal("/CongNhanVien", res.Headers.Location?.ToString());
            }

            // 3. Customer -> /
            var (custUser, custPass) = await CreateRoleUserAsync("CUSTOMER");
            using (var custClient = TestHelpers.NewClient(_factory))
            {
                var res = await TestHelpers.LoginAsync(custClient, custUser, custPass);
                Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
                Assert.Equal("/", res.Headers.Location?.ToString());
            }
        }

        [Fact]
        public async Task Create_Account_Generates_Random_Password_With_MustChange()
        {
            using var adminClient = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(adminClient, "admin", TestAppFactory.AdminPasswordForTests);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            // 1. GET /QuanTriTaiKhoan/Create sinh sẵn mật khẩu ngẫu nhiên 12 ký tự (không còn "Admin@123456")
            var createHtml = await adminClient.GetStringAsync("/QuanTriTaiKhoan/Create");
            Assert.DoesNotContain("Admin@123456", createHtml, StringComparison.Ordinal);

            var pwdMatch = Regex.Match(createHtml, @"id=""inputPassword""[^>]*value=""([^""]+)""");
            Assert.True(pwdMatch.Success, "Không tìm thấy ô inputPassword trên trang /QuanTriTaiKhoan/Create.");
            string prefilledPassword = System.Net.WebUtility.HtmlDecode(pwdMatch.Groups[1].Value);

            Assert.Equal(12, prefilledPassword.Length);
            Assert.Contains(prefilledPassword, char.IsUpper);
            Assert.Contains(prefilledPassword, char.IsLower);
            Assert.Contains(prefilledPassword, char.IsDigit);
            Assert.Contains(prefilledPassword, c => !char.IsLetterOrDigit(c));

            var tokenMatch = Regex.Match(createHtml, @"__RequestVerificationToken""[^>]*value=""([^""]+)""");
            Assert.True(tokenMatch.Success);

            // 2. POST /QuanTriTaiKhoan/Create (kể cả khi để trống Password để hệ thống tự sinh 12 ký tự)
            var newUsername = "staff_" + Guid.NewGuid().ToString("N")[..10];
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = newUsername,
                ["Password"] = string.Empty,
                ["IsActive"] = "true",
                ["MustChangePassword"] = "false", // Controller vẫn cưỡng chế MustChangePassword = true
                ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value
            });

            using var postRes = await adminClient.PostAsync("/QuanTriTaiKhoan/Create", form);
            Assert.Equal(HttpStatusCode.Redirect, postRes.StatusCode);
            Assert.Contains("/QuanTriTaiKhoan", postRes.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            // 3. Kiểm tra mật khẩu hiển thị 1 lần qua TempData trên trang Index
            var indexHtml = await adminClient.GetStringAsync("/QuanTriTaiKhoan");
            var decodedIndexHtml = System.Net.WebUtility.HtmlDecode(indexHtml);
            var tempPwdMatch = Regex.Match(decodedIndexHtml, @"Mật khẩu tạm thời \(hiển thị 1 lần\):\s*([^\s<]+)");
            Assert.True(tempPwdMatch.Success, "Không tìm thấy mật khẩu tạm thời hiển thị 1 lần trong TempData.");
            string oneTimePassword = tempPwdMatch.Groups[1].Value;

            Assert.Equal(12, oneTimePassword.Length);
            Assert.NotEqual("Admin@123456", oneTimePassword);
            Assert.Contains(oneTimePassword, char.IsUpper);
            Assert.Contains(oneTimePassword, char.IsLower);
            Assert.Contains(oneTimePassword, char.IsDigit);
            Assert.Contains(oneTimePassword, c => !char.IsLetterOrDigit(c));

            // 4. Kiểm tra trong DB: MustChangePassword = true và hash khớp với mật khẩu một lần
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            var createdUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == newUsername);
            Assert.NotNull(createdUser);
            Assert.True(createdUser!.MustChangePassword);
            Assert.NotEqual(
                PasswordVerificationResult.Failed,
                hasher.VerifyHashedPassword(createdUser, createdUser.PasswordHash, oneTimePassword));
        }

        [Fact]
        public async Task Register_Still_Assigns_Customer()
        {
            var username = "regc2_" + Guid.NewGuid().ToString("N")[..10];
            using var client = TestHelpers.NewClient(_factory);

            var regToken = await TestHelpers.GetAntiForgeryTokenAsync(client, "/Auth/Register");
            var regForm = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Password"] = "Customer@12345",
                ["ConfirmPassword"] = "Customer@12345",
                ["__RequestVerificationToken"] = regToken
            });

            using var regRes = await client.PostAsync("/Auth/Register", regForm);
            Assert.Equal(HttpStatusCode.Redirect, regRes.StatusCode);
            Assert.Contains("/Auth/Login", regRes.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            Assert.NotNull(user);
            Assert.True(user!.IsActive);
            Assert.False(user.IsSuperAdmin);
            Assert.Contains(user.UserRoles, ur => ur.Role.Code == "CUSTOMER");
        }

        [Fact]
        public async Task Area_Admin_Blocks_Employee()
        {
            var (empUser, empPass) = await CreateRoleUserAsync("EMPLOYEE");
            using var client = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(client, empUser, empPass);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            var adminUrls = new[]
            {
                "/QuanTriTaiKhoan",
                "/PhanQuyen/MaTranPhanQuyen",
                "/Content",
                "/SanPham",
                "/DanhMuc"
            };

            foreach (var url in adminUrls)
            {
                using var res = await client.GetAsync(url);
                Assert.NotEqual(HttpStatusCode.OK, res.StatusCode);
                var loc = res.Headers.Location?.ToString() ?? string.Empty;
                Assert.True(
                    res.StatusCode == HttpStatusCode.Redirect && (loc.Contains("/Home/KhongCoQuyen") || loc.Contains("/Auth/Login"))
                    || res.StatusCode == HttpStatusCode.Forbidden,
                    $"URL '{url}' trả về {(int)res.StatusCode} (Location: '{loc}') thay vì bị chặn!");
            }
        }

        [Fact]
        public async Task Area_Admin_Allows_SuperAdmin()
        {
            using var adminClient = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(adminClient, "admin", TestAppFactory.AdminPasswordForTests);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            using var res = await adminClient.GetAsync("/QuanTriTaiKhoan");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
    }
}

