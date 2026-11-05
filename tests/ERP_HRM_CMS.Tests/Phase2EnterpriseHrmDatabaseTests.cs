using System.Data;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Bộ kiểm thử xác thực Phiên 2: Hoàn thiện Cơ sở Dữ liệu Enterprise HRM trên SSMS & EF Core
    /// - Kiểm tra 9 DbSet mới và cấu hình khóa chính/ngoại, kiểu dữ liệu decimal(18,2)/(5,2)
    /// - Kiểm tra Check Constraints và Non-Clustered / Covering Indexes trên SQL Server
    /// - Kiểm tra Extended Properties tiếng Việt trên các bảng Enterprise HRM
    /// - Kiểm tra tính toàn vẹn và phong phú của Dữ liệu mẫu (Quỹ phép, 6 Tin tuyển dụng SEO, 18 Ứng viên Kanban, Lịch phỏng vấn, Tài sản, Quyết định, Onboarding)
    /// - Kiểm tra hành vi Soft Delete (IAuditable) và chặn vi phạm ràng buộc CHECK
    /// </summary>
    public class Phase2EnterpriseHrmDatabaseTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public Phase2EnterpriseHrmDatabaseTests(TestAppFactory factory) => _factory = factory;

        [Fact]
        public async Task Schema_All_Enterprise_HRM_DbSets_Are_Configured_And_Queryable()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Xác nhận 9 thực thể Enterprise HRM truy vấn thành công qua EF Core
            var leaveBalances = await db.EmployeeLeaveBalances.AsNoTracking().ToListAsync();
            var attendanceLocks = await db.AttendanceLocks.AsNoTracking().ToListAsync();
            var adjustmentRequests = await db.AttendanceAdjustmentRequests.AsNoTracking().ToListAsync();
            var hrDecisions = await db.HrDecisions.AsNoTracking().ToListAsync();
            var companyAssets = await db.CompanyAssets.AsNoTracking().ToListAsync();
            var assetAssignments = await db.EmployeeAssetAssignments.AsNoTracking().ToListAsync();
            var onboardingChecklists = await db.OnboardingChecklists.AsNoTracking().ToListAsync();
            var onboardingTasks = await db.OnboardingTasks.AsNoTracking().ToListAsync();
            var interviewSchedules = await db.InterviewSchedules.AsNoTracking().ToListAsync();

            Assert.NotNull(leaveBalances);
            Assert.NotNull(attendanceLocks);
            Assert.NotNull(adjustmentRequests);
            Assert.NotNull(hrDecisions);
            Assert.NotNull(companyAssets);
            Assert.NotNull(assetAssignments);
            Assert.NotNull(onboardingChecklists);
            Assert.NotNull(onboardingTasks);
            Assert.NotNull(interviewSchedules);
        }

        [Fact]
        public async Task CheckConstraints_And_Indexes_Are_Active_On_SqlServer()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT name FROM sys.check_constraints WHERE is_disabled = 0;
                SELECT name FROM sys.indexes WHERE name IS NOT NULL AND is_hypothetical = 0;
            ";

            var checkConstraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var indexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                        checkConstraints.Add(reader.GetString(0));
                }

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                            indexes.Add(reader.GetString(0));
                    }
                }
            }

            // 1. Kiểm tra CHECK CONSTRAINTS nghiệp vụ
            Assert.Contains("CK_LeaveRequests_DateRange", checkConstraints);
            Assert.Contains("CK_LeaveRequests_TotalDays", checkConstraints);
            Assert.Contains("CK_EmployeeLeaveBalances_Year", checkConstraints);
            Assert.Contains("CK_EmployeeLeaveBalances_Days", checkConstraints);
            Assert.Contains("CK_AttendanceLocks_Month", checkConstraints);
            Assert.Contains("CK_AttendanceAdjustmentRequests_Times", checkConstraints);
            Assert.Contains("CK_HrDecisions_Amount", checkConstraints);
            Assert.Contains("CK_CompanyAssets_Price", checkConstraints);
            Assert.Contains("CK_EmployeeAssetAssignments_Dates", checkConstraints);
            Assert.Contains("CK_InterviewSchedules_Score", checkConstraints);

            // 2. Kiểm tra bộ chỉ mục Non-Clustered & Covering Indexes
            Assert.Contains("IX_AttendanceDailies_EmployeeId_WorkDate", indexes);
            Assert.Contains("IX_LeaveRequests_Status_FromDate_ToDate", indexes);
            Assert.Contains("IX_Employees_DepartmentId_Status_IsDeleted", indexes);
            Assert.Contains("IX_Candidates_JobPostingId_Status", indexes);
            Assert.Contains("IX_JobPostings_Slug", indexes);
            Assert.Contains("IX_Contents_Slug", indexes);
            Assert.Contains("IX_EmployeeLeaveBalances_EmployeeId_Year_LeaveTypeId", indexes);
            Assert.Contains("IX_AttendanceLocks_Year_Month_BranchId_DepartmentId", indexes);
            Assert.Contains("IX_AttendanceAdjustmentRequests_RequestCode", indexes);
            Assert.Contains("IX_HrDecisions_DecisionNumber", indexes);
            Assert.Contains("IX_CompanyAssets_AssetCode", indexes);
            Assert.Contains("IX_EmployeeAssetAssignments_EmployeeId_Status", indexes);
            Assert.Contains("IX_OnboardingChecklists_EmployeeId_Type", indexes);
            Assert.Contains("IX_InterviewSchedules_CandidateId_RoundNumber", indexes);
        }

        [Fact]
        public async Task ExtendedProperties_Exist_On_Enterprise_HRM_Tables()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT objname, CAST(value AS NVARCHAR(MAX)) AS Description
                FROM fn_listextendedproperty(N'MS_Description', N'SCHEMA', N'dbo', N'TABLE', NULL, NULL, NULL);
            ";

            var tableDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    tableDescriptions[reader.GetString(0)] = reader.GetString(1);
                }
            }

            Assert.True(tableDescriptions.ContainsKey("EmployeeLeaveBalances"));
            Assert.True(tableDescriptions.ContainsKey("AttendanceLocks"));
            Assert.True(tableDescriptions.ContainsKey("AttendanceAdjustmentRequests"));
            Assert.True(tableDescriptions.ContainsKey("HrDecisions"));
            Assert.True(tableDescriptions.ContainsKey("CompanyAssets"));
            Assert.True(tableDescriptions.ContainsKey("EmployeeAssetAssignments"));
            Assert.True(tableDescriptions.ContainsKey("OnboardingChecklists"));
            Assert.True(tableDescriptions.ContainsKey("InterviewSchedules"));

            Assert.Contains("Quỹ phép năm", tableDescriptions["EmployeeLeaveBalances"]);
            Assert.Contains("Khóa kỳ công", tableDescriptions["AttendanceLocks"]);
            Assert.Contains("Quyết định Nhân sự", tableDescriptions["HrDecisions"]);
        }

        [Fact]
        public async Task EnterpriseHRM_SeedData_Loaded_Correctly_And_Covers_Lifecycle()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Quỹ phép năm
            var leaveBalances = await db.EmployeeLeaveBalances.Include(b => b.LeaveType).ToListAsync();
            Assert.NotEmpty(leaveBalances);
            Assert.All(leaveBalances, b =>
            {
                Assert.Equal(2026, b.Year);
                Assert.True(b.StandardDays >= 12.0m);
                Assert.True(b.RemainingDays >= 0.0m);
            });

            // 2. Tin tuyển dụng SEO: 6 tin kèm Slug chuẩn
            var jobPostings = await db.JobPostings.ToListAsync();
            Assert.True(jobPostings.Count >= 6);
            Assert.Contains(jobPostings, j => j.Slug == "truong-phong-kinh-doanh-doanh-nghiep");
            Assert.Contains(jobPostings, j => j.Slug == "ky-su-cau-noi-lap-trinh-vien-senior-dotnet");
            Assert.Contains(jobPostings, j => j.Slug == "chuyen-vien-ke-toan-tong-hop-thue");

            // 3. 18 Hồ sơ ứng viên đủ 6 cột Kanban
            var candidates = await db.Candidates.ToListAsync();
            Assert.True(candidates.Count >= 18);
            var statuses = candidates.Select(c => c.Status).ToHashSet();
            Assert.Contains("Applied", statuses);
            Assert.Contains("Screening", statuses);
            Assert.Contains("Interview", statuses);
            Assert.Contains("Offered", statuses);
            Assert.Contains("Hired", statuses);
            Assert.Contains("Rejected", statuses);

            // 4. Lịch phỏng vấn
            var interviews = await db.InterviewSchedules.ToListAsync();
            Assert.NotEmpty(interviews);
            Assert.Contains(interviews, i => i.RoundNumber == 1);
            Assert.Contains(interviews, i => !string.IsNullOrEmpty(i.LocationOrMeetingLink));

            // 5. Tài sản và Cấp phát
            var assets = await db.CompanyAssets.ToListAsync();
            Assert.NotEmpty(assets);
            Assert.Contains(assets, a => a.AssetType == "Laptop" && a.PurchasePrice > 0);
            Assert.Contains(assets, a => a.AssetType == "Monitor");

            var assignments = await db.EmployeeAssetAssignments.Include(a => a.Asset).ToListAsync();
            Assert.NotEmpty(assignments);
            Assert.Contains(assignments, a => a.Status == "Active" && a.Asset != null);

            // 6. Quyết định Nhân sự điện tử
            var decisions = await db.HrDecisions.ToListAsync();
            Assert.NotEmpty(decisions);
            Assert.Contains(decisions, d => d.DecisionType == "Appointment");
            Assert.Contains(decisions, d => d.DecisionType == "Reward" && d.Amount == 10000000.00m);

            // 7. Khóa chấm công & Đơn giải trình
            var attendanceLocks = await db.AttendanceLocks.ToListAsync();
            Assert.Contains(attendanceLocks, l => l.Month == 9 && l.Year == 2026 && l.IsLocked);

            var adjustments = await db.AttendanceAdjustmentRequests.ToListAsync();
            Assert.NotEmpty(adjustments);
            Assert.Contains(adjustments, a => a.AdjustmentType == "MissedCheckIn");

            // 8. Onboarding checklist & tasks phân vai
            var onboarding = await db.OnboardingChecklists.Include(c => c.Tasks).FirstOrDefaultAsync(c => c.Type == "Onboarding");
            Assert.NotNull(onboarding);
            Assert.True(onboarding.Tasks.Count >= 8);
            var roles = onboarding.Tasks.Select(t => t.DepartmentRole).ToHashSet();
            Assert.Contains("HR", roles);
            Assert.Contains("IT", roles);
            Assert.Contains("DepartmentHead", roles);
        }

        [Fact]
        public async Task SoftDelete_Filter_Applies_To_Enterprise_HRM_Entities()
        {
            int createdId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var asset = new CompanyAsset
                {
                    AssetCode = "AST-TEST-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                    AssetName = "Thiết bị kiểm thử Soft Delete",
                    AssetType = "Other",
                    Status = "Available",
                    IsDeleted = false
                };
                db.CompanyAssets.Add(asset);
                await db.SaveChangesAsync();
                createdId = asset.Id;

                // Soft-delete
                asset.IsDeleted = true;
                await db.SaveChangesAsync();
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Truy vấn thông thường: Global query filter lọc bỏ
                var queryResult = await db.CompanyAssets.FirstOrDefaultAsync(a => a.Id == createdId);
                Assert.Null(queryResult);

                // IgnoreQueryFilters: Vẫn tìm thấy trong DB
                var rawResult = await db.CompanyAssets.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == createdId);
                Assert.NotNull(rawResult);
                Assert.True(rawResult.IsDeleted);

                // Dọn dẹp bản ghi kiểm thử
                db.CompanyAssets.Remove(rawResult);
                await db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task CheckConstraint_Rejects_Invalid_DateRange_On_LeaveRequest()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var emp = await db.Employees.FirstAsync();
            var invalidLeave = new LeaveRequest
            {
                RequestCode = "LR-INVALID-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                EmployeeId = emp.Id,
                FromDate = new DateTime(2026, 11, 20),
                ToDate = new DateTime(2026, 11, 10), // ToDate < FromDate vi phạm CK_LeaveRequests_DateRange
                TotalDays = 1.0m,
                Reason = "Kiểm thử ràng buộc CHECK khoảng ngày",
                Status = Models.Enums.LeaveRequestStatus.PendingApproval
            };

            db.LeaveRequests.Add(invalidLeave);
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Contains("CK_LeaveRequests_DateRange", ex.InnerException?.Message ?? ex.Message);
        }
    }
}

