using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Enums;
using ERP_HRM_CMS.Controllers;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services;
using ERP_HRM_CMS.Services.HRM;
using ERP_HRM_CMS.Services.Payroll;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class Phase8PerformanceLmsAndPayrollTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public Phase8PerformanceLmsAndPayrollTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        #region Helper Methods

        private async Task<(Employee emp, Department dept, Position pos)> CreateTestEmployeeAsync(string prefix = "P8")
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var dept = await db.Departments.FirstOrDefaultAsync() ?? new Department
            {
                Code = "HR",
                Name = "Phòng Nhân Sự",
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
                Code = "HR_STAFF",
                Name = "Chuyên viên Nhân sự",
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
                FullName = $"Nhân Viên {prefix} " + Guid.NewGuid().ToString("N")[..6],
                Email = $"emp.{Guid.NewGuid():N}[..8]@company.vn",
                Phone = "0912345678",
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                Status = "Active",
                HireDate = DateTime.Today.AddYears(-1),
                IdentityCard = "enc:v1:001234567890",
                CreatedAt = VietnamTime.Now
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            var contract = new Contract
            {
                ContractNumber = $"HD-{emp.EmployeeCode}",
                EmployeeId = emp.Id,
                ContractType = "Official",
                StartDate = emp.HireDate,
                EndDate = emp.HireDate.AddYears(3),
                BasicSalary = 20000000,
                InsuranceSalary = 20000000,
                Status = "Active",
                CreatedAt = VietnamTime.Now
            };
            db.Contracts.Add(contract);

            var detail = new EmployeeDetail
            {
                EmployeeId = emp.Id,
                BankAccountNumber = "enc:v1:9876543210",
                BankName = "Vietcombank",
                TaxIdentificationNumber = "8888888888"
            };
            db.EmployeeDetails.Add(detail);
            await db.SaveChangesAsync();

            return (emp, dept, pos);
        }

        #endregion

        #region 1. KPI Criteria Weight Validator Tests

        [Fact]
        public void KpiCriteriaWeightValidation_RejectsSumNot100_Accepts100()
        {
            using var scope = _factory.Services.CreateScope();
            var kpiService = scope.ServiceProvider.GetRequiredService<IPerformanceReviewService>();

            // Case 1: Tổng trọng số = 90% (thiếu)
            var criteriaUnder100 = new List<KpiCriteriaDto>
            {
                new() { CriteriaName = "Chuyên môn", Weight = 40 },
                new() { CriteriaName = "Chất lượng", Weight = 30 },
                new() { CriteriaName = "Kỷ luật", Weight = 20 }
            };
            var resultUnder = kpiService.ValidateCriteriaWeights(criteriaUnder100);
            Assert.False(resultUnder.IsValid);
            Assert.Contains("100%", resultUnder.ErrorMessage);

            // Case 2: Tổng trọng số = 110% (thừa)
            var criteriaOver100 = new List<KpiCriteriaDto>
            {
                new() { CriteriaName = "Chuyên môn", Weight = 50 },
                new() { CriteriaName = "Chất lượng", Weight = 40 },
                new() { CriteriaName = "Kỷ luật", Weight = 20 }
            };
            var resultOver = kpiService.ValidateCriteriaWeights(criteriaOver100);
            Assert.False(resultOver.IsValid);
            Assert.Contains("100%", resultOver.ErrorMessage);

            // Case 3: Tổng trọng số chuẩn = 100%
            var criteria100 = new List<KpiCriteriaDto>
            {
                new() { CriteriaName = "Chuyên môn", Weight = 40 },
                new() { CriteriaName = "Chất lượng", Weight = 30 },
                new() { CriteriaName = "Kỷ luật", Weight = 15 },
                new() { CriteriaName = "Sáng tạo", Weight = 15 }
            };
            var result100 = kpiService.ValidateCriteriaWeights(criteria100);
            Assert.True(result100.IsValid);
            Assert.Null(result100.ErrorMessage);
        }

        #endregion

        #region 2. KPI 3-Step Evaluation & 9-Box Lifecycle Tests

        [Fact]
        public async Task KpiThreeStepEvaluation_LifecycleAndNineBoxCategory_Succeeds()
        {
            var (emp, dept, pos) = await CreateTestEmployeeAsync("KPI");

            using var scope = _factory.Services.CreateScope();
            var kpiService = scope.ServiceProvider.GetRequiredService<IPerformanceReviewService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Khởi tạo phiếu KPI
            var createDto = new CreateKpiReviewDto
            {
                EmployeeId = emp.Id,
                ReviewerId = 1,
                PeriodMonth = 10,
                PeriodYear = 2026,
                Criteria = new List<KpiCriteriaDto>
                {
                    new() { CriteriaName = "KPI Doanh số", Weight = 50 },
                    new() { CriteriaName = "Chất lượng dịch vụ", Weight = 30 },
                    new() { CriteriaName = "Văn hóa & Đào tạo", Weight = 20 }
                }
            };

            var createRes = await kpiService.CreateKpiReviewAsync(createDto, 1);
            Assert.True(createRes.IsSuccess);
            int kpiId = createRes.Value;
            Assert.True(kpiId > 0);

            var kpi = await kpiService.GetKpiReviewByIdAsync(kpiId);
            Assert.NotNull(kpi);
            Assert.Equal("Draft", kpi.Status);
            Assert.Equal(3, kpi.Criteria.Count);

            // 2. Bước 1: Nhân viên tự chấm
            var selfDto = new SelfEvaluateKpiDto
            {
                KpiReviewId = kpiId,
                EmployeeComment = "Hoàn thành vượt mức chỉ tiêu đề ra",
                Scores = new List<CriteriaScoreItem>
                {
                    new() { CriteriaId = kpi.Criteria[0].Id, Score = 95 },
                    new() { CriteriaId = kpi.Criteria[1].Id, Score = 90 },
                    new() { CriteriaId = kpi.Criteria[2].Id, Score = 85 }
                }
            };
            var selfRes = await kpiService.SelfEvaluateAsync(selfDto, emp.Id);
            Assert.True(selfRes.IsSuccess);

            // 3. Bước 2: Quản lý trực tiếp chấm điểm & gán điểm tiềm năng 9-Box
            var managerDto = new ManagerEvaluateKpiDto
            {
                KpiReviewId = kpiId,
                PotentialScore = 90, // Tiềm năng cao
                ManagerComment = "Nhân sự xuất sắc, kỹ năng lãnh đạo nổi bật",
                Scores = new List<CriteriaScoreItem>
                {
                    new() { CriteriaId = kpi.Criteria[0].Id, Score = 92 }, // 92 * 0.5 = 46.0
                    new() { CriteriaId = kpi.Criteria[1].Id, Score = 88 }, // 88 * 0.3 = 26.4
                    new() { CriteriaId = kpi.Criteria[2].Id, Score = 90 }  // 90 * 0.2 = 18.0 => Tổng: 90.4 (Hiệu suất cao)
                }
            };
            var managerRes = await kpiService.ManagerEvaluateAsync(managerDto, 1);
            Assert.True(managerRes.IsSuccess);

            var kpiAfterManager = await kpiService.GetKpiReviewByIdAsync(kpiId);
            Assert.NotNull(kpiAfterManager);
            Assert.Equal("Submitted", kpiAfterManager.Status);
            Assert.Equal(90.4m, kpiAfterManager.TotalScore);

            // 4. Bước 3: Ban Giám Đốc Phê duyệt xếp loại A+
            var approveDto = new ApproveKpiDto
            {
                KpiReviewId = kpiId,
                Grade = "A+",
                DirectorComment = "Đồng ý xếp loại A+ và phê duyệt thưởng theo chính sách C&B",
                IsApproved = true
            };
            var approveRes = await kpiService.ApproveKpiAsync(approveDto, 1);
            Assert.True(approveRes.IsSuccess);

            var approvedKpi = await kpiService.GetKpiReviewByIdAsync(kpiId);
            Assert.NotNull(approvedKpi);
            Assert.Equal("Approved", approvedKpi.Status);
            Assert.Equal("A+", approvedKpi.Grade);
            // Hiệu suất cao (90.4 >= 85) & Tiềm năng cao (90 >= 80) => Phân bổ 9-Box: Star (Ngôi sao)
            Assert.Equal("Star", approvedKpi.NineBoxCategory);

            // 5. Kiểm tra hiển thị trong Ma trận 9-Box
            var matrix = await kpiService.GetNineBoxMatrixAsync(10, 2026, null);
            Assert.NotNull(matrix);
            Assert.Contains(matrix.StarTalents, item => item.EmployeeId == emp.Id);
        }

        #endregion

        #region 3. LMS Training Course & Participant Auto-Upgrade Tests

        [Fact]
        public async Task LmsParticipantGrading_PassGrantsSkillAndCertificate_FailDoesNot()
        {
            var (empPassed, _, _) = await CreateTestEmployeeAsync("PASSED");
            var (empFailed, _, _) = await CreateTestEmployeeAsync("FAILED");

            using var scope = _factory.Services.CreateScope();
            var trainingService = scope.ServiceProvider.GetRequiredService<ITrainingService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Tạo khóa đào tạo LMS kèm cấu hình tự động cấp Kỹ năng & Chứng chỉ
            var courseDto = new TrainingCreateEditDto
            {
                Code = "TRN-NET8-" + Guid.NewGuid().ToString("N")[..6],
                Title = "Clean Architecture",
                TrainerName = "Technical Architect",
                Location = "Phòng Training A",
                StartDate = DateTime.Today.AddDays(-10),
                EndDate = DateTime.Today.AddDays(-2),
                Budget = 15000000,
                Status = "InProgress",
                TargetSkillName = "Clean Architecture",
                CertificateName = "Clean Architecture"
            };

            var courseRes = await trainingService.CreateTrainingAsync(courseDto, 1);
            Assert.True(courseRes.IsSuccess);
            int trainingId = courseRes.Value;
            Assert.True(trainingId > 0);

            // 2. Gán học viên vào khóa
            var assignDto = new AssignTrainingParticipantsDto
            {
                TrainingId = trainingId,
                EmployeeIds = new List<int> { empPassed.Id, empFailed.Id }
            };
            var assignRes = await trainingService.AssignParticipantsAsync(assignDto, 1);
            Assert.True(assignRes.IsSuccess);

            // 3. Sát hạch học viên 1: Điểm 85 (>= 60) -> PASSED (Tự động cấp chứng chỉ & nâng kỹ năng)
            var gradePassedDto = new GradeParticipantDto
            {
                TrainingId = trainingId,
                EmployeeId = empPassed.Id,
                Score = 85,
                Result = "Passed",
                Comment = "Nắm vững nguyên lý và hoàn thành bài thi xuất sắc",
                AutoGrantSkillOrCertificate = true
            };
            var gradePassRes = await trainingService.GradeParticipantAsync(gradePassedDto, 1);
            Assert.True(gradePassRes.IsSuccess);

            // Kiểm tra kỹ năng được tự động thêm/cập nhật vào EmployeeSkills
            var skills = await db.EmployeeSkills.Where(s => s.EmployeeId == empPassed.Id).ToListAsync();
            Assert.Contains(skills, s => s.SkillName == "Clean Architecture");

            // Kiểm tra chứng chỉ nội bộ được tự động cấp vào EmployeeCertificates
            var certs = await db.EmployeeCertificates.Where(c => c.EmployeeId == empPassed.Id).ToListAsync();
            Assert.Contains(certs, c => c.CertificateName == "Clean Architecture");

            // 4. Sát hạch học viên 2: Điểm 45 (< 60) -> FAILED (Không được cấp chứng chỉ)
            var gradeFailedDto = new GradeParticipantDto
            {
                TrainingId = trainingId,
                EmployeeId = empFailed.Id,
                Score = 45,
                Result = "Failed",
                Comment = "Chưa đạt yêu cầu bài kiểm tra thực hành",
                AutoGrantSkillOrCertificate = true
            };
            var gradeFailRes = await trainingService.GradeParticipantAsync(gradeFailedDto, 1);
            Assert.True(gradeFailRes.IsSuccess);

            // Xác nhận KHÔNG cấp chứng chỉ cho học viên trượt
            var failCerts = await db.EmployeeCertificates.Where(c => c.EmployeeId == empFailed.Id).ToListAsync();
            Assert.DoesNotContain(failCerts, c => c.CertificateName == "Clean Architecture");
        }

        #endregion

        #region 4. Payroll Calculator Integration Tests (KPI Bonus & HR Decisions)

        [Fact]
        public void PayrollCalculator_IntegratesKpiBonusAndDecisions_CorrectNetSalary()
        {
            var rules = PayrollTestData.Rules(statutoryBase: 2_340_000m);
            var input = PayrollTestData.FullMonth(baseSalary: 20_000_000m, declared: 20_000_000m);
            input.KpiBonus = 4_000_000m; // Thưởng KPI loại A+ (+20% của 20tr)
            input.DecisionReward = 1_500_000m; // Quyết định khen thưởng đột xuất
            input.DecisionDiscipline = 500_000m; // Quyết định phạt kỷ luật

            var result = PayrollCalculator.Calculate(input, rules);

            // Gross = Lương công (20tr) + KPI Bonus (4tr) + Khen thưởng (1.5tr) = 25,500,000 đ
            Assert.Equal(25500000m, result.Gross);
            Assert.Equal(4000000m, result.KpiBonus);
            Assert.Equal(1500000m, result.DecisionReward);
            Assert.Equal(500000m, result.DecisionDiscipline);

            // Bảo hiểm 10.5% trên 20tr = 2,100,000 đ (EmployeeTotal)
            Assert.Equal(2100000m, result.EmployeeTotal);

            // Net phải lớn hơn 0 và đã trừ khấu trừ kỷ luật 500,000 đ
            Assert.True(result.NetSalary > 0);
            Assert.True(result.NetSalary < result.Gross);
        }

        #endregion

        #region 5. Payroll Period Workflow & Lock/Pay Transitions

        [Fact]
        public async Task PayrollPeriod_LockAndPayTransitions_UpdatesStatusAndAudit()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var period = await payrollService.CalculatePayrollAsync(11, 2026, 22, 1);
            Assert.NotNull(period);

            // Giả lập Controller khóa kỳ lương
            var controller = new BangLuongController(
                db,
                payrollService,
                scope.ServiceProvider.GetRequiredService<ICurrentUserService>(),
                scope.ServiceProvider.GetRequiredService<IDataScopeService>());

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "SUPERADMIN")
            }, "TestAuth"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext,
                new DummyTempDataProvider());

            // Khóa kỳ lương
            var lockResult = await controller.KhoaBangLuong(period.Id) as RedirectToActionResult;
            Assert.NotNull(lockResult);

            var lockedPeriod = await db.PayrollPeriods.FindAsync(period.Id);
            Assert.NotNull(lockedPeriod);
            Assert.Equal(PayrollPeriodStatus.Locked, lockedPeriod.Status);
            Assert.NotNull(lockedPeriod.LockedAt);

            // Chi trả và phát hành
            var payResult = await controller.ChuyenChiTra(period.Id) as RedirectToActionResult;
            Assert.NotNull(payResult);

            var paidPeriod = await db.PayrollPeriods.FindAsync(period.Id);
            Assert.NotNull(paidPeriod);
            Assert.Equal(PayrollPeriodStatus.Paid, paidPeriod.Status);
        }

        #endregion

        #region 6. Bank Transfer UNC Export Security & AuditLog Tests

        [Fact]
        public async Task BankTransferUNC_UnauthorizedUser_Returns403ForbiddenAndLogsAudit()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var period = await payrollService.CalculatePayrollAsync(12, 2026, 22, 1);

            var controller = new BangLuongController(
                db,
                payrollService,
                scope.ServiceProvider.GetRequiredService<ICurrentUserService>(),
                scope.ServiceProvider.GetRequiredService<IDataScopeService>());

            // User bình thường (vai trò EMPLOYEE, không có SUPERADMIN hay ADMIN_BANGLUONG)
            var normalUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "99"),
                new Claim(ClaimTypes.Name, "nhanvien_thuong"),
                new Claim(ClaimTypes.Role, "EMPLOYEE")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = normalUser }
            };

            var actionResult = await controller.XuatUyNhiemChi(period.Id);
            var statusResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);

            // Xác minh AuditLog đã ghi nhận vi phạm bảo mật
            var auditLog = await db.AuditLogs
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync(a => a.Action == "SECURITY_VIOLATION" && a.RecordId == period.Id.ToString());

            Assert.NotNull(auditLog);
            Assert.Contains("không có quyền", auditLog.NewValues);
        }

        [Fact]
        public async Task BankTransferUNC_AuthorizedAdmin_ReturnsExcelFileAndLogsAudit()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var period = await payrollService.CalculatePayrollAsync(12, 2026, 22, 1);

            var controller = new BangLuongController(
                db,
                payrollService,
                scope.ServiceProvider.GetRequiredService<ICurrentUserService>(),
                scope.ServiceProvider.GetRequiredService<IDataScopeService>());

            // User có quyền ADMIN_BANGLUONG
            var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "ketoan_luong"),
                new Claim(ClaimTypes.Role, "ADMIN_BANGLUONG")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = adminUser }
            };

            var actionResult = await controller.XuatUyNhiemChi(period.Id);
            var fileResult = Assert.IsType<FileContentResult>(actionResult);

            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
            Assert.StartsWith("UyNhiemChi_", fileResult.FileDownloadName);
            Assert.True(fileResult.FileContents.Length > 0);

            // Xác minh AuditLog đã ghi nhận sự kiện xuất file UNC
            var auditLog = await db.AuditLogs
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync(a => a.Action == "EXPORT_BANK_TRANSFER" && a.RecordId == period.Id.ToString());

            Assert.NotNull(auditLog);
            Assert.Contains("Ủy nhiệm chi", auditLog.NewValues);
        }

        #endregion

        #region 7. Payroll Month-over-Month Comparison Tests

        [Fact]
        public async Task PayrollComparison_CalculatesMonthOverMonthMetrics_Correctly()
        {
            using var scope = _factory.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            // Tính 2 kỳ liên tiếp
            await payrollService.CalculatePayrollAsync(8, 2026, 22, 1);
            await payrollService.CalculatePayrollAsync(9, 2026, 22, 1);

            var comparison = await payrollService.GetPayrollComparisonAsync(9, 2026);
            Assert.NotNull(comparison);
            Assert.Equal(9, comparison.CurrentMonth);
            Assert.Equal(2026, comparison.CurrentYear);
            Assert.Contains("Tháng 08/2026", comparison.PreviousPeriodName);
            Assert.True(comparison.CurrentHeadcount >= 0);
        }

        #endregion
    }

    internal class DummyTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
