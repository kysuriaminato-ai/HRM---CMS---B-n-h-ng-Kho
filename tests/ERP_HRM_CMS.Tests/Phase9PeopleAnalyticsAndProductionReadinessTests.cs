using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Controllers;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class Phase9PeopleAnalyticsAndProductionReadinessTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public Phase9PeopleAnalyticsAndProductionReadinessTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        #region Helper Methods

        private async Task<(Employee emp, Department dept, Position pos)> CreateTestEmployeeAsync(string prefix = "P9")
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var dept = await db.Departments.FirstOrDefaultAsync() ?? new Department
            {
                Code = "DEPT_P9",
                Name = "Phòng Phân Tích Dữ Liệu",
                BranchId = 1,
                IsActive = true
            };
            if (dept.Id == 0)
            {
                db.Departments.Add(dept);
                await db.SaveChangesAsync();
            }

            var pos = await db.Positions.FirstOrDefaultAsync() ?? new Position
            {
                Code = "POS_P9",
                Name = "Chuyên viên People Analytics",
                IsActive = true
            };
            if (pos.Id == 0)
            {
                db.Positions.Add(pos);
                await db.SaveChangesAsync();
            }

            var emp = new Employee
            {
                EmployeeCode = $"{prefix}-{Guid.NewGuid():N}"[..10],
                FullName = $"Nhân Viên Analytics {prefix} " + Guid.NewGuid().ToString("N")[..6],
                Email = $"analytics.{Guid.NewGuid():N}[..8]@company.vn",
                Phone = "0988776655",
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                Status = "Active",
                HireDate = DateTime.Today.AddYears(-2),
                IdentityCard = "enc:v1:099887766554",
                CreatedAt = VietnamTime.Now
            };

            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            return (emp, dept, pos);
        }

        #endregion

        #region 1. People Analytics Service Tests

        [Fact]
        public async Task GetExecutiveAnalyticsAsync_ReturnsValidDto_WithAll6KpiCardsAndVisualizations()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IPeopleAnalyticsService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "SUPERADMIN")
            }, "TestAuth"));
            httpContextAccessor.HttpContext = new DefaultHttpContext { User = adminUser };

            var (emp, dept, pos) = await CreateTestEmployeeAsync("P9_KPI");

            // Seed some candidate, attendance, contract, and certificate data to test aggregations
            var candidate = new Candidate
            {
                JobPostingId = 1,
                Code = "UV-P9-" + Guid.NewGuid().ToString("N")[..6],
                FullName = "Ứng Viên Tuyển Dụng P9",
                Email = "cand.p9@example.com",
                Phone = "0900112233",
                Status = "Hired",
                CreatedAt = VietnamTime.Now
            };
            db.Candidates.Add(candidate);

            var contract = new Contract
            {
                EmployeeId = emp.Id,
                ContractNumber = "HD-P9-" + Guid.NewGuid().ToString("N")[..6],
                ContractType = "Xác định thời hạn",
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddDays(15), // Expiring in 15 days
                BasicSalary = 20000000m,
                Status = "Active"
            };
            db.Contracts.Add(contract);

            var cert = new EmployeeCertificate
            {
                EmployeeId = emp.Id,
                CertificateName = "PMP Certification",
                IssuedBy = "PMI",
                IssuedDate = DateTime.Today.AddYears(-3),
                ExpiryDate = DateTime.Today.AddDays(20) // Expiring in 20 days
            };
            db.EmployeeCertificates.Add(cert);

            await db.SaveChangesAsync();

            var filter = new PeopleAnalyticsFilterDto
            {
                Year = DateTime.Today.Year,
                Quarter = 0,
                DepartmentId = dept.Id
            };

            // Act
            var result = await analyticsService.GetExecutiveAnalyticsAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalHeadcount >= 1);
            Assert.True(result.ActiveHeadcount >= 1);
            Assert.True(result.BudgetedHeadcount >= result.ActiveHeadcount);
            Assert.True(result.AttendanceRate >= 0 && result.AttendanceRate <= 100);
            Assert.True(result.TurnoverRate >= 0);
            Assert.True(result.CurrentGrossPayroll > 0);

            // Chart 1: 12 Months
            Assert.NotNull(result.HeadcountMovement12Months);
            Assert.Equal(12, result.HeadcountMovement12Months.Count);

            // Chart 2: Department breakdown
            Assert.NotNull(result.DepartmentPayrollBreakdown);
            Assert.Contains(result.DepartmentPayrollBreakdown, d => d.DepartmentId == dept.Id);

            // Chart 3: Recruitment funnel
            Assert.NotNull(result.RecruitmentFunnel);
            Assert.True(result.RecruitmentFunnel.HiredCount >= 1);

            // Chart 4: Seniority pyramid
            Assert.NotNull(result.SeniorityPyramid);
            Assert.True(result.SeniorityPyramid.Total >= 1);

            // Early Action Matrix
            Assert.NotNull(result.ExpiringContracts);
            Assert.Contains(result.ExpiringContracts, c => c.EmployeeId == emp.Id);

            Assert.NotNull(result.ExpiringCertificates);
            Assert.Contains(result.ExpiringCertificates, c => c.EmployeeId == emp.Id);
        }

        [Fact]
        public async Task ExportExecutiveReportExcel_GeneratesValidMultiSheetExcelWorkbook()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IPeopleAnalyticsService>();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "SUPERADMIN")
            }, "TestAuth"));
            httpContextAccessor.HttpContext = new DefaultHttpContext { User = adminUser };

            var filter = new PeopleAnalyticsFilterDto
            {
                Year = DateTime.Today.Year,
                Quarter = 0
            };

            var data = await analyticsService.GetExecutiveAnalyticsAsync(filter);

            // Act
            var excelBytes = analyticsService.ExportExecutiveReportExcel(data);

            // Assert
            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 1000, "File Excel phải có dung lượng lớn hơn 1KB.");

            // Verify ClosedXML sheet structure
            using var ms = new MemoryStream(excelBytes);
            using var workbook = new XLWorkbook(ms);

            Assert.Equal(4, workbook.Worksheets.Count);
            Assert.NotNull(workbook.Worksheet("Tổng Quan Điều Hành"));
            Assert.NotNull(workbook.Worksheet("Biến Động Nhân Sự"));
            Assert.NotNull(workbook.Worksheet("Quỹ Lương & Chi Phí"));
            Assert.NotNull(workbook.Worksheet("Tuyển Dụng & Cảnh Báo"));

            var sheet1 = workbook.Worksheet("Tổng Quan Điều Hành");
            Assert.Contains("BÁO CÁO PHÂN TÍCH NGUỒN NHÂN LỰC CẤP ĐIỀU HÀNH", sheet1.Cell("A1").GetString());
        }

        #endregion

        #region 2. Controller & UI Action Tests

        [Fact]
        public async Task NhanSuController_BaoCao_ReturnsViewResultWithModel()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var empService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IPeopleAnalyticsService>();
            var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "SUPERADMIN")
            }, "TestAuth"));
            httpContextAccessor.HttpContext = new DefaultHttpContext { User = adminUser };

            var controller = new NhanSuController(
                empService,
                analyticsService,
                deptService,
                currentUser
            );

            // Act
            var actionResult = await controller.BaoCao(DateTime.Today.Year, 0, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(actionResult);
            var model = Assert.IsType<ExecutivePeopleAnalyticsDto>(viewResult.Model);
            Assert.NotNull(model);
            Assert.True(model.HeadcountMovement12Months.Count == 12);
        }

        [Fact]
        public async Task NhanSuController_XuatBaoCaoExcel_ReturnsFileResult()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var empService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IPeopleAnalyticsService>();
            var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "SUPERADMIN")
            }, "TestAuth"));
            httpContextAccessor.HttpContext = new DefaultHttpContext { User = adminUser };

            var controller = new NhanSuController(
                empService,
                analyticsService,
                deptService,
                currentUser
            );

            // Act
            var actionResult = await controller.XuatBaoCaoExcel(DateTime.Today.Year, 0, null, null);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(actionResult);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
            Assert.Contains("Executive_People_Analytics", fileResult.FileDownloadName);
            Assert.True(fileResult.FileContents.Length > 1000);
        }

        #endregion

        #region 3. Production Readiness & Security Architecture Audit

        [Fact]
        public void ProductionReadiness_AllAdministrativeControllers_AreSecuredWithAuthorizeOrCustomAuth()
        {
            // Rà soát phản xạ (Reflection) toàn bộ Controller để đảm bảo không có Controller quản trị nào bị hở phân quyền
            var assembly = typeof(NhanSuController).Assembly;
            var controllerTypes = assembly.GetTypes()
                .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            var publicControllers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "PublicController",
                "AuthController",
                "ErrorController",
                "HomeController",
                "GioHangController"
            };

            var unsecureControllers = new List<string>();

            foreach (var controller in controllerTypes)
            {
                if (publicControllers.Contains(controller.Name))
                    continue;

                bool hasControllerAuthorize = controller.GetCustomAttribute<AuthorizeAttribute>(true) != null ||
                                              controller.GetCustomAttributes().Any(a => a.GetType().Name.Contains("KiemTraQuyen") || a.GetType().Name.Contains("Authorize"));

                if (!hasControllerAuthorize)
                {
                    // Kiểm tra xem tất cả các Action public có gắn attribute bảo vệ không
                    var publicActions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(m => typeof(IActionResult).IsAssignableFrom(m.ReturnType) || typeof(Task<IActionResult>).IsAssignableFrom(m.ReturnType))
                        .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() == null)
                        .ToList();

                    bool allActionsSecured = publicActions.All(m =>
                        m.GetCustomAttribute<AuthorizeAttribute>(true) != null ||
                        m.GetCustomAttributes().Any(a => a.GetType().Name.Contains("KiemTraQuyen")));

                    if (!allActionsSecured && publicActions.Count > 0)
                    {
                        unsecureControllers.Add(controller.Name);
                    }
                }
            }

            Assert.True(unsecureControllers.Count == 0,
                $"Phát hiện các Controller quản trị chưa được cấu hình Authorize hoặc KiemTraQuyen: {string.Join(", ", unsecureControllers)}");
        }

        [Fact]
        public void ProductionReadiness_DatabaseHealthScript_ContainsAllEnterpriseHrmTablesAndIndexes()
        {
            // Verify presence and content of the master health verification script
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var scriptPath = Path.Combine(repoRoot, "db", "scripts", "V6_99_Verify_Schema_Health.sql");

            Assert.True(File.Exists(scriptPath), $"Không tìm thấy file kịch bản SQL kiểm định sức khỏe: {scriptPath}");

            var scriptContent = File.ReadAllText(scriptPath);

            var requiredTables = new[]
            {
                "Employees",
                "Departments",
                "Positions",
                "Contracts",
                "AttendanceDailies",
                "LeaveRequests",
                "EmployeeLeaveBalances",
                "HrDecisions",
                "EmployeeAssets",
                "JobPostings",
                "Candidates",
                "KpiReviews",
                "Trainings",
                "PayrollPeriods"
            };

            foreach (var table in requiredTables)
            {
                Assert.Contains(table, scriptContent);
            }

            // Verify performance indexes are checked
            Assert.Contains("IX_Employees_EmployeeCode", scriptContent);
            Assert.Contains("IX_AttendanceDailies_Employee_Date", scriptContent);
            Assert.Contains("IX_Contracts_EmployeeId", scriptContent);
        }

        #endregion
    }
}
