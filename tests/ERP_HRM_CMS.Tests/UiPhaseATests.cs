using System.Net;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class UiPhaseATests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public UiPhaseATests(TestAppFactory factory) => _factory = factory;

        [Fact]
        public void WorkflowStatusUiHelper_Maps_All_WorkflowStatuses_Enums_And_Codes()
        {
            var validVariants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "primary", "success", "warning", "danger", "info", "neutral"
            };

            void AssertEnumMapping<TEnum>(IReadOnlyDictionary<TEnum, string> codeMap, string workflowName)
                where TEnum : struct, Enum
            {
                foreach (var value in Enum.GetValues<TEnum>())
                {
                    var fromEnum = WorkflowStatusUiHelper.ResolveObject(value, workflowName);
                    Assert.False(string.IsNullOrWhiteSpace(fromEnum.Label));
                    Assert.Contains(fromEnum.Variant, validVariants);
                    Assert.StartsWith("bi-", fromEnum.Icon);

                    string dbCode = WorkflowStatusCodes.ToCode(codeMap, value);
                    var fromCode = WorkflowStatusUiHelper.ResolveCode(dbCode, workflowName);
                    Assert.Equal(fromEnum.Label, fromCode.Label);
                    Assert.Equal(fromEnum.Variant, fromCode.Variant);
                }
            }

            AssertEnumMapping(WorkflowStatusCodes.PurchaseOrder, "PurchaseOrder");
            AssertEnumMapping(WorkflowStatusCodes.Debt, "Debt");
            AssertEnumMapping(WorkflowStatusCodes.LeaveRequest, "LeaveRequest");
            AssertEnumMapping(WorkflowStatusCodes.PayrollPeriod, "PayrollPeriod");
            AssertEnumMapping(WorkflowStatusCodes.SalarySlip, "SalarySlip");
        }

        [Fact]
        public async Task Styleguide_Denies_NonSuperAdmin_User_With_403()
        {
            int userId = await TestHelpers.CreateUserAsync(_factory.Services, isActive: true);
            string username = await TestHelpers.GetUsernameAsync(_factory.Services, userId);

            using var client = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(client, username, TestHelpers.Password);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            using var pageRes = await client.GetAsync("/UI/Styleguide");
            var pageHtml = WebUtility.HtmlDecode(await pageRes.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.Forbidden, pageRes.StatusCode);
            Assert.Contains("Super Admin", pageHtml, StringComparison.OrdinalIgnoreCase);

            using var ajaxReq = new HttpRequestMessage(HttpMethod.Get, "/UI/StyleguideTableData");
            ajaxReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var ajaxRes = await client.SendAsync(ajaxReq);
            Assert.Equal(HttpStatusCode.Forbidden, ajaxRes.StatusCode);
        }

        [Fact]
        public async Task Styleguide_Allows_SuperAdmin_And_Serves_Ajax_DataTable()
        {
            string superAdminUser = await CreateSuperAdminUserAsync(_factory.Services);

            using var client = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(client, superAdminUser, TestHelpers.Password);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            using var res = await client.GetAsync("/UI/Styleguide");
            string html = WebUtility.HtmlDecode(await res.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Contains("data-module=\"system\"", html);
            Assert.Contains("erp-page-header", html);
            Assert.Contains("stat-card", html);
            Assert.Contains("erp-filter-bar", html);
            Assert.Contains("sgAjaxTable", html);
            Assert.Contains("erp-tabs", html);
            Assert.Contains("erp-timeline", html);
            Assert.Contains("erp-drawer", html);
            Assert.Contains("erp-confirm-modal", html);

            // Kiểm tra lọc, sắp xếp và phân trang AJAX của DataTable
            using var tableRes = await client.GetAsync("/UI/StyleguideTableData?keyword=PO-2026&moduleFilter=warehouse&sortBy=amount&sortDir=desc&page=1&pageSize=5");
            string tableHtml = WebUtility.HtmlDecode(await tableRes.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.OK, tableRes.StatusCode);
            Assert.Contains("PO-2026-001", tableHtml);
            Assert.DoesNotContain("LV-2026-201", tableHtml);
        }

        [Fact]
        public async Task ErrorPages_403_404_500_And_AccessDenied_Preserve_StatusCode_And_Use_Theme_Tokens()
        {
            using var client = TestHelpers.NewClient(_factory);

            using var res404 = await client.GetAsync("/Home/Error?statusCode=404");
            string html404 = WebUtility.HtmlDecode(await res404.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.NotFound, res404.StatusCode);
            Assert.Contains("HTTP 404", html404);
            Assert.Contains("enterprise-theme.css", html404);

            using var res403 = await client.GetAsync("/Home/Error?statusCode=403");
            string html403 = WebUtility.HtmlDecode(await res403.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.Forbidden, res403.StatusCode);
            Assert.Contains("HTTP 403", html403);

            using var res500 = await client.GetAsync("/Home/Error?statusCode=500");
            string html500 = WebUtility.HtmlDecode(await res500.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.InternalServerError, res500.StatusCode);
            Assert.Contains("HTTP 500", html500);

            using var resDenied = await client.GetAsync("/Auth/AccessDenied?returnUrl=%2FUI%2FStyleguide");
            string htmlDenied = WebUtility.HtmlDecode(await resDenied.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.Forbidden, resDenied.StatusCode);
            Assert.Contains("/UI/Styleguide", htmlDenied);
            Assert.Contains("enterprise-theme.css", htmlDenied);

            using var resNoPerm = await client.GetAsync("/Home/KhongCoQuyen");
            Assert.Equal(HttpStatusCode.Forbidden, resNoPerm.StatusCode);
        }

        private static async Task<string> CreateSuperAdminUserAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string salt = MatKhauHelper.GenerateSalt();
            var user = new User
            {
                Username = "sa_" + Guid.NewGuid().ToString("N")[..10],
                PasswordSalt = salt,
                PasswordHash = MatKhauHelper.HashPassword(TestHelpers.Password, salt),
                IsActive = true,
                IsSuperAdmin = true,
                CreatedAt = VietnamTime.Now
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Username;
        }
    }
}

