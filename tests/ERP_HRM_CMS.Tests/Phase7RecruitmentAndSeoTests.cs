using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class Phase7RecruitmentAndSeoTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public Phase7RecruitmentAndSeoTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        #region Helper Methods

        private async Task<(JobPosting job, Department dept, Position pos)> CreateTestJobPostingAsync(string title, string slug, string status = "Open")
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var dept = await db.Departments.FirstOrDefaultAsync() ?? new Department
            {
                Code = "IT",
                Name = "Công Nghệ Thông Tin",
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
                Code = "DEV",
                Name = "Kỹ Sư Phần Mềm",
                IsActive = true
            };
            if (pos.Id == 0)
            {
                db.Positions.Add(pos);
                await db.SaveChangesAsync();
            }

            var job = new JobPosting
            {
                Code = $"TD-{Guid.NewGuid():N}"[..10],
                Title = title,
                Slug = slug,
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                Vacancies = 3,
                Description = "Mô tả công việc kiểm thử tự động ATS.",
                Requirements = "Thành thạo C# .NET và SQL Server.",
                SalaryRange = "20 - 35 Triệu",
                WorkLocation = "Hà Nội",
                Status = status,
                OpenDate = DateTime.Today,
                CloseDate = DateTime.Today.AddDays(30),
                CreatedAt = VietnamTime.Now
            };

            db.JobPostings.Add(job);
            await db.SaveChangesAsync();

            return (job, dept, pos);
        }

        private static IFormFile CreateMockFormFile(string fileName, byte[] contentBytes)
        {
            var stream = new MemoryStream(contentBytes);
            return new FormFile(stream, 0, contentBytes.Length, "CvFile", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = fileName.EndsWith(".pdf") ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };
        }

        #endregion

        #region 1. Sitemap.xml & Robots.txt Integration Tests

        [Fact]
        public async Task Sitemap_Returns_Valid_Xml_With_Careers_And_Pages()
        {
            // Arrange
            var client = _factory.CreateClient();
            var (job, _, _) = await CreateTestJobPostingAsync("Kỹ sư Frontend Vue/React", $"frontend-dev-{Guid.NewGuid():N}"[..20]);

            // Act
            var response = await client.GetAsync("/sitemap.xml");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Contains("xml", response.Content.Headers.ContentType.MediaType?.ToLowerInvariant());

            string xml = await response.Content.ReadAsStringAsync();
            Assert.Contains("<urlset", xml);
            Assert.Contains("/tuyen-dung", xml);
            Assert.Contains(job.Slug, xml);
            Assert.Contains("<changefreq>", xml);
            Assert.Contains("<priority>", xml);
        }

        [Fact]
        public async Task RobotsTxt_Allows_Public_And_Disallows_Admin_Routes()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/robots.txt");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);

            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("User-agent: *", content);
            Assert.Contains("Allow: /", content);
            Assert.Contains("Allow: /tuyen-dung", content);
            Assert.Contains("Disallow: /admin", content);
            Assert.Contains("Disallow: /NhanSu", content);
            Assert.Contains("Disallow: /BangLuong", content);
            Assert.Contains("Disallow: /ChamCong", content);
            Assert.Contains("Disallow: /Auth/", content);
            Assert.Contains("Sitemap:", content);
            Assert.Contains("/sitemap.xml", content);
        }

        #endregion

        #region 2. Public Careers Portal & Technical SEO (JSON-LD Google for Jobs)

        [Fact]
        public async Task Public_Careers_Index_Returns_Success_And_Lists_Open_Jobs()
        {
            // Arrange
            var client = _factory.CreateClient();
            string slug = $"backend-lead-{Guid.NewGuid():N}"[..20];
            await CreateTestJobPostingAsync("Trưởng Nhóm Backend .NET", slug);

            // Act
            var response = await client.GetAsync("/tuyen-dung");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.Contains("Cơ Hội Nghề Nghiệp", html);
            Assert.Contains(slug, html);
            Assert.Contains("Trưởng Nhóm Backend .NET", html);
        }

        [Fact]
        public async Task Public_Job_Detail_Contains_GoogleForJobs_LdJson_And_OpenGraph()
        {
            // Arrange
            var client = _factory.CreateClient();
            string slug = $"devops-lead-{Guid.NewGuid():N}"[..20];
            await CreateTestJobPostingAsync("Kỹ Sư Trưởng DevOps Cloud", slug);

            // Act
            var response = await client.GetAsync($"/tuyen-dung/{slug}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string rawHtml = await response.Content.ReadAsStringAsync();
            string html = WebUtility.HtmlDecode(rawHtml);

            // Schema.org Google for Jobs structured data
            Assert.Contains("application/ld+json", rawHtml);
            Assert.Contains("\"@type\": \"JobPosting\"", rawHtml);
            Assert.Contains("Kỹ Sư Trưởng DevOps Cloud", html);
            Assert.Contains("\"hiringOrganization\"", rawHtml);

            // Open Graph & Meta tags
            Assert.Contains("og:title", rawHtml);
            Assert.Contains("og:description", rawHtml);
            Assert.Contains("og:type", rawHtml);
            Assert.Contains("canonical", rawHtml);
        }

        #endregion

        #region 3. CV Security File Validation (Magic Bytes & Anti-Tampering)

        [Fact]
        public void ValidateCvFile_Accepts_Valid_Pdf_With_Magic_Bytes()
        {
            // %PDF magic bytes (0x25, 0x50, 0x44, 0x46)
            byte[] validPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nSample content");
            var mockFile = CreateMockFormFile("ho-so-ung-vien.pdf", validPdfBytes);

            var (isValid, error) = FileUploadValidator.ValidateCvFile(mockFile);

            Assert.True(isValid);
            Assert.Null(error);
        }

        [Fact]
        public void ValidateCvFile_Rejects_Spoofed_Pdf_With_Invalid_Magic_Bytes()
        {
            // Fake PDF: an executable or plain text masquerading as .pdf
            byte[] fakePdfBytes = Encoding.ASCII.GetBytes("MZ fake executable binary content here");
            var mockFile = CreateMockFormFile("malicious-file.pdf", fakePdfBytes);

            var (isValid, error) = FileUploadValidator.ValidateCvFile(mockFile);

            Assert.False(isValid);
            Assert.NotNull(error);
            Assert.Contains("nghi ngờ giả mạo", error);
        }

        [Fact]
        public void ValidateCvFile_Accepts_Valid_Docx_With_Zip_Magic_Bytes()
        {
            // PK\x03\x04 magic bytes (0x50, 0x4B, 0x03, 0x04)
            byte[] validDocxBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 };
            var mockFile = CreateMockFormFile("resume.docx", validDocxBytes);

            var (isValid, error) = FileUploadValidator.ValidateCvFile(mockFile);

            Assert.True(isValid);
            Assert.Null(error);
        }

        [Fact]
        public void ValidateCvFile_Rejects_Spoofed_Docx_With_Invalid_Magic_Bytes()
        {
            byte[] fakeDocxBytes = Encoding.ASCII.GetBytes("Plain text masquerading as docx file");
            var mockFile = CreateMockFormFile("fake-resume.docx", fakeDocxBytes);

            var (isValid, error) = FileUploadValidator.ValidateCvFile(mockFile);

            Assert.False(isValid);
            Assert.NotNull(error);
            Assert.Contains("nghi ngờ giả mạo", error);
        }

        [Fact]
        public void ValidateCvFile_Rejects_Unsupported_Extensions()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Some content");
            var mockFile = CreateMockFormFile("script.exe", bytes);

            var (isValid, error) = FileUploadValidator.ValidateCvFile(mockFile);

            Assert.False(isValid);
            Assert.Contains("Chỉ chấp nhận", error);
        }

        #endregion

        #region 4. ATS Recruitment Pipeline & Interview Lifecycle

        [Fact]
        public async Task Candidate_Can_Be_Scheduled_For_Interview_And_Evaluated()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var recruitmentService = scope.ServiceProvider.GetRequiredService<IRecruitmentService>();

            var (job, _, _) = await CreateTestJobPostingAsync("Kỹ sư AI/ML", $"ai-ml-engineer-{Guid.NewGuid():N}"[..20]);

            var candidate = new Candidate
            {
                Code = $"UV-{Guid.NewGuid():N}"[..10],
                FullName = "Nguyễn Văn AI",
                Email = "nguyenvan.ai@example.com",
                Phone = "0912345678",
                JobPostingId = job.Id,
                Status = "Applied",
                AppliedDate = VietnamTime.Now,
                CreatedAt = VietnamTime.Now
            };
            db.Candidates.Add(candidate);
            await db.SaveChangesAsync();

            // 1. Lên lịch phỏng vấn
            var schedDto = new InterviewScheduleCreateDto
            {
                CandidateId = candidate.Id,
                RoundNumber = 1,
                Title = "Phỏng vấn chuyên sâu thuật toán & ML",
                ScheduledTime = DateTime.Now.AddDays(2),
                DurationMinutes = 60,
                InterviewType = "Online",
                LocationOrMeetingLink = "https://meet.google.com/test-ai-interview"
            };

            var schedResult = await recruitmentService.ScheduleInterviewAsync(schedDto, 1);
            Assert.True(schedResult.IsSuccess);
            Assert.True(schedResult.Value > 0);

            // Verify candidate moved to 'Interview' status
            var candAfterSchedule = await recruitmentService.GetCandidateByIdAsync(candidate.Id);
            Assert.NotNull(candAfterSchedule);
            Assert.Equal("Interview", candAfterSchedule.Status);

            // 2. Chấm điểm & Đánh giá phỏng vấn
            var evalDto = new InterviewEvaluationDto
            {
                ScheduleId = schedResult.Value,
                Score = 92.5m,
                Result = "Passed",
                Feedback = "Ứng viên nắm rất chắc kiến thức Deep Learning, tư duy logic tốt."
            };

            var evalResult = await recruitmentService.EvaluateInterviewAsync(evalDto, 1);
            Assert.True(evalResult.IsSuccess);

            // Verify evaluation score synced to candidate
            var candAfterEval = await recruitmentService.GetCandidateByIdAsync(candidate.Id);
            Assert.NotNull(candAfterEval);
            Assert.Equal(92.5m, candAfterEval.EvaluationScore);
        }

        #endregion

        #region 5. Golden Path: 1-Click Candidate To Employee & Onboarding Lifecycle

        [Fact]
        public async Task GoldenPath_ConvertCandidateToEmployee_Creates_Employee_Contract_And_Onboarding_Checklist()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var recruitmentService = scope.ServiceProvider.GetRequiredService<IRecruitmentService>();
            var onboardingService = scope.ServiceProvider.GetRequiredService<IOnboardingService>();

            var (job, dept, pos) = await CreateTestJobPostingAsync("Senior C# Architect", $"csharp-lead-{Guid.NewGuid():N}"[..20]);

            var candidate = new Candidate
            {
                Code = $"UV-{Guid.NewGuid():N}"[..10],
                FullName = "Lê Thị Thảo Nguyên",
                Email = $"thaonguyen.{Guid.NewGuid():N}[..8]@company.vn",
                Phone = "0987654321",
                JobPostingId = job.Id,
                Status = "Offered",
                AppliedDate = VietnamTime.Now,
                CreatedAt = VietnamTime.Now
            };
            db.Candidates.Add(candidate);
            await db.SaveChangesAsync();

            // Golden Path: Convert Candidate To Employee
            var convertDto = new ConvertCandidateToEmployeeDto
            {
                CandidateId = candidate.Id,
                FullName = candidate.FullName,
                Email = candidate.Email,
                Phone = candidate.Phone,
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                HireDate = DateTime.Today,
                BaseSalary = 25000000,
                ProbationMonths = 2
            };

            var convertResult = await recruitmentService.ConvertToEmployeeAsync(convertDto, 1);
            Assert.True(convertResult.IsSuccess);
            int newEmployeeId = convertResult.Value;
            Assert.True(newEmployeeId > 0);

            // 1. Verify Employee created
            var employee = await db.Employees.FindAsync(newEmployeeId);
            Assert.NotNull(employee);
            Assert.Equal("Lê Thị Thảo Nguyên", employee.FullName);
            Assert.Equal("Probation", employee.Status);
            Assert.StartsWith("NV-", employee.EmployeeCode);

            // 2. Verify Contract created
            var contract = await db.Contracts.FirstOrDefaultAsync(c => c.EmployeeId == newEmployeeId);
            Assert.NotNull(contract);
            Assert.Equal("Probation", contract.ContractType);
            Assert.Equal(25000000, contract.BasicSalary);
            Assert.Equal("Draft", contract.Status);

            // 3. Verify Candidate updated to Hired
            var candDb = await db.Candidates.FindAsync(candidate.Id);
            Assert.NotNull(candDb);
            Assert.Equal("Hired", candDb.Status);
            Assert.Contains(employee.EmployeeCode, candDb.Note);

            // 4. Verify Onboarding Checklist automatically created with 4 standard tasks
            var checklists = await onboardingService.GetChecklistsAsync("Onboarding", null, newEmployeeId);
            Assert.Single(checklists);
            var checklist = checklists[0];
            Assert.Equal("InProgress", checklist.Status);
            Assert.Equal(4, checklist.TotalTasks);
            Assert.Equal(0, checklist.CompletedTasks);
            Assert.Equal(0, checklist.ProgressPercentage);

            // 5. Test task completion progress lifecycle: 0% -> 25% -> 50% -> 75% -> 100%
            var tasks = checklist.Tasks;
            Assert.Equal(4, tasks.Count);

            // Hoàn thành task 1 (IT Cấp Email)
            await onboardingService.UpdateTaskProgressAsync(tasks[0].Id, true, "Đã tạo email công ty", 1);
            var chk1 = await onboardingService.GetChecklistByIdAsync(checklist.Id);
            Assert.NotNull(chk1);
            Assert.Equal(1, chk1.CompletedTasks);
            Assert.Equal(25, chk1.ProgressPercentage);
            Assert.Equal("InProgress", chk1.Status);

            // Hoàn thành task 2 (IT Bàn giao laptop)
            await onboardingService.UpdateTaskProgressAsync(tasks[1].Id, true, "Bàn giao laptop ThinkPad T14", 1);
            var chk2 = await onboardingService.GetChecklistByIdAsync(checklist.Id);
            Assert.NotNull(chk2);
            Assert.Equal(2, chk2.CompletedTasks);
            Assert.Equal(50, chk2.ProgressPercentage);

            // Hoàn thành task 3 (HR Đào tạo nội quy)
            await onboardingService.UpdateTaskProgressAsync(tasks[2].Id, true, "Đã ký cam kết bảo mật & nội quy", 1);
            var chk3 = await onboardingService.GetChecklistByIdAsync(checklist.Id);
            Assert.NotNull(chk3);
            Assert.Equal(3, chk3.CompletedTasks);
            Assert.Equal(75, chk3.ProgressPercentage);

            // Hoàn thành task 4 (Trưởng bộ phận giao việc tuần 1)
            await onboardingService.UpdateTaskProgressAsync(tasks[3].Id, true, "Đã giao backlog sprint 1", 1);
            var chk4 = await onboardingService.GetChecklistByIdAsync(checklist.Id);
            Assert.NotNull(chk4);
            Assert.Equal(4, chk4.CompletedTasks);
            Assert.Equal(100, chk4.ProgressPercentage);
            Assert.Equal("Completed", chk4.Status);
            Assert.NotNull(chk4.CompletedDate);
        }

        #endregion
    }
}

