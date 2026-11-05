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
    public class PerformanceReviewService : IPerformanceReviewService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PerformanceReviewService> _logger;

        public PerformanceReviewService(
            AppDbContext context,
            INotificationService notificationService,
            ILogger<PerformanceReviewService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public (bool IsValid, string? ErrorMessage) ValidateCriteriaWeights(IEnumerable<KpiCriteriaDto> criteria)
        {
            var list = criteria?.ToList() ?? new List<KpiCriteriaDto>();
            if (!list.Any())
            {
                return (false, "Bộ tiêu chí đánh giá KPI không được để trống.");
            }

            if (list.Any(c => c.Weight <= 0))
            {
                return (false, "Trọng số của mỗi tiêu chí phải lớn hơn 0%.");
            }

            decimal totalWeight = list.Sum(c => c.Weight);
            if (Math.Abs(totalWeight - 100m) > 0.01m)
            {
                return (false, $"Tổng trọng số các tiêu chí phải chính xác bằng 100% (Hiện tại là {totalWeight}%).");
            }

            return (true, null);
        }

        public async Task<List<KpiReviewDto>> GetKpiReviewsAsync(int? month, int? year, int? departmentId, string? grade, string? status)
        {
            var query = _context.KpiReviews
                .Include(k => k.Employee)
                    .ThenInclude(e => e.Department)
                .Include(k => k.Employee)
                    .ThenInclude(e => e.Position)
                .Include(k => k.Reviewer)
                    .ThenInclude(r => r.Employee)
                .Include(k => k.Criteria)
                .Where(k => !k.IsDeleted)
                .AsNoTracking();

            if (month.HasValue && month.Value > 0)
            {
                query = query.Where(k => k.PeriodMonth == month.Value);
            }

            if (year.HasValue && year.Value > 0)
            {
                query = query.Where(k => k.PeriodYear == year.Value);
            }

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(k => k.Employee.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(grade))
            {
                query = query.Where(k => k.Grade == grade);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(k => k.Status == status);
            }

            var list = await query
                .OrderByDescending(k => k.PeriodYear)
                .ThenByDescending(k => k.PeriodMonth)
                .ThenByDescending(k => k.TotalScore)
                .ToListAsync();

            return list.Select(MapToDto).ToList();
        }

        public async Task<KpiReviewDto?> GetKpiReviewByIdAsync(int id)
        {
            var item = await _context.KpiReviews
                .Include(k => k.Employee)
                    .ThenInclude(e => e.Department)
                .Include(k => k.Employee)
                    .ThenInclude(e => e.Position)
                .Include(k => k.Reviewer)
                    .ThenInclude(r => r.Employee)
                .Include(k => k.Criteria)
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);

            if (item == null) return null;
            return MapToDto(item);
        }

        public async Task<List<KpiReviewDto>> GetEmployeeKpiHistoryAsync(int employeeId)
        {
            var list = await _context.KpiReviews
                .Include(k => k.Reviewer)
                    .ThenInclude(r => r.Employee)
                .Include(k => k.Criteria)
                .Where(k => k.EmployeeId == employeeId && !k.IsDeleted)
                .OrderByDescending(k => k.PeriodYear)
                .ThenByDescending(k => k.PeriodMonth)
                .AsNoTracking()
                .ToListAsync();

            return list.Select(MapToDto).ToList();
        }

        public async Task<Result<int>> CreateKpiReviewAsync(CreateKpiReviewDto dto, int currentUserId)
        {
            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
            {
                return Result<int>.Failure("Không tìm thấy nhân viên được chỉ định.");
            }

            // Kiểm tra trọng số tổng = 100%
            var (isValid, error) = ValidateCriteriaWeights(dto.Criteria);
            if (!isValid)
            {
                return Result<int>.Failure(error ?? "Trọng số tiêu chí không hợp lệ.");
            }

            // Kiểm tra xem đã có kỳ đánh giá trùng lặp chưa
            bool exists = await _context.KpiReviews
                .AnyAsync(k => k.EmployeeId == dto.EmployeeId &&
                               k.PeriodMonth == dto.PeriodMonth &&
                               k.PeriodYear == dto.PeriodYear &&
                               !k.IsDeleted);

            if (exists)
            {
                return Result<int>.Failure($"Nhân viên {employee.FullName} đã có bản đánh giá KPI cho kỳ Tháng {dto.PeriodMonth:D2}/{dto.PeriodYear}.");
            }

            int reviewerUserId = dto.ReviewerId > 0 ? dto.ReviewerId : currentUserId;

            var review = new KpiReview
            {
                EmployeeId = dto.EmployeeId,
                ReviewerId = reviewerUserId,
                PeriodMonth = dto.PeriodMonth,
                PeriodYear = dto.PeriodYear,
                TotalScore = 0,
                Grade = null,
                Status = "Draft",
                CreatedAt = VietnamTime.Now,
                CreatedBy = currentUserId.ToString()
            };

            foreach (var c in dto.Criteria)
            {
                review.Criteria.Add(new KpiCriteria
                {
                    CriteriaName = c.CriteriaName.Trim(),
                    Weight = c.Weight,
                    Score = 0,
                    Comment = c.Comment
                });
            }

            _context.KpiReviews.Add(review);
            await _context.SaveChangesAsync();

            return Result<int>.Success(review.Id);
        }

        public async Task<Result> SelfEvaluateAsync(SelfEvaluateKpiDto dto, int employeeId)
        {
            var review = await _context.KpiReviews
                .Include(k => k.Criteria)
                .FirstOrDefaultAsync(k => k.Id == dto.KpiReviewId && !k.IsDeleted);

            if (review == null)
            {
                return Result.Failure("Không tìm thấy bản đánh giá KPI.");
            }

            if (review.EmployeeId != employeeId)
            {
                return Result.Failure("Bạn không có quyền tự chấm điểm cho nhân viên khác.");
            }

            foreach (var item in dto.Scores)
            {
                var crit = review.Criteria.FirstOrDefault(c => c.Id == item.CriteriaId);
                if (crit != null)
                {
                    crit.Comment = string.IsNullOrWhiteSpace(item.Comment) ? crit.Comment : item.Comment;
                    if (!string.IsNullOrWhiteSpace(item.EvidenceUrl))
                    {
                        crit.Comment = $"[Minh chứng: {item.EvidenceUrl}] {crit.Comment}";
                    }
                }
            }

            review.OverallComment = dto.EmployeeComment;
            review.Status = "Submitted"; // Đã nộp tự đánh giá
            review.UpdatedAt = VietnamTime.Now;
            review.UpdatedBy = employeeId.ToString();

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ManagerEvaluateAsync(ManagerEvaluateKpiDto dto, int reviewerUserId)
        {
            var review = await _context.KpiReviews
                .Include(k => k.Criteria)
                .Include(k => k.Employee)
                .FirstOrDefaultAsync(k => k.Id == dto.KpiReviewId && !k.IsDeleted);

            if (review == null)
            {
                return Result.Failure("Không tìm thấy bản đánh giá KPI.");
            }

            decimal calculatedTotalScore = 0;

            foreach (var item in dto.Scores)
            {
                var crit = review.Criteria.FirstOrDefault(c => c.Id == item.CriteriaId);
                if (crit != null)
                {
                    crit.Score = Math.Clamp(item.Score, 0, 100);
                    if (!string.IsNullOrWhiteSpace(item.Comment))
                    {
                        crit.Comment = item.Comment;
                    }
                    calculatedTotalScore += (crit.Score * crit.Weight) / 100m;
                }
            }

            review.TotalScore = Math.Round(calculatedTotalScore, 2);

            // Gợi ý xếp loại
            if (review.TotalScore >= 90) review.Grade = "A+";
            else if (review.TotalScore >= 80) review.Grade = "A";
            else if (review.TotalScore >= 65) review.Grade = "B";
            else if (review.TotalScore >= 50) review.Grade = "C";
            else review.Grade = "D";

            string comment = $"[Tiềm năng: {dto.PotentialScore:0.#}/100] {dto.ManagerComment}";
            review.OverallComment = comment;
            review.Status = "Submitted"; // Đã chấm, chờ phê duyệt chốt
            review.UpdatedAt = VietnamTime.Now;
            review.UpdatedBy = reviewerUserId.ToString();

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ApproveKpiAsync(ApproveKpiDto dto, int directorUserId)
        {
            var review = await _context.KpiReviews
                .Include(k => k.Employee)
                .FirstOrDefaultAsync(k => k.Id == dto.KpiReviewId && !k.IsDeleted);

            if (review == null)
            {
                return Result.Failure("Không tìm thấy bản đánh giá KPI.");
            }

            if (dto.IsApproved)
            {
                review.Status = "Approved";
                if (!string.IsNullOrWhiteSpace(dto.Grade))
                {
                    review.Grade = dto.Grade;
                }
            }
            else
            {
                review.Status = "Draft"; // Trả về để đánh giá lại
            }

            if (!string.IsNullOrWhiteSpace(dto.DirectorComment))
            {
                review.OverallComment += $" | [Ý kiến phê duyệt]: {dto.DirectorComment}";
            }

            review.UpdatedAt = VietnamTime.Now;
            review.UpdatedBy = directorUserId.ToString();

            await _context.SaveChangesAsync();

            // Gửi thông báo SignalR cho nhân viên
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == review.EmployeeId);
            if (user != null)
            {
                try
                {
                    await _notificationService.SendToUserAsync(
                        user.Id,
                        $"Kết quả đánh giá KPI Kỳ Tháng {review.PeriodMonth:D2}/{review.PeriodYear}",
                        $"Kết quả KPI của bạn đã được phê duyệt chốt xếp loại: {review.Grade} (Điểm: {review.TotalScore:0.#}).",
                        "/CongNhanVien?tab=profile",
                        "KPI");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể gửi SignalR cho nhân viên {EmpId}", review.EmployeeId);
                }
            }

            return Result.Success();
        }

        public async Task<NineBoxTalentMatrixDto> GetNineBoxMatrixAsync(int month, int year, int? departmentId)
        {
            var reviews = await _context.KpiReviews
                .Include(k => k.Employee)
                    .ThenInclude(e => e.Department)
                .Include(k => k.Employee)
                    .ThenInclude(e => e.Position)
                .Where(k => k.PeriodMonth == month && k.PeriodYear == year && !k.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                reviews = reviews.Where(k => k.Employee.DepartmentId == departmentId.Value).ToList();
            }

            var matrix = new NineBoxTalentMatrixDto
            {
                PeriodMonth = month,
                PeriodYear = year,
                DepartmentId = departmentId
            };

            foreach (var r in reviews)
            {
                decimal perf = r.TotalScore;
                decimal pot = ParsePotential(r.OverallComment);

                string box = CalculateNineBoxCategory(perf, pot);

                var item = new NineBoxItemDto
                {
                    EmployeeId = r.EmployeeId,
                    EmployeeName = r.Employee.FullName,
                    EmployeeCode = r.Employee.EmployeeCode,
                    DepartmentName = r.Employee.Department?.Name ?? string.Empty,
                    PositionName = r.Employee.Position?.Name ?? string.Empty,
                    PerformanceScore = perf,
                    PotentialScore = pot,
                    Grade = r.Grade ?? "Chưa chốt",
                    BoxCategory = box
                };

                switch (box)
                {
                    case "Star": matrix.StarTalents.Add(item); break;
                    case "HighPerformer": matrix.HighPerformers.Add(item); break;
                    case "Specialist": matrix.Specialists.Add(item); break;
                    case "Growth": matrix.GrowthTalents.Add(item); break;
                    case "CoreEmployee": matrix.CoreEmployees.Add(item); break;
                    case "SteadyPerformer": matrix.SteadyPerformers.Add(item); break;
                    case "Enigma": matrix.Enigmas.Add(item); break;
                    case "Dilemma": matrix.Dilemmas.Add(item); break;
                    default: matrix.UnderPerformers.Add(item); break;
                }
            }

            return matrix;
        }

        private static decimal ParsePotential(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment)) return 70;

            int idx = comment.IndexOf("[Tiềm năng: ");
            if (idx >= 0)
            {
                int end = comment.IndexOf("/100", idx);
                if (end > idx)
                {
                    string sub = comment.Substring(idx + 12, end - (idx + 12)).Trim();
                    if (decimal.TryParse(sub, out decimal p)) return p;
                }
            }
            return 70;
        }

        public static string CalculateNineBoxCategory(decimal perf, decimal pot)
        {
            // Performance: Low (< 65), Med (65 - 84.9), High (>= 85)
            // Potential: Low (< 60), Med (60 - 79.9), High (>= 80)
            if (perf >= 85)
            {
                if (pot >= 80) return "Star";
                if (pot >= 60) return "HighPerformer";
                return "Specialist";
            }
            if (perf >= 65)
            {
                if (pot >= 80) return "Growth";
                if (pot >= 60) return "CoreEmployee";
                return "SteadyPerformer";
            }
            // perf < 65
            if (pot >= 80) return "Enigma";
            if (pot >= 60) return "Dilemma";
            return "UnderPerformer";
        }

        private static KpiReviewDto MapToDto(KpiReview r)
        {
            decimal pot = ParsePotential(r.OverallComment);
            string box = CalculateNineBoxCategory(r.TotalScore, pot);

            return new KpiReviewDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee?.FullName ?? string.Empty,
                EmployeeCode = r.Employee?.EmployeeCode ?? string.Empty,
                DepartmentName = r.Employee?.Department?.Name ?? string.Empty,
                PositionName = r.Employee?.Position?.Name ?? string.Empty,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer?.Employee?.FullName ?? r.Reviewer?.Username ?? string.Empty,
                PeriodMonth = r.PeriodMonth,
                PeriodYear = r.PeriodYear,
                TotalScore = r.TotalScore,
                Grade = r.Grade,
                OverallComment = r.OverallComment,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                PotentialScore = pot,
                NineBoxCategory = box,
                Criteria = r.Criteria.Select(c => new KpiCriteriaDto
                {
                    Id = c.Id,
                    KpiReviewId = c.KpiReviewId,
                    CriteriaName = c.CriteriaName,
                    Weight = c.Weight,
                    Score = c.Score,
                    Comment = c.Comment
                }).ToList()
            };
        }
    }
}

