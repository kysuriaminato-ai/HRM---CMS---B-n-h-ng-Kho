using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Common.Models;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP_HRM_CMS.Services.HRM
{
    public class TrainingService : ITrainingService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<TrainingService> _logger;

        public TrainingService(
            AppDbContext context,
            INotificationService notificationService,
            ILogger<TrainingService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<List<TrainingDto>> GetTrainingsAsync(string? keyword, string? status)
        {
            var query = _context.Trainings
                .Include(t => t.Participants)
                    .ThenInclude(p => p.Employee)
                        .ThenInclude(e => e.Department)
                .Where(t => !t.IsDeleted)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(kw) || t.Code.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var list = await query
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            return list.Select(t => new TrainingDto
            {
                Id = t.Id,
                Code = t.Code,
                Title = t.Title,
                TrainerName = t.TrainerName,
                Location = t.Location,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Budget = t.Budget,
                Status = t.Status,
                ParticipantCount = t.Participants.Count,
                PassedCount = t.Participants.Count(p => p.Result == "Passed"),
                CreatedAt = t.CreatedAt,
                Participants = t.Participants.Select(p => new TrainingParticipantDto
                {
                    TrainingId = p.TrainingId,
                    TrainingTitle = t.Title,
                    EmployeeId = p.EmployeeId,
                    EmployeeName = p.Employee?.FullName ?? string.Empty,
                    EmployeeCode = p.Employee?.EmployeeCode ?? string.Empty,
                    DepartmentName = p.Employee?.Department?.Name ?? string.Empty,
                    Result = p.Result,
                    Score = p.Score,
                    Comment = p.Comment
                }).ToList()
            }).ToList();
        }

        public async Task<TrainingDto?> GetTrainingByIdAsync(int id)
        {
            var t = await _context.Trainings
                .Include(x => x.Participants)
                    .ThenInclude(p => p.Employee)
                        .ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (t == null) return null;

            return new TrainingDto
            {
                Id = t.Id,
                Code = t.Code,
                Title = t.Title,
                TrainerName = t.TrainerName,
                Location = t.Location,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Budget = t.Budget,
                Status = t.Status,
                ParticipantCount = t.Participants.Count,
                PassedCount = t.Participants.Count(p => p.Result == "Passed"),
                CreatedAt = t.CreatedAt,
                Participants = t.Participants.Select(p => new TrainingParticipantDto
                {
                    TrainingId = p.TrainingId,
                    TrainingTitle = t.Title,
                    EmployeeId = p.EmployeeId,
                    EmployeeName = p.Employee?.FullName ?? string.Empty,
                    EmployeeCode = p.Employee?.EmployeeCode ?? string.Empty,
                    DepartmentName = p.Employee?.Department?.Name ?? string.Empty,
                    Result = p.Result,
                    Score = p.Score,
                    Comment = p.Comment
                }).ToList()
            };
        }

        public async Task<Result<int>> CreateTrainingAsync(TrainingCreateEditDto dto, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return Result<int>.Failure("Tiêu đề khóa đào tạo không được để trống.");
            }

            string code = $"DT-{VietnamTime.Now:yyMM}-{Random.Shared.Next(100, 999)}";

            var training = new Training
            {
                Code = code,
                Title = dto.Title.Trim(),
                TrainerName = dto.TrainerName,
                Location = dto.Location,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate ?? dto.StartDate.AddDays(1),
                Budget = dto.Budget,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Planned" : dto.Status,
                CreatedAt = VietnamTime.Now,
                CreatedBy = currentUserId.ToString()
            };

            _context.Trainings.Add(training);
            await _context.SaveChangesAsync();

            return Result<int>.Success(training.Id);
        }

        public async Task<Result> UpdateTrainingAsync(TrainingCreateEditDto dto, int currentUserId)
        {
            var training = await _context.Trainings.FirstOrDefaultAsync(t => t.Id == dto.Id && !t.IsDeleted);
            if (training == null) return Result.Failure("Không tìm thấy khóa đào tạo.");

            training.Title = dto.Title.Trim();
            training.TrainerName = dto.TrainerName;
            training.Location = dto.Location;
            training.Description = dto.Description;
            training.StartDate = dto.StartDate;
            training.EndDate = dto.EndDate;
            training.Budget = dto.Budget;
            training.Status = dto.Status;
            training.UpdatedAt = VietnamTime.Now;
            training.UpdatedBy = currentUserId.ToString();

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> DeleteTrainingAsync(int id, int currentUserId)
        {
            var training = await _context.Trainings.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (training == null) return Result.Failure("Không tìm thấy khóa đào tạo.");

            training.IsDeleted = true;
            training.UpdatedAt = VietnamTime.Now;
            training.UpdatedBy = currentUserId.ToString();

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> AssignParticipantsAsync(AssignTrainingParticipantsDto dto, int currentUserId)
        {
            var training = await _context.Trainings
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Id == dto.TrainingId && !t.IsDeleted);

            if (training == null) return Result.Failure("Không tìm thấy khóa đào tạo.");

            var employeeIds = new HashSet<int>(dto.EmployeeIds ?? new List<int>());

            // Nếu chỉ định phòng ban, lấy toàn bộ nhân viên đang hoạt động trong phòng ban đó
            if (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0)
            {
                var deptEmpIds = await _context.Employees
                    .Where(e => e.DepartmentId == dto.DepartmentId.Value && e.Status == "Active" && !e.IsDeleted)
                    .Select(e => e.Id)
                    .ToListAsync();

                foreach (var id in deptEmpIds)
                {
                    employeeIds.Add(id);
                }
            }

            int addedCount = 0;
            foreach (var empId in employeeIds)
            {
                if (!training.Participants.Any(p => p.EmployeeId == empId))
                {
                    training.Participants.Add(new TrainingParticipant
                    {
                        TrainingId = training.Id,
                        EmployeeId = empId,
                        Result = "Attended",
                        Score = null
                    });
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                training.UpdatedAt = VietnamTime.Now;
                training.UpdatedBy = currentUserId.ToString();
                await _context.SaveChangesAsync();
            }

            return Result.Success();
        }

        public async Task<Result> GradeParticipantAsync(GradeParticipantDto dto, int currentUserId)
        {
            var participant = await _context.TrainingParticipants
                .Include(p => p.Training)
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.TrainingId == dto.TrainingId && p.EmployeeId == dto.EmployeeId);

            if (participant == null)
            {
                return Result.Failure("Học viên không có trong danh sách khóa đào tạo này.");
            }

            participant.Score = dto.Score;
            bool isPassed = dto.Score >= 60;
            participant.Result = !string.IsNullOrWhiteSpace(dto.Result) ? dto.Result : (isPassed ? "Passed" : "Failed");
            participant.Comment = dto.Comment;

            // Tự động hóa nâng bậc kỹ năng hoặc cấp chứng chỉ khi đạt Passed
            if (participant.Result == "Passed" && dto.AutoGrantSkillOrCertificate)
            {
                string courseTitle = participant.Training.Title;

                // 1. Kiểm tra hoặc nâng bậc kỹ năng
                var existingSkill = await _context.EmployeeSkills
                    .FirstOrDefaultAsync(s => s.EmployeeId == dto.EmployeeId && s.SkillName.ToLower() == courseTitle.ToLower());

                if (existingSkill == null)
                {
                    _context.EmployeeSkills.Add(new EmployeeSkill
                    {
                        EmployeeId = dto.EmployeeId,
                        SkillName = courseTitle,
                        Level = dto.Score >= 85 ? "Advanced" : "Intermediate",
                        Note = $"Chứng nhận tự động sau khóa học '{courseTitle}' (Điểm: {dto.Score:0.#})",
                        CreatedAt = VietnamTime.Now
                    });
                }
                else
                {
                    // Nâng bậc nếu điểm cao
                    if (existingSkill.Level == "Beginner") existingSkill.Level = "Intermediate";
                    else if (existingSkill.Level == "Intermediate" && dto.Score >= 80) existingSkill.Level = "Advanced";
                    else if (existingSkill.Level == "Advanced" && dto.Score >= 90) existingSkill.Level = "Expert";
                    existingSkill.Note = $"Đã nâng cấp qua khóa học '{courseTitle}' (Điểm: {dto.Score:0.#})";
                }

                // 2. Cấp chứng nhận nội bộ (EmployeeCertificates)
                bool certExists = await _context.EmployeeCertificates
                    .AnyAsync(c => c.EmployeeId == dto.EmployeeId && c.CertificateName.ToLower() == courseTitle.ToLower());

                if (!certExists)
                {
                    _context.EmployeeCertificates.Add(new EmployeeCertificate
                    {
                        EmployeeId = dto.EmployeeId,
                        CertificateName = courseTitle,
                        IssuedBy = participant.Training.TrainerName ?? "Học Viện Nội Bộ ERP Enterprise",
                        IssuedDate = VietnamTime.Today,
                        ExpiryDate = VietnamTime.Today.AddYears(2),
                        Note = $"Cấp chứng chỉ sau khi hoàn thành khóa đào tạo mã {participant.Training.Code} với số điểm {dto.Score:0.#}/100.",
                        CreatedAt = VietnamTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<List<TrainingParticipantDto>> GetEmployeeTrainingsAsync(int employeeId)
        {
            var list = await _context.TrainingParticipants
                .Include(p => p.Training)
                .Where(p => p.EmployeeId == employeeId && !p.Training.IsDeleted)
                .OrderByDescending(p => p.Training.StartDate)
                .AsNoTracking()
                .ToListAsync();

            return list.Select(p => new TrainingParticipantDto
            {
                TrainingId = p.TrainingId,
                TrainingTitle = p.Training.Title,
                EmployeeId = p.EmployeeId,
                Result = p.Result,
                Score = p.Score,
                Comment = p.Comment
            }).ToList();
        }
    }
}

