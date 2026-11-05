using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using ERP.Application.Common.Models;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ERP_HRM_CMS.Services.HRM
{
    public class RecruitmentService : IRecruitmentService
    {
        private readonly AppDbContext _context;
        private readonly IOnboardingService _onboardingService;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RecruitmentService> _logger;

        public RecruitmentService(
            AppDbContext context,
            IOnboardingService onboardingService,
            INotificationService notificationService,
            IWebHostEnvironment env,
            IMemoryCache cache,
            ILogger<RecruitmentService> logger)
        {
            _context = context;
            _onboardingService = onboardingService;
            _notificationService = notificationService;
            _env = env;
            _cache = cache;
            _logger = logger;
        }

        #region Job Postings Management

        public async Task<PagedResult<JobPostingDto>> GetJobPostingsAsync(JobPostingFilterDto filter)
        {
            var query = _context.JobPostings
                .Include(j => j.Department)
                    .ThenInclude(d => d.Branch)
                .Include(j => j.Position)
                .Include(j => j.Candidates)
                .Where(j => !j.IsDeleted)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                string kw = filter.Keyword.Trim().ToLower();
                query = query.Where(j => j.Title.ToLower().Contains(kw) || j.Code.ToLower().Contains(kw));
            }

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(j => j.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.BranchId.HasValue)
            {
                query = query.Where(j => j.Department.BranchId == filter.BranchId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(j => j.Status == filter.Status);
            }

            int totalCount = await query.CountAsync();

            int page = filter.Page > 0 ? filter.Page : 1;
            int pageSize = filter.PageSize > 0 ? filter.PageSize : 20;

            var items = await query
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy SEO Meta tương ứng
            var jobIds = items.Select(j => j.Id).ToList();
            var seoMetas = await _context.SeoMetas
                .Where(s => s.EntityType == "JobPosting" && jobIds.Contains(s.EntityId) && !s.IsDeleted)
                .ToDictionaryAsync(s => s.EntityId, s => s);

            var dtos = items.Select(j =>
            {
                seoMetas.TryGetValue(j.Id, out var seo);
                return new JobPostingDto
                {
                    Id = j.Id,
                    Code = j.Code,
                    Title = j.Title,
                    Slug = j.Slug,
                    DepartmentId = j.DepartmentId,
                    DepartmentName = j.Department?.Name ?? string.Empty,
                    PositionId = j.PositionId,
                    PositionName = j.Position?.Name ?? string.Empty,
                    BranchId = j.Department?.BranchId,
                    BranchName = j.Department?.Branch?.Name ?? string.Empty,
                    Vacancies = j.Vacancies,
                    Description = j.Description,
                    Requirements = j.Requirements,
                    SalaryRange = j.SalaryRange,
                    WorkLocation = j.WorkLocation,
                    Status = j.Status,
                    OpenDate = j.OpenDate,
                    CloseDate = j.CloseDate,
                    CreatedAt = j.CreatedAt,
                    CandidateCount = j.Candidates.Count(c => !c.IsDeleted),
                    MetaTitle = seo?.MetaTitle,
                    MetaDescription = seo?.MetaDescription,
                    MetaKeywords = seo?.MetaKeywords,
                    OgImage = seo?.OgImage
                };
            }).ToList();

            return PagedResult<JobPostingDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<JobPostingDto?> GetJobPostingByIdAsync(int id)
        {
            var j = await _context.JobPostings
                .Include(x => x.Department)
                    .ThenInclude(d => d.Branch)
                .Include(x => x.Position)
                .Include(x => x.Candidates)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (j == null) return null;

            var seo = await _context.SeoMetas
                .FirstOrDefaultAsync(s => s.EntityType == "JobPosting" && s.EntityId == j.Id && !s.IsDeleted);

            return new JobPostingDto
            {
                Id = j.Id,
                Code = j.Code,
                Title = j.Title,
                Slug = j.Slug,
                DepartmentId = j.DepartmentId,
                DepartmentName = j.Department?.Name ?? string.Empty,
                PositionId = j.PositionId,
                PositionName = j.Position?.Name ?? string.Empty,
                BranchId = j.Department?.BranchId,
                BranchName = j.Department?.Branch?.Name ?? string.Empty,
                Vacancies = j.Vacancies,
                Description = j.Description,
                Requirements = j.Requirements,
                SalaryRange = j.SalaryRange,
                WorkLocation = j.WorkLocation,
                Status = j.Status,
                OpenDate = j.OpenDate,
                CloseDate = j.CloseDate,
                CreatedAt = j.CreatedAt,
                CandidateCount = j.Candidates.Count(c => !c.IsDeleted),
                MetaTitle = seo?.MetaTitle,
                MetaDescription = seo?.MetaDescription,
                MetaKeywords = seo?.MetaKeywords,
                OgImage = seo?.OgImage
            };
        }

        public async Task<JobPostingDto?> GetJobPostingBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            var j = await _context.JobPostings
                .Include(x => x.Department)
                    .ThenInclude(d => d.Branch)
                .Include(x => x.Position)
                .Include(x => x.Candidates)
                .FirstOrDefaultAsync(x => x.Slug == slug.Trim().ToLowerInvariant() && !x.IsDeleted);

            if (j == null) return null;

            var seo = await _context.SeoMetas
                .FirstOrDefaultAsync(s => s.EntityType == "JobPosting" && s.EntityId == j.Id && !s.IsDeleted);

            return new JobPostingDto
            {
                Id = j.Id,
                Code = j.Code,
                Title = j.Title,
                Slug = j.Slug,
                DepartmentId = j.DepartmentId,
                DepartmentName = j.Department?.Name ?? string.Empty,
                PositionId = j.PositionId,
                PositionName = j.Position?.Name ?? string.Empty,
                BranchId = j.Department?.BranchId,
                BranchName = j.Department?.Branch?.Name ?? string.Empty,
                Vacancies = j.Vacancies,
                Description = j.Description,
                Requirements = j.Requirements,
                SalaryRange = j.SalaryRange,
                WorkLocation = j.WorkLocation,
                Status = j.Status,
                OpenDate = j.OpenDate,
                CloseDate = j.CloseDate,
                CreatedAt = j.CreatedAt,
                CandidateCount = j.Candidates.Count(c => !c.IsDeleted),
                MetaTitle = seo?.MetaTitle,
                MetaDescription = seo?.MetaDescription,
                MetaKeywords = seo?.MetaKeywords,
                OgImage = seo?.OgImage
            };
        }

        public async Task<Result<int>> CreateJobPostingAsync(JobPostingCreateEditDto dto, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return Result<int>.Failure("Tiêu đề tin tuyển dụng không được để trống.");
            }

            // Sinh slug chuẩn SEO
            string baseSlug = string.IsNullOrWhiteSpace(dto.Slug)
                ? SlugHelper.GenerateSlug(dto.Title)
                : SlugHelper.GenerateSlug(dto.Slug);

            string finalSlug = baseSlug;
            int counter = 1;
            while (await _context.JobPostings.AnyAsync(j => j.Slug == finalSlug && !j.IsDeleted))
            {
                finalSlug = $"{baseSlug}-{counter++}";
            }

            string code = $"TD-{VietnamTime.Now:yyMM}-{Random.Shared.Next(1000, 9999)}";

            var job = new JobPosting
            {
                Code = code,
                Title = dto.Title.Trim(),
                Slug = finalSlug,
                DepartmentId = dto.DepartmentId,
                PositionId = dto.PositionId,
                Vacancies = dto.Vacancies,
                Description = dto.Description,
                Requirements = dto.Requirements,
                SalaryRange = dto.SalaryRange,
                WorkLocation = dto.WorkLocation,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Open" : dto.Status,
                OpenDate = dto.OpenDate,
                CloseDate = dto.CloseDate,
                CreatedAt = VietnamTime.Now,
                CreatedBy = currentUserId.ToString()
            };

            _context.JobPostings.Add(job);
            await _context.SaveChangesAsync();

            // Tạo SeoMeta
            var seo = new SeoMeta
            {
                EntityType = "JobPosting",
                EntityId = job.Id,
                MetaTitle = string.IsNullOrWhiteSpace(dto.MetaTitle) ? job.Title : dto.MetaTitle,
                MetaDescription = string.IsNullOrWhiteSpace(dto.MetaDescription)
                    ? (job.Description?.Length > 160 ? job.Description[..157] + "..." : job.Description)
                    : dto.MetaDescription,
                MetaKeywords = dto.MetaKeywords,
                CanonicalUrl = $"/tuyen-dung/{job.Slug}",
                OgImage = dto.OgImage,
                CreatedAt = VietnamTime.Now,
                CreatedBy = currentUserId.ToString()
            };
            _context.SeoMetas.Add(seo);
            await _context.SaveChangesAsync();

            return Result<int>.Success(job.Id);
        }

        public async Task<Result> UpdateJobPostingAsync(JobPostingCreateEditDto dto, int currentUserId)
        {
            var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == dto.Id && !j.IsDeleted);
            if (job == null) return Result.Failure("Không tìm thấy tin tuyển dụng.");

            // Slug
            string baseSlug = string.IsNullOrWhiteSpace(dto.Slug)
                ? SlugHelper.GenerateSlug(dto.Title)
                : SlugHelper.GenerateSlug(dto.Slug);

            string finalSlug = baseSlug;
            int counter = 1;
            while (await _context.JobPostings.AnyAsync(j => j.Slug == finalSlug && j.Id != job.Id && !j.IsDeleted))
            {
                finalSlug = $"{baseSlug}-{counter++}";
            }

            job.Title = dto.Title.Trim();
            job.Slug = finalSlug;
            job.DepartmentId = dto.DepartmentId;
            job.PositionId = dto.PositionId;
            job.Vacancies = dto.Vacancies;
            job.Description = dto.Description;
            job.Requirements = dto.Requirements;
            job.SalaryRange = dto.SalaryRange;
            job.WorkLocation = dto.WorkLocation;
            job.Status = dto.Status;
            job.OpenDate = dto.OpenDate;
            job.CloseDate = dto.CloseDate;
            job.UpdatedAt = VietnamTime.Now;
            job.UpdatedBy = currentUserId.ToString();

            // Cập nhật SeoMeta
            var seo = await _context.SeoMetas
                .FirstOrDefaultAsync(s => s.EntityType == "JobPosting" && s.EntityId == job.Id && !s.IsDeleted);

            if (seo == null)
            {
                seo = new SeoMeta
                {
                    EntityType = "JobPosting",
                    EntityId = job.Id,
                    CreatedAt = VietnamTime.Now,
                    CreatedBy = currentUserId.ToString()
                };
                _context.SeoMetas.Add(seo);
            }

            seo.MetaTitle = string.IsNullOrWhiteSpace(dto.MetaTitle) ? job.Title : dto.MetaTitle;
            seo.MetaDescription = string.IsNullOrWhiteSpace(dto.MetaDescription)
                ? (job.Description?.Length > 160 ? job.Description[..157] + "..." : job.Description)
                : dto.MetaDescription;
            seo.MetaKeywords = dto.MetaKeywords;
            seo.CanonicalUrl = $"/tuyen-dung/{job.Slug}";
            seo.OgImage = dto.OgImage;
            seo.UpdatedAt = VietnamTime.Now;
            seo.UpdatedBy = currentUserId.ToString();

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> DeleteJobPostingAsync(int id, int currentUserId)
        {
            var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);
            if (job == null) return Result.Failure("Không tìm thấy tin tuyển dụng.");

            job.IsDeleted = true;
            job.UpdatedAt = VietnamTime.Now;
            job.UpdatedBy = currentUserId.ToString();

            var seo = await _context.SeoMetas
                .FirstOrDefaultAsync(s => s.EntityType == "JobPosting" && s.EntityId == job.Id && !s.IsDeleted);
            if (seo != null)
            {
                seo.IsDeleted = true;
                seo.UpdatedAt = VietnamTime.Now;
                seo.UpdatedBy = currentUserId.ToString();
            }

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        #endregion

        #region Kanban Pipeline & Candidates

        public async Task<List<CandidateKanbanDto>> GetKanbanCandidatesAsync(int? jobPostingId, string? keyword, decimal? minScore)
        {
            var query = _context.Candidates
                .Include(c => c.JobPosting)
                    .ThenInclude(j => j.Department)
                .Include(c => c.InterviewSchedules)
                .Where(c => !c.IsDeleted)
                .AsNoTracking();

            if (jobPostingId.HasValue && jobPostingId.Value > 0)
            {
                query = query.Where(c => c.JobPostingId == jobPostingId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(kw) ||
                                         (c.Email != null && c.Email.ToLower().Contains(kw)) ||
                                         (c.Phone != null && c.Phone.Contains(kw)));
            }

            if (minScore.HasValue)
            {
                query = query.Where(c => c.EvaluationScore >= minScore.Value);
            }

            var list = await query
                .OrderByDescending(c => c.AppliedDate)
                .ToListAsync();

            return list.Select(c =>
            {
                var upcomingInterview = c.InterviewSchedules
                    .Where(i => !i.IsDeleted && i.Status == "Scheduled" && i.ScheduledTime >= VietnamTime.Now)
                    .OrderBy(i => i.ScheduledTime)
                    .FirstOrDefault();

                return new CandidateKanbanDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    FullName = c.FullName,
                    Email = c.Email,
                    Phone = c.Phone,
                    ResumeUrl = c.ResumeUrl,
                    CoverLetter = c.CoverLetter,
                    Status = c.Status,
                    EvaluationScore = c.EvaluationScore,
                    Note = c.Note,
                    AppliedDate = c.AppliedDate,
                    JobPostingId = c.JobPostingId,
                    JobPostingTitle = c.JobPosting?.Title ?? string.Empty,
                    DepartmentName = c.JobPosting?.Department?.Name ?? string.Empty,
                    InterviewCount = c.InterviewSchedules.Count(i => !i.IsDeleted),
                    NextInterviewDate = upcomingInterview?.ScheduledTime
                };
            }).ToList();
        }

        public async Task<CandidateKanbanDto?> GetCandidateByIdAsync(int candidateId)
        {
            var c = await _context.Candidates
                .Include(x => x.JobPosting)
                    .ThenInclude(j => j.Department)
                .Include(x => x.InterviewSchedules)
                .FirstOrDefaultAsync(x => x.Id == candidateId && !x.IsDeleted);

            if (c == null) return null;

            return new CandidateKanbanDto
            {
                Id = c.Id,
                Code = c.Code,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone,
                ResumeUrl = c.ResumeUrl,
                CoverLetter = c.CoverLetter,
                Status = c.Status,
                EvaluationScore = c.EvaluationScore,
                Note = c.Note,
                AppliedDate = c.AppliedDate,
                JobPostingId = c.JobPostingId,
                JobPostingTitle = c.JobPosting?.Title ?? string.Empty,
                DepartmentName = c.JobPosting?.Department?.Name ?? string.Empty,
                InterviewCount = c.InterviewSchedules.Count(i => !i.IsDeleted)
            };
        }

        public async Task<Result> UpdateCandidateStatusAsync(UpdateCandidateStatusDto dto, int currentUserId)
        {
            var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Id == dto.CandidateId && !c.IsDeleted);
            if (candidate == null) return Result.Failure("Không tìm thấy ứng viên.");

            string[] allowedStatuses = { "Applied", "Screening", "Interview", "Offered", "Hired", "Rejected" };
            if (!allowedStatuses.Contains(dto.NewStatus))
            {
                return Result.Failure($"Trạng thái '{dto.NewStatus}' không hợp lệ.");
            }

            candidate.Status = dto.NewStatus;
            if (!string.IsNullOrWhiteSpace(dto.Note))
            {
                candidate.Note = dto.Note;
            }
            if (dto.EvaluationScore.HasValue)
            {
                candidate.EvaluationScore = dto.EvaluationScore.Value;
            }

            candidate.UpdatedAt = VietnamTime.Now;
            candidate.UpdatedBy = currentUserId.ToString();

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        #endregion

        #region Interview Scheduling & Evaluation

        public async Task<Result<int>> ScheduleInterviewAsync(InterviewScheduleCreateDto dto, int currentUserId)
        {
            var candidate = await _context.Candidates
                .Include(c => c.JobPosting)
                .FirstOrDefaultAsync(c => c.Id == dto.CandidateId && !c.IsDeleted);

            if (candidate == null) return Result<int>.Failure("Không tìm thấy thông tin ứng viên.");

            var schedule = new InterviewSchedule
            {
                CandidateId = dto.CandidateId,
                RoundNumber = dto.RoundNumber > 0 ? dto.RoundNumber : 1,
                Title = dto.Title,
                ScheduledTime = dto.ScheduledTime,
                DurationMinutes = dto.DurationMinutes,
                InterviewType = dto.InterviewType,
                LocationOrMeetingLink = dto.LocationOrMeetingLink,
                InterviewerUserId = dto.InterviewerUserId,
                InterviewerEmployeeId = dto.InterviewerEmployeeId,
                Status = "Scheduled",
                Result = "Pending",
                CreatedAt = VietnamTime.Now,
                CreatedBy = currentUserId.ToString()
            };

            _context.InterviewSchedules.Add(schedule);

            // Tự động chuyển ứng viên sang trạng thái 'Interview' nếu đang ở Applied/Screening
            if (candidate.Status == "Applied" || candidate.Status == "Screening")
            {
                candidate.Status = "Interview";
                candidate.UpdatedAt = VietnamTime.Now;
                candidate.UpdatedBy = currentUserId.ToString();
            }

            await _context.SaveChangesAsync();

            // Gửi thông báo real-time qua SignalR cho người phỏng vấn nếu có
            if (dto.InterviewerUserId.HasValue)
            {
                try
                {
                    string timeFormatted = dto.ScheduledTime.ToString("HH:mm dd/MM/yyyy");
                    await _notificationService.SendToUserAsync(
                        dto.InterviewerUserId.Value,
                        $"Lịch phỏng vấn: {candidate.FullName}",
                        $"Bạn có lịch phỏng vấn ứng viên {candidate.FullName} ({candidate.JobPosting?.Title}) vào lúc {timeFormatted}.",
                        $"/TuyenDung/Kanban?jobPostingId={candidate.JobPostingId}",
                        "INTERVIEW");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể gửi SignalR notification cho interviewer {UserId}", dto.InterviewerUserId.Value);
                }
            }

            return Result<int>.Success(schedule.Id);
        }

        public async Task<Result> EvaluateInterviewAsync(InterviewEvaluationDto dto, int currentUserId)
        {
            var schedule = await _context.InterviewSchedules
                .Include(s => s.Candidate)
                .FirstOrDefaultAsync(s => s.Id == dto.ScheduleId && !s.IsDeleted);

            if (schedule == null) return Result.Failure("Không tìm thấy lịch phỏng vấn.");

            schedule.Score = dto.Score;
            schedule.Feedback = dto.Feedback;
            schedule.Result = dto.Result;
            schedule.Status = "Completed";
            schedule.UpdatedAt = VietnamTime.Now;
            schedule.UpdatedBy = currentUserId.ToString();

            // Cập nhật điểm đánh giá cho ứng viên
            if (dto.Score.HasValue)
            {
                schedule.Candidate.EvaluationScore = dto.Score.Value;
                schedule.Candidate.UpdatedAt = VietnamTime.Now;
                schedule.Candidate.UpdatedBy = currentUserId.ToString();
            }

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<List<InterviewScheduleDto>> GetInterviewSchedulesAsync(int? candidateId, int? interviewerUserId)
        {
            var query = _context.InterviewSchedules
                .Include(s => s.Candidate)
                    .ThenInclude(c => c.JobPosting)
                .Include(s => s.InterviewerUser)
                .Where(s => !s.IsDeleted)
                .AsNoTracking();

            if (candidateId.HasValue)
            {
                query = query.Where(s => s.CandidateId == candidateId.Value);
            }

            if (interviewerUserId.HasValue)
            {
                query = query.Where(s => s.InterviewerUserId == interviewerUserId.Value);
            }

            var list = await query
                .OrderByDescending(s => s.ScheduledTime)
                .ToListAsync();

            return list.Select(s => new InterviewScheduleDto
            {
                Id = s.Id,
                CandidateId = s.CandidateId,
                CandidateName = s.Candidate?.FullName ?? string.Empty,
                CandidateEmail = s.Candidate?.Email,
                CandidatePhone = s.Candidate?.Phone,
                JobPostingTitle = s.Candidate?.JobPosting?.Title ?? string.Empty,
                RoundNumber = s.RoundNumber,
                Title = s.Title,
                ScheduledTime = s.ScheduledTime,
                DurationMinutes = s.DurationMinutes,
                InterviewType = s.InterviewType,
                LocationOrMeetingLink = s.LocationOrMeetingLink,
                InterviewerUserId = s.InterviewerUserId,
                InterviewerName = s.InterviewerUser?.Employee?.FullName ?? s.InterviewerUser?.Username,
                Status = s.Status,
                Score = s.Score,
                Feedback = s.Feedback,
                Result = s.Result
            }).ToList();
        }

        #endregion

        #region Golden Path: Convert Candidate to Employee & Onboarding

        public async Task<Result<int>> ConvertToEmployeeAsync(ConvertCandidateToEmployeeDto dto, int currentUserId)
        {
            var candidate = await _context.Candidates
                .Include(c => c.JobPosting)
                .FirstOrDefaultAsync(c => c.Id == dto.CandidateId && !c.IsDeleted);

            if (candidate == null)
            {
                return Result<int>.Failure("Không tìm thấy ứng viên để chuyển đổi.");
            }

            // Sinh mã nhân viên mới
            string empCode = $"NV-{VietnamTime.Now:yyMMdd}-{Random.Shared.Next(100, 999)}";
            while (await _context.Employees.AnyAsync(e => e.EmployeeCode == empCode))
            {
                empCode = $"NV-{VietnamTime.Now:yyMMdd}-{Random.Shared.Next(100, 999)}";
            }

            // 1. Tạo hồ sơ Employee mới trạng thái Probation
            var employee = new Employee
            {
                EmployeeCode = empCode,
                FullName = dto.FullName.Trim(),
                DepartmentId = dto.DepartmentId,
                PositionId = dto.PositionId,
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                Status = "Probation", // Thử việc
                HireDate = dto.HireDate,
                CreatedAt = VietnamTime.Now,
                CreatedBy = currentUserId.ToString()
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // 2. Tạo Hợp đồng thử việc dự thảo (Contract)
            string contractNumber = $"HDTV-{VietnamTime.Now:yyyyMMdd}-{employee.Id}";
            var contract = new Contract
            {
                EmployeeId = employee.Id,
                ContractNumber = contractNumber,
                ContractType = "Probation",
                StartDate = dto.HireDate,
                EndDate = dto.HireDate.AddMonths(dto.ProbationMonths > 0 ? dto.ProbationMonths : 2),
                BasicSalary = dto.BaseSalary > 0 ? dto.BaseSalary : 10000000,
                InsuranceSalary = (dto.BaseSalary > 0 ? dto.BaseSalary : 10000000) * 0.85m,
                Status = "Draft",
                SignDate = dto.HireDate,
                Note = $"Hợp đồng thử việc tự động sinh từ ứng viên {candidate.FullName} ({candidate.Code})",
                CreatedAt = VietnamTime.Now
            };

            _context.Contracts.Add(contract);

            // 3. Cập nhật trạng thái ứng viên thành Hired
            candidate.Status = "Hired";
            candidate.Note = $"Đã tiếp nhận thành nhân sự mã {employee.EmployeeCode}.";
            candidate.UpdatedAt = VietnamTime.Now;
            candidate.UpdatedBy = currentUserId.ToString();

            await _context.SaveChangesAsync();

            // 4. Khởi tạo Onboarding Checklist tự động (4 task chuẩn)
            var checklistDto = new CreateChecklistDto
            {
                EmployeeId = employee.Id,
                Type = "Onboarding",
                Title = $"Quy trình hội nhập - {employee.FullName} ({employee.EmployeeCode})",
                StartDate = dto.HireDate,
                TargetCompletionDate = dto.HireDate.AddDays(14),
                AssignedHrId = dto.AssignedHrUserId,
                Note = "Quy trình hội nhập tự động sinh sau khi tiếp nhận ứng viên trúng tuyển."
            };

            await _onboardingService.CreateChecklistAsync(checklistDto, currentUserId);

            return Result<int>.Success(employee.Id);
        }

        #endregion

        #region Public Careers Portal & CV Upload

        public async Task<PublicCareersIndexViewModel> GetPublicCareersIndexAsync(string? keyword, int? departmentId, int? branchId)
        {
            var query = _context.JobPostings
                .Include(j => j.Department)
                    .ThenInclude(d => d.Branch)
                .Include(j => j.Position)
                .Where(j => !j.IsDeleted && j.Status == "Open")
                .AsNoTracking();

            // Lấy danh sách phòng ban có tin tuyển dụng
            var departments = await _context.Departments
                .Where(d => d.IsActive)
                .Select(d => new DepartmentSelectDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    OpenJobCount = _context.JobPostings.Count(j => j.DepartmentId == d.Id && !j.IsDeleted && j.Status == "Open")
                })
                .Where(d => d.OpenJobCount > 0)
                .ToListAsync();

            var branches = await _context.Branches
                .Where(b => b.IsActive)
                .Select(b => new BranchSelectDto
                {
                    Id = b.Id,
                    Name = b.Name
                })
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(j => j.Title.ToLower().Contains(kw) ||
                                         (j.Description != null && j.Description.ToLower().Contains(kw)) ||
                                         (j.Requirements != null && j.Requirements.ToLower().Contains(kw)));
            }

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(j => j.DepartmentId == departmentId.Value);
            }

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(j => j.Department.BranchId == branchId.Value);
            }

            var jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var jobDtos = jobs.Select(j => new JobPostingDto
            {
                Id = j.Id,
                Code = j.Code,
                Title = j.Title,
                Slug = j.Slug,
                DepartmentId = j.DepartmentId,
                DepartmentName = j.Department?.Name ?? string.Empty,
                PositionId = j.PositionId,
                PositionName = j.Position?.Name ?? string.Empty,
                BranchId = j.Department?.BranchId,
                BranchName = j.Department?.Branch?.Name ?? string.Empty,
                Vacancies = j.Vacancies,
                Description = j.Description,
                Requirements = j.Requirements,
                SalaryRange = j.SalaryRange,
                WorkLocation = j.WorkLocation,
                Status = j.Status,
                OpenDate = j.OpenDate,
                CloseDate = j.CloseDate,
                CreatedAt = j.CreatedAt
            }).ToList();

            return new PublicCareersIndexViewModel
            {
                Keyword = keyword,
                DepartmentId = departmentId,
                BranchId = branchId,
                Departments = departments,
                Branches = branches,
                OpenJobs = jobDtos,
                TotalOpenings = jobDtos.Sum(j => j.Vacancies)
            };
        }

        public async Task<PublicJobDetailViewModel?> GetPublicJobDetailAsync(string slug)
        {
            var jobDto = await GetJobPostingBySlugAsync(slug);
            if (jobDto == null || jobDto.Status != "Open") return null;

            var relatedJobs = await _context.JobPostings
                .Include(j => j.Department)
                .Include(j => j.Position)
                .Where(j => !j.IsDeleted && j.Status == "Open" && j.Id != jobDto.Id && j.DepartmentId == jobDto.DepartmentId)
                .OrderByDescending(j => j.CreatedAt)
                .Take(4)
                .Select(j => new JobPostingDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    Slug = j.Slug,
                    DepartmentName = j.Department.Name,
                    SalaryRange = j.SalaryRange,
                    WorkLocation = j.WorkLocation
                })
                .ToListAsync();

            var company = await _context.Companies.FirstOrDefaultAsync();
            string companyName = company?.Name ?? "Tập Đoàn Doanh Nghiệp";
            string companyAddress = company?.Address ?? "Hà Nội, Việt Nam";

            // Sinh JSON-LD Google for Jobs schema
            string jsonLd = BuildJobPostingJsonLd(jobDto, companyName, companyAddress);

            return new PublicJobDetailViewModel
            {
                Job = jobDto,
                RelatedJobs = relatedJobs,
                CompanyName = companyName,
                CompanyAddress = companyAddress,
                CanonicalUrl = $"/tuyen-dung/{jobDto.Slug}",
                JsonLdSchema = jsonLd
            };
        }

        public async Task<Result<int>> SubmitPublicApplicationAsync(PublicApplyCandidateDto dto, string? clientIp)
        {
            // 1. Kiểm tra honeypot chống bot tự động
            if (!string.IsNullOrWhiteSpace(dto.Website))
            {
                _logger.LogWarning("Spam bot detected via honeypot. IP: {Ip}", clientIp);
                return Result<int>.Failure("Hồ sơ không thể gửi. Yêu cầu bị từ chối.");
            }

            // 2. Rate limiting theo IP (tối đa 5 lượt nộp / 10 phút)
            string cacheKey = $"rate_limit_apply_{clientIp ?? "unknown"}";
            if (_cache.TryGetValue(cacheKey, out int count) && count >= 5)
            {
                return Result<int>.Failure("Bạn đã nộp hồ sơ quá số lần quy định trong thời gian ngắn. Vui lòng thử lại sau 10 phút.");
            }

            var job = await _context.JobPostings.FirstOrDefaultAsync(j => j.Id == dto.JobPostingId && !j.IsDeleted && j.Status == "Open");
            if (job == null)
            {
                return Result<int>.Failure("Tin tuyển dụng này hiện không mở hoặc đã hết hạn nhận hồ sơ.");
            }

            // 3. Kiểm tra file CV (Magic Bytes, Extension, Size)
            var (isValidFile, fileError) = FileUploadValidator.ValidateCvFile(dto.CvFile);
            if (!isValidFile)
            {
                return Result<int>.Failure(fileError ?? "Tập tin hồ sơ không hợp lệ.");
            }

            // 4. Lưu file CV an toàn
            string uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "cvs");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string ext = Path.GetExtension(dto.CvFile!.FileName).ToLowerInvariant();
            string safeFileName = $"cv_{Guid.NewGuid():N}{ext}";
            string filePath = Path.Combine(uploadsFolder, safeFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.CvFile.CopyToAsync(stream);
            }

            string resumeUrl = $"/uploads/cvs/{safeFileName}";

            // 5. Lưu Candidate vào database
            string candCode = $"UV-{VietnamTime.Now:yyMM}-{Random.Shared.Next(1000, 9999)}";
            var candidate = new Candidate
            {
                Code = candCode,
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Phone = dto.Phone.Trim(),
                CoverLetter = dto.CoverLetter?.Trim(),
                ResumeUrl = resumeUrl,
                JobPostingId = dto.JobPostingId,
                Status = "Applied", // Cột đầu tiên trong Kanban ATS
                AppliedDate = VietnamTime.Now,
                CreatedAt = VietnamTime.Now
            };

            _context.Candidates.Add(candidate);
            await _context.SaveChangesAsync();

            // Cập nhật rate limit đếm số lần
            _cache.Set(cacheKey, count + 1, TimeSpan.FromMinutes(10));

            return Result<int>.Success(candidate.Id);
        }

        private static string BuildJobPostingJsonLd(JobPostingDto job, string companyName, string companyAddress)
        {
            var schema = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org/",
                ["@type"] = "JobPosting",
                ["title"] = job.Title,
                ["description"] = job.Description ?? job.Title,
                ["datePosted"] = job.OpenDate.ToString("yyyy-MM-dd"),
                ["validThrough"] = (job.CloseDate ?? job.OpenDate.AddMonths(1)).ToString("yyyy-MM-dd"),
                ["employmentType"] = "FULL_TIME",
                ["hiringOrganization"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Organization",
                    ["name"] = companyName,
                    ["sameAs"] = "/"
                },
                ["jobLocation"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Place",
                    ["address"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "PostalAddress",
                        ["streetAddress"] = companyAddress,
                        ["addressLocality"] = job.WorkLocation ?? "Hà Nội",
                        ["addressCountry"] = "VN"
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(job.SalaryRange))
            {
                schema["baseSalary"] = new Dictionary<string, object?>
                {
                    ["@type"] = "MonetaryAmount",
                    ["currency"] = "VND",
                    ["value"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "QuantitativeValue",
                        ["value"] = job.SalaryRange,
                        ["unitText"] = "MONTH"
                    }
                };
            }

            return JsonSerializer.Serialize(schema, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        #endregion
    }
}
