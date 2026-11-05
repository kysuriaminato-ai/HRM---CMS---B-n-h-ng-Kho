using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Bộ kiểm thử nghiệp vụ PHIÊN 4: Hồ Sơ Nhân Sự 360°, Quyết Định Nhân Sự & Quản Lý Tài Sản Cấp Phát
    /// </summary>
    public class Phase4CoreHrm360Tests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public Phase4CoreHrm360Tests(TestAppFactory factory)
        {
            _factory = factory;
        }

        // ═══════════════════════════════════════════════════════════
        // 1. NGHIỆP VỤ QUYẾT ĐỊNH NHÂN SỰ (HR DECISIONS)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task HrDecision_Appointment_Updates_Employee_Position_And_Logs_History()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hrService = scope.ServiceProvider.GetRequiredService<IHrDecisionService>();

            // Lấy 1 nhân sự mẫu và 1 chức vụ
            var emp = await db.Employees.Include(e => e.Position).FirstAsync(e => !e.IsDeleted);
            var positions = await db.Positions.ToListAsync();
            var targetPosition = positions.FirstOrDefault(p => p.Id != emp.PositionId) ?? positions.First();

            var oldPositionId = emp.PositionId;

            // Tạo quyết định bổ nhiệm
            var decision = new HrDecision
            {
                DecisionNumber = $"QD-TEST-APPOINT-{Guid.NewGuid():N}".Substring(0, 20),
                Title = "Quyết định bổ nhiệm thử nghiệm",
                EmployeeId = emp.Id,
                DecisionType = "Appointment",
                EffectiveDate = DateTime.Today,
                OldPositionId = oldPositionId,
                NewPositionId = targetPosition.Id,
                Reason = "Thành tích xuất sắc trong công tác",
                Status = "Draft"
            };

            var (createSuccess, _, decisionId) = await hrService.CreateAsync(decision, 1);
            Assert.True(createSuccess);
            Assert.True(decisionId > 0);

            // Phê duyệt quyết định
            var (approveSuccess, approveError) = await hrService.ApproveDecisionAsync(decisionId, 1);
            Assert.True(approveSuccess, approveError);

            // Kiểm tra nhân sự đã được cập nhật chức vụ
            var reloadedEmp = await db.Employees.AsNoTracking().FirstAsync(e => e.Id == emp.Id);
            Assert.Equal(targetPosition.Id, reloadedEmp.PositionId);

            // Kiểm tra lịch sử biến động nhân sự (EmployeeChangeHistories)
            var history = await db.EmployeeChangeHistories
                .Where(h => h.EmployeeId == emp.Id && h.ChangeType == "Position")
                .OrderByDescending(h => h.Id)
                .FirstOrDefaultAsync();
            Assert.NotNull(history);
            Assert.Equal("Thành tích xuất sắc trong công tác", history.Reason);
        }

        [Fact]
        public async Task HrDecision_Transfer_Updates_Employee_Department()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hrService = scope.ServiceProvider.GetRequiredService<IHrDecisionService>();

            var emp = await db.Employees.FirstAsync(e => !e.IsDeleted);
            var depts = await db.Departments.ToListAsync();
            var targetDept = depts.FirstOrDefault(d => d.Id != emp.DepartmentId) ?? depts.First();

            var decision = new HrDecision
            {
                DecisionNumber = $"QD-TEST-TRANS-{Guid.NewGuid():N}".Substring(0, 20),
                Title = "Quyết định điều chuyển phòng ban",
                EmployeeId = emp.Id,
                DecisionType = "Transfer",
                EffectiveDate = DateTime.Today,
                OldDepartmentId = emp.DepartmentId,
                NewDepartmentId = targetDept.Id,
                Status = "Draft"
            };

            var (_, _, decisionId) = await hrService.CreateAsync(decision, 1);
            var (success, error) = await hrService.ApproveDecisionAsync(decisionId, 1);
            Assert.True(success, error);

            var updatedEmp = await db.Employees.AsNoTracking().FirstAsync(e => e.Id == emp.Id);
            Assert.Equal(targetDept.Id, updatedEmp.DepartmentId);
        }

        [Fact]
        public async Task HrDecision_Termination_Sets_Resigned_And_Deactivates_User_Account()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hrService = scope.ServiceProvider.GetRequiredService<IHrDecisionService>();

            var dept = await db.Departments.FirstAsync();
            var pos = await db.Positions.FirstAsync();
            var testEmp = new Employee
            {
                EmployeeCode = $"NV-TERM-{Guid.NewGuid():N}".Substring(0, 10),
                FullName = "Nhân viên kiểm thử thôi việc",
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                Status = "Active",
                HireDate = DateTime.Today.AddYears(-1)
            };
            db.Employees.Add(testEmp);
            await db.SaveChangesAsync();

            // Tạo tài khoản user liên kết với nhân viên này
            var testUser = new User
            {
                Username = $"user_term_{Guid.NewGuid():N}".Substring(0, 20),
                PasswordHash = "hashed",
                PasswordSalt = "salt",
                EmployeeId = testEmp.Id,
                IsActive = true
            };
            db.Users.Add(testUser);
            await db.SaveChangesAsync();

            var decision = new HrDecision
            {
                DecisionNumber = $"QD-TERM-{Guid.NewGuid():N}".Substring(0, 20),
                Title = "Quyết định chấm dứt hợp đồng lao động",
                EmployeeId = testEmp.Id,
                DecisionType = "Termination",
                EffectiveDate = DateTime.Today,
                Reason = "Thỏa thuận chấm dứt HĐLĐ",
                Status = "Draft"
            };

            var (_, _, decisionId) = await hrService.CreateAsync(decision, 1);
            var (success, error) = await hrService.ApproveDecisionAsync(decisionId, 1);
            Assert.True(success, error);

            // Kiểm tra trạng thái nhân viên
            var reloadedEmp = await db.Employees.AsNoTracking().FirstAsync(e => e.Id == testEmp.Id);
            Assert.Equal("Resigned", reloadedEmp.Status);

            // Kiểm tra tài khoản user bị vô hiệu hóa
            var reloadedUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == testUser.Id);
            Assert.False(reloadedUser.IsActive);
        }

        [Fact]
        public async Task HrDecision_Approve_AlreadyApproved_Returns_Failure()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hrService = scope.ServiceProvider.GetRequiredService<IHrDecisionService>();

            var emp = await db.Employees.FirstAsync();
            var decision = new HrDecision
            {
                DecisionNumber = $"QD-DUP-{Guid.NewGuid():N}".Substring(0, 20),
                Title = "Quyết định trùng",
                EmployeeId = emp.Id,
                DecisionType = "SalaryAdjustment",
                EffectiveDate = DateTime.Today,
                Status = "Approved" // Đã duyệt
            };
            db.HrDecisions.Add(decision);
            await db.SaveChangesAsync();

            var (success, error) = await hrService.ApproveDecisionAsync(decision.Id, 1);
            Assert.False(success);
            Assert.Contains("đã được phê duyệt", error ?? "");
        }

        // ═══════════════════════════════════════════════════════════
        // 2. NGHIỆP VỤ TÀI SẢN CẤP PHÁT (ASSET MANAGEMENT)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Asset_Assign_And_Return_Cycle_Works_Correctly()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var assetService = scope.ServiceProvider.GetRequiredService<IAssetManagementService>();

            var emp = await db.Employees.FirstAsync(e => !e.IsDeleted);

            // 1. Tạo tài sản mới
            var asset = new CompanyAsset
            {
                AssetCode = $"AST-TEST-{Guid.NewGuid():N}".Substring(0, 15),
                AssetName = "Laptop Lenovo ThinkPad T14",
                AssetType = "Laptop",
                Status = "Available",
                PurchasePrice = 25000000,
                Specification = "i7 16GB RAM 512GB SSD"
            };
            var (createSuccess, _, assetId) = await assetService.CreateAssetAsync(asset, 1);
            Assert.True(createSuccess);

            // 2. Cấp phát tài sản cho nhân viên
            var (assignSuccess, assignError) = await assetService.AssignAssetAsync(
                assetId, emp.Id, "Nguyên hộp mới 100%", 1, "Cấp cho dự án mới");
            Assert.True(assignSuccess, assignError);

            // Kiểm tra trạng thái tài sản chuyển sang Assigned
            var reloadedAsset = await assetService.GetAssetByIdAsync(assetId);
            Assert.NotNull(reloadedAsset);
            Assert.Equal("Assigned", reloadedAsset.Status);

            // Kiểm tra HasUnreturnedAssetsAsync trả về true
            bool hasUnreturned = await assetService.HasUnreturnedAssetsAsync(emp.Id);
            Assert.True(hasUnreturned);

            // 3. Thu hồi tài sản
            var assignments = await assetService.GetAssetAssignmentsAsync(assetId);
            var activeAssignment = assignments.First(a => a.Status == "Active");

            var (returnSuccess, returnError) = await assetService.ReturnAssetAsync(
                activeAssignment.Id, "Máy nguyên vẹn, trầy xước nhẹ", "Available", 1, "Thu hồi nhập kho");
            Assert.True(returnSuccess, returnError);

            // Kiểm tra trạng thái tài sản đã về Available
            var returnedAsset = await assetService.GetAssetByIdAsync(assetId);
            Assert.NotNull(returnedAsset);
            Assert.Equal("Available", returnedAsset.Status);
        }

        // ═══════════════════════════════════════════════════════════
        // 3. CẢNH BÁO NHÂN SỰ & HỢP ĐỒNG (SMART HR ALERTS)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task SmartAlerts_Returns_Expiring_Contracts_And_Probations()
        {
            using var scope = _factory.Services.CreateScope();
            var empService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            var alerts = await empService.GetSmartAlertsAsync();
            Assert.NotNull(alerts);
            Assert.NotNull(alerts.ExpiringContracts);
            Assert.NotNull(alerts.EndingProbations);
            Assert.NotNull(alerts.ExpiringCertificates);
            Assert.NotNull(alerts.BirthdaysInMonth);
        }

        // ═══════════════════════════════════════════════════════════
        // 4. EXCEL IMPORT & EXPORT (CLOSEDXML)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void EmployeeService_GenerateImportTemplate_Returns_Valid_Excel_Bytes()
        {
            using var scope = _factory.Services.CreateScope();
            var empService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            var templateBytes = empService.GenerateImportTemplate();
            Assert.NotNull(templateBytes);
            Assert.True(templateBytes.Length > 0);

            // Kiểm tra header file zip/xlsx (PK magic bytes: 0x50, 0x4B)
            Assert.Equal(0x50, templateBytes[0]);
            Assert.Equal(0x4B, templateBytes[1]);
        }

        [Fact]
        public async Task EmployeeService_ExportToExcel_Returns_Valid_Excel_Bytes()
        {
            using var scope = _factory.Services.CreateScope();
            var empService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            var exportBytes = await empService.ExportToExcelAsync(new EmployeeFilterDto());
            Assert.NotNull(exportBytes);
            Assert.True(exportBytes.Length > 0);
            Assert.Equal(0x50, exportBytes[0]);
            Assert.Equal(0x4B, exportBytes[1]);
        }

        // ═══════════════════════════════════════════════════════════
        // 5. IN ẤN VĂN BẢN HÀNH CHÍNH (PRINTABLE CONTRACT & RESUME)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task ContractService_GetPrintModel_Returns_Accurate_Labor_Details()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var contractService = scope.ServiceProvider.GetRequiredService<IContractService>();

            var contract = await db.Contracts.Include(c => c.Employee).FirstOrDefaultAsync();
            if (contract != null)
            {
                var printModel = await contractService.GetPrintModelAsync(contract.Id);
                Assert.NotNull(printModel);
                Assert.Equal(contract.ContractNumber, printModel.ContractNumber);
                Assert.Equal(contract.Employee.FullName, printModel.FullName);
                Assert.False(string.IsNullOrEmpty(printModel.CompanyName));
            }
        }

        [Fact]
        public async Task EmployeeService_GetPrintableResume_Returns_Full_360_Profile()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var empService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            var emp = await db.Employees.FirstOrDefaultAsync();
            if (emp != null)
            {
                var resume = await empService.GetPrintableResumeAsync(emp.Id);
                Assert.NotNull(resume);
                Assert.Equal(emp.EmployeeCode, resume.EmployeeCode);
                Assert.Equal(emp.FullName, resume.FullName);
            }
        }
    }
}
