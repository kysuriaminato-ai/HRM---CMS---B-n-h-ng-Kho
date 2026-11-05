using System.Net;
using System.Text.Json;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Enums;
using ERP.Domain.Exceptions;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Kiểm thử toàn diện Phiên 1:
    /// - Clean Architecture 4 tầng & quy tắc phụ thuộc một chiều (Dependency Rule)
    /// - Result / Result&lt;T&gt;, PagedResult&lt;T&gt;, IDataScopeService (Company / Branch / Department / Self)
    /// - HealthCheck (/health), CorrelationId Middleware, IMemoryCache tự động xóa cache khi đổi quyền/cấu hình
    /// - Các bản vá Bảo mật P0: Dashboard theo quyền, GlobalQuickSearch không lộ PII,
    ///   Cấp tài khoản nhanh 1 lần + bắt buộc đổi mật khẩu lần đầu, Chặn tự duyệt đơn nghỉ phép,
    ///   Trần giảm giá POS phía Server, và bảo vệ API TimKiemSanPhamPos / Supplier.GetSupplier.
    /// </summary>
    public class Phase1ArchitectureAndSecurityTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public Phase1ArchitectureAndSecurityTests(TestAppFactory factory) => _factory = factory;

        // ═══════════════════════════════════════════════════════════════════════════
        // 1. KIẾN TRÚC CLEAN ARCHITECTURE & MÔ HÌNH CỐT LÕI APPLICATION
        // ═══════════════════════════════════════════════════════════════════════════

        [Fact]
        public void CleanArchitecture_DependencyRule_IsStrictlyEnforced()
        {
            var domainAsm = typeof(DomainException).Assembly;
            var appAsm = typeof(Result).Assembly;
            var infraAsm = typeof(AppDbContext).Assembly;

            var domainRefs = domainAsm.GetReferencedAssemblies().Select(a => a.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("ERP.Application", domainRefs);
            Assert.DoesNotContain("ERP.Infrastructure", domainRefs);
            Assert.DoesNotContain("ERP_HRM_CMS", domainRefs);
            Assert.DoesNotContain(domainRefs, r => r.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));

            var appRefs = appAsm.GetReferencedAssemblies().Select(a => a.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("ERP.Domain", appRefs);
            Assert.DoesNotContain("ERP.Infrastructure", appRefs);
            Assert.DoesNotContain("ERP_HRM_CMS", appRefs);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.SqlServer", appRefs);

            var infraRefs = infraAsm.GetReferencedAssemblies().Select(a => a.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("ERP.Domain", infraRefs);
            Assert.Contains("ERP.Application", infraRefs);
            Assert.DoesNotContain("ERP_HRM_CMS", infraRefs);
        }

        [Fact]
        public void Result_And_PagedResult_Enforce_Invariants_Correctly()
        {
            var ok = Result<int>.Success(42);
            Assert.True(ok.IsSuccess);
            Assert.False(ok.IsFailure);
            Assert.Equal(42, ok.GetRequiredValue());

            var fail = Result<int>.Failure("Không đủ quyền", "FORBIDDEN");
            Assert.True(fail.IsFailure);
            Assert.Equal("FORBIDDEN", fail.ErrorCode);
            Assert.Throws<InvalidOperationException>(() => fail.GetRequiredValue());

            var validation = Result.ValidationFailure(new[] { "Thiếu mã nhân viên", "Thiếu phòng ban" });
            Assert.True(validation.IsFailure);
            Assert.Equal("VALIDATION_ERROR", validation.ErrorCode);
            Assert.Equal(2, validation.Errors.Count);

            var paged = PagedResult<string>.Create(new[] { "A", "B" }, totalCount: 45, pageNumber: 2, pageSize: 20);
            Assert.Equal(3, paged.TotalPages);
            Assert.True(paged.HasPreviousPage);
            Assert.True(paged.HasNextPage);

            var clamped = PagedResult<string>.Create(Array.Empty<string>(), totalCount: -5, pageNumber: 0, pageSize: 9999);
            Assert.Equal(1, clamped.PageNumber);
            Assert.Equal(PagedResult<string>.MaxPageSize, clamped.PageSize);
            Assert.Equal(0, clamped.TotalCount);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // 2. PHÂN QUYỀN PHẠM VI DỮ LIỆU (COMPANY / BRANCH / DEPARTMENT / SELF)
        // ═══════════════════════════════════════════════════════════════════════════

        [Fact]
        public void DataScopeService_Filters_Company_Branch_Department_And_Self_Accurately()
        {
            var deptHnIt = new Department { Id = 10, BranchId = 1, Name = "IT HN" };
            var deptHnSales = new Department { Id = 11, BranchId = 1, Name = "Sales HN" };
            var deptHcmSales = new Department { Id = 20, BranchId = 2, Name = "Sales HCM" };

            var employees = new List<Employee>
            {
                new() { Id = 100, FullName = "NV IT HN", DepartmentId = 10, Department = deptHnIt },
                new() { Id = 101, FullName = "NV Sales HN", DepartmentId = 11, Department = deptHnSales },
                new() { Id = 200, FullName = "NV Sales HCM", DepartmentId = 20, Department = deptHcmSales }
            }.AsQueryable();

            var orders = new List<Order>
            {
                new() { Id = 1, BranchId = 1, CreatedBy = 50 },
                new() { Id = 2, BranchId = 2, CreatedBy = 60 }
            }.AsQueryable();

            // 1. Company scope (HR_MANAGER / SuperAdmin) -> thấy cả 3 nhân sự
            var hrScope = new DataScopeService(new FakeCurrentUser(userId: 1, roles: new[] { "HR_MANAGER" }));
            Assert.Equal(DataScopeLevel.Company, hrScope.GetHrmScopeLevel());
            Assert.Equal(3, hrScope.ApplyEmployeeScope(employees).Count());

            // 2. Branch scope (BRANCH_MANAGER chi nhánh 1) -> chỉ thấy 2 nhân sự CN 1 và đơn hàng CN 1; ép BranchId về 1
            var branchScope = new DataScopeService(new FakeCurrentUser(userId: 2, branchId: 1, roles: new[] { "BRANCH_MANAGER" }));
            Assert.Equal(DataScopeLevel.Branch, branchScope.GetHrmScopeLevel());
            Assert.Equal(new[] { 100, 101 }, branchScope.ApplyEmployeeScope(employees).Select(e => e.Id).ToArray());
            Assert.Equal(new[] { 1L }, branchScope.ApplyOrderScope(orders).Select(o => o.Id).ToArray());
            Assert.Equal(1, branchScope.ResolveAllowedBranchId(requestedBranchId: 2));

            // 3. Department scope (DEPARTMENT_HEAD phòng 10) -> chỉ thấy nhân sự phòng 10
            var deptScope = new DataScopeService(new FakeCurrentUser(userId: 3, branchId: 1, departmentId: 10, roles: new[] { "DEPARTMENT_HEAD" }));
            Assert.Equal(DataScopeLevel.Department, deptScope.GetHrmScopeLevel());
            Assert.Equal(new[] { 100 }, deptScope.ApplyEmployeeScope(employees).Select(e => e.Id).ToArray());

            // 4. Self scope (EMPLOYEE mã 200) -> chỉ thấy chính hồ sơ 200 của mình
            var selfScope = new DataScopeService(new FakeCurrentUser(userId: 60, employeeId: 200, branchId: 2, departmentId: 20, roles: new[] { "EMPLOYEE" }));
            Assert.Equal(DataScopeLevel.Self, selfScope.GetHrmScopeLevel());
            Assert.Equal(new[] { 200 }, selfScope.ApplyEmployeeScope(employees).Select(e => e.Id).ToArray());
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // 3. UNIT TEST CHÍNH SÁCH NGHIỆP VỤ BẢO MẬT (LEAVE APPROVAL & POS DISCOUNT)
        // ═══════════════════════════════════════════════════════════════════════════

        [Fact]
        public void LeaveApprovalPolicy_Blocks_SelfApproval_Except_SuperAdmin_With_AuditFlag()
        {
            // Trưởng phòng tự bấm duyệt đơn của chính mình -> bị chặn
            var selfAttempt = LeaveApprovalPolicy.ValidateApprover(
                currentUserId: 15,
                currentUserEmployeeId: 8,
                isSuperAdmin: false,
                requestEmployeeId: 8,
                requestCreatedByUserId: 15);
            Assert.True(selfAttempt.IsFailure);
            Assert.Equal("SELF_APPROVAL_FORBIDDEN", selfAttempt.ErrorCode);

            // Trưởng phòng duyệt đơn của nhân viên khác -> hợp lệ, không bật cờ override
            var normalApprove = LeaveApprovalPolicy.ValidateApprover(
                currentUserId: 15,
                currentUserEmployeeId: 8,
                isSuperAdmin: false,
                requestEmployeeId: 9,
                requestCreatedByUserId: 22);
            Assert.True(normalApprove.IsSuccess);
            Assert.False(normalApprove.GetRequiredValue().IsSelfApprovalOverrideBySuperAdmin);

            // SuperAdmin tự duyệt đơn do chính SuperAdmin tạo -> hợp lệ nhưng bắt buộc gắn cờ AuditLog
            var superAdminSelf = LeaveApprovalPolicy.ValidateApprover(
                currentUserId: 1,
                currentUserEmployeeId: null,
                isSuperAdmin: true,
                requestEmployeeId: 9,
                requestCreatedByUserId: 1);
            Assert.True(superAdminSelf.IsSuccess);
            Assert.True(superAdminSelf.GetRequiredValue().IsSelfApprovalOverrideBySuperAdmin);
        }

        [Fact]
        public void PosDiscountPolicy_Enforces_RoleCeilings_Bounds_And_AuditReason()
        {
            const decimal subTotal = 1_000_000m;

            // Giảm giá âm -> từ chối
            Assert.Equal("NEGATIVE_DISCOUNT",
                PosDiscountPolicy.Validate(subTotal, -10_000m, "test", false, false).ErrorCode);

            // Giảm giá vượt SubTotal -> từ chối
            Assert.Equal("DISCOUNT_EXCEEDS_SUBTOTAL",
                PosDiscountPolicy.Validate(subTotal, 1_200_000m, "test", true, true).ErrorCode);

            // Thu ngân (order.pos) giảm 15% (> 10%) -> từ chối
            Assert.Equal("DISCOUNT_CEILING_EXCEEDED",
                PosDiscountPolicy.Validate(subTotal, 150_000m, "Khách VIP", isSuperAdmin: false, canManageOrders: false).ErrorCode);

            // Thu ngân giảm 8% (<= 10%) nhưng bỏ trống lý do (> 5% cần lý do) -> từ chối
            Assert.Equal("DISCOUNT_REASON_REQUIRED",
                PosDiscountPolicy.Validate(subTotal, 80_000m, note: "", isSuperAdmin: false, canManageOrders: false).ErrorCode);

            // Thu ngân giảm 8% có ghi lý do -> chấp nhận
            Assert.True(PosDiscountPolicy.Validate(subTotal, 80_000m, "Ưu đãi sinh nhật", isSuperAdmin: false, canManageOrders: false).IsSuccess);

            // Quản lý đơn hàng (order.manage) giảm 25% (<= 30%) có lý do -> chấp nhận; 35% -> từ chối
            Assert.True(PosDiscountPolicy.Validate(subTotal, 250_000m, "Chiết khấu đại lý", isSuperAdmin: false, canManageOrders: true).IsSuccess);
            Assert.Equal("DISCOUNT_CEILING_EXCEEDED",
                PosDiscountPolicy.Validate(subTotal, 350_000m, "Chiết khấu đại lý", isSuperAdmin: false, canManageOrders: true).ErrorCode);

            // SuperAdmin giảm 60% (> 50%) -> vẫn bị chặn xuất đơn gần 0 đồng
            Assert.Equal("DISCOUNT_CEILING_EXCEEDED",
                PosDiscountPolicy.Validate(subTotal, 600_000m, "Test", isSuperAdmin: true, canManageOrders: true).ErrorCode);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // 4. HẠ TẦNG INFRASTRUCTURE: HEALTH CHECK, CORRELATION ID & MEMORY CACHE
        // ═══════════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task HealthEndpoint_And_CorrelationIdMiddleware_Work_EndToEnd()
        {
            using var client = TestHelpers.NewClient(_factory);
            const string customCorrelationId = "corr-phase1-test-999";

            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("X-Correlation-ID", customCorrelationId);

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var echoedValues));
            Assert.Equal(customCorrelationId, echoedValues!.Single());

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
        }

        [Fact]
        public async Task CentralizedMemoryCache_AutoInvalidates_When_Permissions_Or_SiteSettings_Change()
        {
            int userId = await TestHelpers.CreateUserAsync(_factory.Services);

            using (var scope = _factory.Services.CreateScope())
            {
                var permService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Lần 1: User mới chưa có quyền order.view -> nạp vào IMemoryCache
                Assert.False(await permService.HasPermissionAsync(userId, "order.view"));

                // Cấp quyền trực tiếp qua AppDbContext -> SaveChangesAsync phải tự động xóa cache quyền của user
                var perm = await db.Permissions.FirstAsync(p => p.Code == "order.view");
                db.UserPermissions.Add(new UserPermission
                {
                    UserId = userId,
                    PermissionId = perm.Id,
                    IsGranted = true
                });
                await db.SaveChangesAsync();

                // Đọc lại ngay lập tức phải thấy quyền mới mà không bị kẹt dữ liệu cũ trong cache
                Assert.True(await permService.HasPermissionAsync(userId, "order.view"));
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                string originalHotline = SiteSettingReader.Get(db, "Site_Hotline", "1900 6868");

                var hotlineRow = await db.SiteSettings.FirstAsync(s => s.SettingKey == "Site_Hotline");
                hotlineRow.SettingValue = "1900 9999";
                await db.SaveChangesAsync();

                string refreshedHotline = SiteSettingReader.Get(db, "Site_Hotline", "1900 6868");
                Assert.Equal("1900 9999", refreshedHotline);

                // Khôi phục giá trị ban đầu
                hotlineRow.SettingValue = originalHotline;
                await db.SaveChangesAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // 5. KIỂM THỬ TÍCH HỢP BẢO MẬT P0 (DASHBOARD, QUICKSEARCH, TÀI KHOẢN TẠM, ĐƠN TỪ, POS)
        // ═══════════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Dashboard_And_GlobalQuickSearch_StrictlyEnforce_Permissions_And_Hide_PII()
        {
            var (empUserId, empUsername) = await CreateUserWithRoleAsync("EMPLOYEE", linkToSampleEmployee: true);

            // 1. EMPLOYEE truy cập Dashboard -> nhận không gian cá nhân 200 OK, KHÔNG lộ doanh thu / công nợ toàn công ty
            using var empClient = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(empClient, empUsername, TestHelpers.Password);

            var empDashRes = await empClient.GetAsync("/Home/Index");
            Assert.Equal(HttpStatusCode.OK, empDashRes.StatusCode);
            string empDashHtml = WebUtility.HtmlDecode(await empDashRes.Content.ReadAsStringAsync());
            Assert.Contains("Không gian làm việc cá nhân", empDashHtml);
            Assert.Contains("Ngày công tháng này", empDashHtml);
            Assert.DoesNotContain("Doanh thu tháng", empDashHtml);
            Assert.DoesNotContain("Tổng công nợ phải thu", empDashHtml);
            Assert.DoesNotContain("Top sản phẩm bán chạy", empDashHtml);

            // 2. EMPLOYEE gọi GlobalQuickSearch -> không có hrm.employee.view / product.view / order.view nên không lộ kết quả
            var empSearchRes = await empClient.GetAsync("/Home/GlobalQuickSearch?q=NV");
            Assert.Equal(HttpStatusCode.OK, empSearchRes.StatusCode);
            using (var searchDoc = JsonDocument.Parse(await empSearchRes.Content.ReadAsStringAsync()))
            {
                Assert.Equal(0, searchDoc.RootElement.GetProperty("employees").GetArrayLength());
                Assert.Equal(0, searchDoc.RootElement.GetProperty("products").GetArrayLength());
                Assert.Equal(0, searchDoc.RootElement.GetProperty("orders").GetArrayLength());
            }

            // 3. SuperAdmin truy cập Dashboard & GlobalQuickSearch -> thấy đầy đủ điều hành nhưng QuickSearch không lộ Phone / Email nhạy cảm
            using var adminClient = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(adminClient, "admin", TestAppFactory.AdminPasswordForTests);

            var adminDashRes = await adminClient.GetAsync("/Home/Index");
            Assert.Equal(HttpStatusCode.OK, adminDashRes.StatusCode);
            string adminDashHtml = WebUtility.HtmlDecode(await adminDashRes.Content.ReadAsStringAsync());
            Assert.Contains("Trung tâm điều hành", adminDashHtml);
            Assert.Contains("Doanh thu tháng", adminDashHtml);

            var adminSearchRes = await adminClient.GetAsync("/Home/GlobalQuickSearch?q=NV");
            Assert.Equal(HttpStatusCode.OK, adminSearchRes.StatusCode);
            string adminSearchJson = await adminSearchRes.Content.ReadAsStringAsync();
            Assert.DoesNotContain("\"phone\"", adminSearchJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"email\"", adminSearchJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CapTaiKhoanNhanh_Generates_OneTimePassword_And_Enforces_FirstLogin_PasswordChange()
        {
            int newEmployeeId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dept = await db.Departments.FirstAsync();
                var pos = await db.Positions.FirstAsync();
                var emp = new Employee
                {
                    EmployeeCode = "NV" + Random.Shared.Next(10000, 99999),
                    FullName = "Nguyễn Văn Kiểm Thử P0",
                    Email = $"nv_p0_{Guid.NewGuid():N}@tienphong.vn",
                    Phone = "09" + Random.Shared.Next(10000000, 99999999),
                    DepartmentId = dept.Id,
                    PositionId = pos.Id,
                    Status = "Active",
                    HireDate = VietnamTime.Today,
                    CreatedAt = VietnamTime.Now
                };
                db.Employees.Add(emp);
                await db.SaveChangesAsync();
                newEmployeeId = emp.Id;
            }

            // 1. Admin cấp tài khoản nhanh qua AJAX
            using var adminClient = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(adminClient, "admin", TestAppFactory.AdminPasswordForTests);
            string csrf = await TestHelpers.GetAntiForgeryTokenAsync(adminClient, $"/NhanSu/Details/{newEmployeeId}");

            using var provisionReq = new HttpRequestMessage(HttpMethod.Post, "/NhanSu/CapTaiKhoanNhanh")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["employeeId"] = newEmployeeId.ToString(),
                    ["__RequestVerificationToken"] = csrf
                })
            };
            provisionReq.Headers.Add("X-Requested-With", "XMLHttpRequest");

            using var provisionRes = await adminClient.SendAsync(provisionReq);
            Assert.Equal(HttpStatusCode.OK, provisionRes.StatusCode);
            Assert.Contains("no-store", provisionRes.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            string jsonBody = await provisionRes.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonBody);
            Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
            Assert.True(doc.RootElement.GetProperty("mustChangePasswordOnFirstLogin").GetBoolean());

            string provisionedUsername = doc.RootElement.GetProperty("username").GetString()!;
            string tempPassword = doc.RootElement.GetProperty("temporaryPassword").GetString()!;
            Assert.True(tempPassword.Length >= 12);

            // Kiểm tra AuditLogs tuyệt đối không lưu mật khẩu tạm dạng rõ
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userInDb = await db.Users.SingleAsync(u => u.Username == provisionedUsername);
                Assert.True(userInDb.MustChangePassword);

                var audit = await db.AuditLogs
                    .OrderByDescending(a => a.Id)
                    .FirstAsync(a => a.Action == "PROVISION_TEMP_ACCOUNT" && a.RecordId == userInDb.Id.ToString());
                Assert.DoesNotContain(tempPassword, audit.NewValues ?? string.Empty);
            }

            // 2. Nhân viên đăng nhập lần đầu bằng mật khẩu tạm -> bị chuyển hướng sang /Auth/ChangePassword
            using var userClient = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(userClient, provisionedUsername, tempPassword);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);
            Assert.Contains("/Auth/ChangePassword", loginRes.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            // 3. Cố tình vượt rào truy cập thẳng /Home/Index khi chưa đổi mật khẩu -> Middleware chặn và ép về /Auth/ChangePassword
            var bypassAttempt = await userClient.GetAsync("/Home/Index");
            Assert.Equal(HttpStatusCode.Redirect, bypassAttempt.StatusCode);
            Assert.Contains("/Auth/ChangePassword", bypassAttempt.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            // 4. Thực hiện đổi mật khẩu cá nhân hợp lệ
            const string newPersonalPassword = "NewStrong@Pass2026";
            string changeCsrf = await TestHelpers.GetAntiForgeryTokenAsync(userClient, "/Auth/ChangePassword");
            var changeForm = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["CurrentPassword"] = tempPassword,
                ["NewPassword"] = newPersonalPassword,
                ["ConfirmNewPassword"] = newPersonalPassword,
                ["__RequestVerificationToken"] = changeCsrf
            });

            var changeRes = await userClient.PostAsync("/Auth/ChangePassword", changeForm);
            Assert.Equal(HttpStatusCode.Redirect, changeRes.StatusCode);

            // Sau khi đổi mật khẩu, cờ MustChangePassword = false và vào /Home/Index bình thường (200 OK)
            var afterChangeHome = await userClient.GetAsync("/Home/Index");
            Assert.Equal(HttpStatusCode.OK, afterChangeHome.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var updatedUser = await db.Users.SingleAsync(u => u.Username == provisionedUsername);
                Assert.False(updatedUser.MustChangePassword);
            }
        }

        [Fact]
        public async Task DonTu_Blocks_Manager_SelfApproval_And_Audits_SuperAdmin_SelfApproval()
        {
            var (mgrUserId, mgrUsername) = await CreateUserWithRoleAsync("HR_MANAGER", linkToSampleEmployee: true);
            int selfLeaveId;
            int adminSelfLeaveId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var mgrUser = await db.Users.FirstAsync(u => u.Id == mgrUserId);
                var adminUser = await db.Users.FirstAsync(u => u.Username == "admin");
                int sampleEmpId = mgrUser.EmployeeId!.Value;

                var mgrLeave = new LeaveRequest
                {
                    RequestCode = "LR-SELF-" + Random.Shared.Next(1000, 9999),
                    EmployeeId = sampleEmpId,
                    CreatedByUserId = mgrUserId,
                    RequestType = "NGHI_PHEP_NAM",
                    FromDate = VietnamTime.Today.AddDays(1),
                    ToDate = VietnamTime.Today.AddDays(2),
                    TotalDays = 2,
                    Reason = "Đơn tự tạo để kiểm thử chặn tự duyệt",
                    Status = LeaveRequestStatus.PendingApproval,
                    CreatedAt = VietnamTime.Now
                };

                var adminLeave = new LeaveRequest
                {
                    RequestCode = "LR-ADM-" + Random.Shared.Next(1000, 9999),
                    EmployeeId = sampleEmpId,
                    CreatedByUserId = adminUser.Id,
                    RequestType = "NGHI_PHEP_NAM",
                    FromDate = VietnamTime.Today.AddDays(3),
                    ToDate = VietnamTime.Today.AddDays(3),
                    TotalDays = 1,
                    Reason = "Đơn do SuperAdmin tạo để kiểm thử AuditLog",
                    Status = LeaveRequestStatus.PendingApproval,
                    CreatedAt = VietnamTime.Now
                };

                db.LeaveRequests.AddRange(mgrLeave, adminLeave);
                await db.SaveChangesAsync();
                selfLeaveId = mgrLeave.Id;
                adminSelfLeaveId = adminLeave.Id;
            }

            // 1. HR_MANAGER cố tự duyệt đơn của chính mình -> bị chặn 403 Forbidden
            using var mgrClient = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(mgrClient, mgrUsername, TestHelpers.Password);
            string mgrCsrf = await TestHelpers.GetAntiForgeryTokenAsync(mgrClient, "/DonTu/Index");

            using var selfApproveReq = new HttpRequestMessage(HttpMethod.Post, "/DonTu/Duyet")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["id"] = selfLeaveId.ToString(),
                    ["level"] = "TP",
                    ["note"] = "Tự duyệt",
                    ["__RequestVerificationToken"] = mgrCsrf
                })
            };
            selfApproveReq.Headers.Add("X-Requested-With", "XMLHttpRequest");

            using var selfApproveRes = await mgrClient.SendAsync(selfApproveReq);
            Assert.Equal(HttpStatusCode.Forbidden, selfApproveRes.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var unchanged = await db.LeaveRequests.FindAsync(selfLeaveId);
                Assert.Equal(LeaveRequestStatus.PendingApproval, unchanged!.Status);
            }

            // 2. SuperAdmin duyệt đơn do chính SuperAdmin tạo -> thành công và bắt buộc ghi SELF_APPROVE_BY_SUPERADMIN vào AuditLogs
            using var adminClient = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(adminClient, "admin", TestAppFactory.AdminPasswordForTests);
            string adminCsrf = await TestHelpers.GetAntiForgeryTokenAsync(adminClient, "/DonTu/Index");

            using var adminApproveReq = new HttpRequestMessage(HttpMethod.Post, "/DonTu/Duyet")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["id"] = adminSelfLeaveId.ToString(),
                    ["level"] = "GD",
                    ["note"] = "Duyệt khẩn cấp",
                    ["__RequestVerificationToken"] = adminCsrf
                })
            };
            adminApproveReq.Headers.Add("X-Requested-With", "XMLHttpRequest");

            using var adminApproveRes = await adminClient.SendAsync(adminApproveReq);
            Assert.Equal(HttpStatusCode.OK, adminApproveRes.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                bool hasAudit = await db.AuditLogs.AnyAsync(a =>
                    a.Action == "SELF_APPROVE_BY_SUPERADMIN" &&
                    a.EntityName == "LeaveRequest" &&
                    a.RecordId == adminSelfLeaveId.ToString());
                Assert.True(hasAudit);
            }
        }

        [Fact]
        public async Task ProtectedAjaxEndpoints_And_PosDiscountCeiling_Block_Unauthorized_And_ExcessiveDiscount()
        {
            // 1. EMPLOYEE không có quyền order.pos / scm.supplier.view gọi AJAX bị chặn 403
            var (_, empUsername) = await CreateUserWithRoleAsync("EMPLOYEE", linkToSampleEmployee: false);
            using var empClient = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(empClient, empUsername, TestHelpers.Password);

            using var posSearchReq = new HttpRequestMessage(HttpMethod.Get, "/BanHang/TimKiemSanPhamPos?keyword=a&branchId=1");
            posSearchReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var posSearchRes = await empClient.SendAsync(posSearchReq);
            Assert.Equal(HttpStatusCode.Forbidden, posSearchRes.StatusCode);

            using var supplierReq = new HttpRequestMessage(HttpMethod.Get, "/Supplier/GetSupplier?id=1");
            supplierReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var supplierRes = await empClient.SendAsync(supplierReq);
            Assert.Equal(HttpStatusCode.Forbidden, supplierRes.StatusCode);

            // 2. SALES_STAFF cố tình gửi DiscountAmount vượt trần 10% lên Server tại XuatDonPOS -> bị chặn 400 BadRequest
            var (salesUserId, salesUsername) = await CreateUserWithRoleAsync("SALES_STAFF", linkToSampleEmployee: false);
            int branchId;
            int productId;
            decimal salePrice;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var inv = await db.Inventories.Include(i => i.Product).FirstAsync(i => i.StockQuantity > 0 && i.Product.IsActive);
                branchId = inv.BranchId;
                productId = inv.ProductId;
                salePrice = inv.Product.SalePrice;

                var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                await cartService.AddToCartAsync(productId, 1, branchId, salesUserId, "pos-test-session");
            }

            using var salesClient = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(salesClient, salesUsername, TestHelpers.Password);
            string posCsrf = await TestHelpers.GetAntiForgeryTokenAsync(salesClient, $"/BanHang/POS?branchId={branchId}");

            decimal excessiveDiscount = Math.Round(salePrice * 0.40m, 0); // 40% > trần 10% của thu ngân
            using var checkoutReq = new HttpRequestMessage(HttpMethod.Post, "/BanHang/XuatDonPOS")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["BranchId"] = branchId.ToString(),
                    ["CustomerName"] = "Khách lẻ kiểm thử",
                    ["PaymentMethod"] = "CASH",
                    ["DiscountAmount"] = excessiveDiscount.ToString("0"),
                    ["Note"] = "Thử vượt trần giảm giá",
                    ["__RequestVerificationToken"] = posCsrf
                })
            };
            checkoutReq.Headers.Add("X-Requested-With", "XMLHttpRequest");

            using var checkoutRes = await salesClient.SendAsync(checkoutReq);
            Assert.Equal(HttpStatusCode.BadRequest, checkoutRes.StatusCode);
            string errJson = await checkoutRes.Content.ReadAsStringAsync();
            Assert.Contains("DISCOUNT_CEILING_EXCEEDED", errJson);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // HELPERS NỘI BỘ CHO KIỂM THỬ
        // ═══════════════════════════════════════════════════════════════════════════

        private async Task<(int UserId, string Username)> CreateUserWithRoleAsync(string roleCode, bool linkToSampleEmployee)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            int? empId = null;
            if (linkToSampleEmployee)
            {
                var dept = await db.Departments.FirstAsync();
                var pos = await db.Positions.FirstAsync();
                var emp = new Employee
                {
                    EmployeeCode = "T" + Random.Shared.Next(100000, 999999),
                    FullName = $"Nhân sự kiểm thử ({roleCode})",
                    Email = $"test_{Guid.NewGuid():N}@tienphong.vn",
                    Phone = "09" + Random.Shared.Next(10000000, 99999999),
                    DepartmentId = dept.Id,
                    PositionId = pos.Id,
                    Status = "Active",
                    HireDate = VietnamTime.Today,
                    CreatedAt = VietnamTime.Now
                };
                db.Employees.Add(emp);
                await db.SaveChangesAsync();
                empId = emp.Id;
            }

            string salt = MatKhauHelper.GenerateSalt();
            var user = new User
            {
                Username = $"{roleCode.ToLowerInvariant()}_{Guid.NewGuid():N}"[..20],
                PasswordSalt = salt,
                PasswordHash = MatKhauHelper.HashPassword(TestHelpers.Password, salt),
                EmployeeId = empId,
                IsActive = true,
                IsSuperAdmin = false,
                MustChangePassword = false,
                CreatedAt = VietnamTime.Now
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var role = await db.Roles.FirstAsync(r => r.Code == roleCode);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync();

            return (user.Id, user.Username);
        }

        private sealed class FakeCurrentUser : ICurrentUserService
        {
            private readonly HashSet<string> _roles;

            public FakeCurrentUser(
                int? userId,
                int? employeeId = null,
                int? branchId = null,
                int? departmentId = null,
                bool isSuperAdmin = false,
                IEnumerable<string>? roles = null)
            {
                IsAuthenticated = userId.HasValue;
                UserId = userId;
                EmployeeId = employeeId;
                BranchId = branchId;
                DepartmentId = departmentId;
                IsSuperAdmin = isSuperAdmin;
                _roles = new HashSet<string>(roles ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            }

            public bool IsAuthenticated { get; }
            public int? UserId { get; }
            public string? Username => IsAuthenticated ? $"user_{UserId}" : null;
            public string? FullName => Username;
            public int? EmployeeId { get; }
            public int? BranchId { get; }
            public int? DepartmentId { get; }
            public bool IsSuperAdmin { get; }
            public bool MustChangePassword => false;
            public string? IpAddress => "127.0.0.1";
            public string? CorrelationId => "test-corr";
            public IReadOnlyList<string> Roles => _roles.ToList();

            public bool IsInRole(string roleCode) => _roles.Contains(roleCode);
            public int GetRequiredUserId() => UserId ?? throw new UnauthorizedAccessException();
        }
    }
}
