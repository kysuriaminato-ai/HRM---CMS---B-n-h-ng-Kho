using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Bộ kiểm thử xác thực Phiên 3: Nâng cấp Theme "Nordic People & Indigo Slate",
    /// Hiệu ứng động/tĩnh toàn diện & Hệ thống CSS HRM Workspace.
    /// </summary>
    public class UiPhase3HrmThemeTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public UiPhase3HrmThemeTests(TestAppFactory factory) => _factory = factory;

        [Fact]
        public void EnterpriseTheme_Contains_WcagTokens_TabularNums_And_DensityClasses()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "ERP_HRM_CMS.sln")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            Assert.NotNull(dir);

            var cssPath = Path.Combine(dir, "wwwroot", "css", "enterprise-theme.css");
            Assert.True(File.Exists(cssPath), "enterprise-theme.css phải tồn tại trong wwwroot/css");

            var cssText = File.ReadAllText(cssPath);

            // 1. Kiểm tra tokens WCAG AA
            Assert.Contains("--erp-text-muted", cssText);
            Assert.Contains("--erp-success-text", cssText);
            Assert.Contains("--erp-warning-text", cssText);
            Assert.Contains("--erp-danger-text", cssText);
            Assert.Contains("--erp-info-text", cssText);

            // 2. Kiểm tra Tabular Nums
            Assert.Contains("tabular-nums", cssText);
            Assert.Contains("font-variant-numeric: tabular-nums", cssText);
            Assert.Contains("font-feature-settings: \"tnum\"", cssText);

            // 3. Kiểm tra Density & Sticky Columns
            Assert.Contains("data-density=\"compact\"", cssText);
            Assert.Contains("--erp-table-cell-py", cssText);
            Assert.Contains("sticky-thead", cssText);
            Assert.Contains("sticky-col-1", cssText);
            Assert.Contains("sticky-col-2", cssText);
            Assert.Contains("sticky-col-actions", cssText);
        }

        [Fact]
        public void HrmWorkspace_Css_Contains_Bento_OrgChart_Kanban_And_ShiftMatrix()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "ERP_HRM_CMS.sln")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            Assert.NotNull(dir);

            var cssPath = Path.Combine(dir, "wwwroot", "css", "hrm-workspace.css");
            Assert.True(File.Exists(cssPath), "hrm-workspace.css phải tồn tại trong wwwroot/css");

            var cssText = File.ReadAllText(cssPath);

            // 1. Bento Dashboard Grid
            Assert.Contains(".erp-bento-grid", cssText);
            Assert.Contains(".bento-card", cssText);
            Assert.Contains(".border-glow", cssText);
            Assert.Contains(".bento-shimmer", cssText);

            // 2. Interactive Org Chart
            Assert.Contains(".erp-org-viewport", cssText);
            Assert.Contains(".org-tree", cssText);
            Assert.Contains(".org-node-card", cssText);
            Assert.Contains(".org-node-avatar", cssText);
            Assert.Contains(".org-toggle-btn", cssText);

            // 3. ATS Kanban Board
            Assert.Contains(".erp-kanban-board", cssText);
            Assert.Contains(".kanban-column", cssText);
            Assert.Contains(".kanban-card", cssText);
            Assert.Contains(".is-dragging", cssText);
            Assert.Contains(".kanban-counter", cssText);

            // 4. Shift Matrix Grid
            Assert.Contains(".erp-shift-matrix", cssText);
            Assert.Contains(".erp-shift-cell", cssText);
            Assert.Contains(".shift-ontime", cssText);
            Assert.Contains(".shift-late", cssText);
            Assert.Contains(".shift-leave", cssText);
            Assert.Contains(".shift-ot", cssText);
            Assert.Contains(".shift-legend-bar", cssText);
        }

        [Fact]
        public void AdminPrint_Css_Contains_A4_PrintRules_And_SignatureBoxes()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "ERP_HRM_CMS.sln")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            Assert.NotNull(dir);

            var cssPath = Path.Combine(dir, "wwwroot", "css", "admin-print.css");
            Assert.True(File.Exists(cssPath), "admin-print.css phải tồn tại trong wwwroot/css");

            var cssText = File.ReadAllText(cssPath);

            Assert.Contains("@media print", cssText);
            Assert.Contains(".erp-sidebar", cssText);
            Assert.Contains(".erp-topbar", cssText);
            Assert.Contains("display: none !important", cssText);
            Assert.Contains(".print-document", cssText);
            Assert.Contains(".print-signatures", cssText);
            Assert.Contains(".print-signature-box", cssText);
        }

        [Fact]
        public void ClientScripts_Contain_Density_OrgChart_And_Kanban_Handlers()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "ERP_HRM_CMS.sln")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            Assert.NotNull(dir);

            var themeJsPath = Path.Combine(dir, "wwwroot", "js", "enterprise-theme.js");
            var adminUiJsPath = Path.Combine(dir, "wwwroot", "js", "admin-ui.js");

            Assert.True(File.Exists(themeJsPath));
            Assert.True(File.Exists(adminUiJsPath));

            var themeJs = File.ReadAllText(themeJsPath);
            var adminUiJs = File.ReadAllText(adminUiJsPath);

            // Density switcher trong enterprise-theme.js
            Assert.Contains("initDensity", themeJs);
            Assert.Contains("applyDensity", themeJs);
            Assert.Contains("toggleDensity", themeJs);
            Assert.Contains("erp_density", themeJs);

            // Org Chart & Kanban trong admin-ui.js
            Assert.Contains("initOrgChart", adminUiJs);
            Assert.Contains("initKanbanBoard", adminUiJs);
            Assert.Contains("ERP.orgChart", adminUiJs);
            Assert.Contains("ERP.kanban", adminUiJs);
        }

        [Fact]
        public async Task Layout_Includes_HrmWorkspaceCss_DensityBtn_And_HrmSidebar()
        {
            // Kiểm tra view _Layout.cshtml chứa các thẻ link và thành phần HRM mới
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "ERP_HRM_CMS.sln")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            Assert.NotNull(dir);

            var layoutPath = Path.Combine(dir, "Views", "Shared", "_Layout.cshtml");
            var layoutHtml = File.ReadAllText(layoutPath);

            Assert.Contains("hrm-workspace.css", layoutHtml);
            Assert.Contains("admin-print.css", layoutHtml);
            Assert.Contains("densityToggleBtn", layoutHtml);
            Assert.Contains("window.toggleDensity()", layoutHtml);
            Assert.Contains("data-density", layoutHtml);

            // Kiểm tra HRM navigation menu
            Assert.Contains("/NhanSu/Index", layoutHtml);
            Assert.Contains("/PhongBan/Index", layoutHtml);
            Assert.Contains("/HopDong/Index", layoutHtml);
            Assert.Contains("/ChamCong/Index", layoutHtml);
            Assert.Contains("/ChamCong/BangCongThang", layoutHtml);
            Assert.Contains("/DonTu/Index", layoutHtml);
        }

        [Fact]
        public async Task Styleguide_Renders_HrmWorkspaceSection_For_SuperAdmin()
        {
            using var client = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(client, "admin", TestAppFactory.AdminPasswordForTests);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            using var res = await client.GetAsync("/UI/Styleguide");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var html = await res.Content.ReadAsStringAsync();

            // Kiểm tra các phần mới của HRM Workspace xuất hiện trên Styleguide
            Assert.Contains("HRM Workspace Components", html);
            Assert.Contains("Table &amp; UI Density Switcher", html);
            Assert.Contains("Interactive Org Chart Node", html);
            Assert.Contains("Calendar &amp; Shift Matrix Grid Cells", html);
            Assert.Contains("ATS Kanban Board", html);
            Assert.Contains("Bento Dashboard Grid", html);
        }
    }
}

