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
    public class OnboardingService : IOnboardingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OnboardingService> _logger;

        public OnboardingService(AppDbContext context, ILogger<OnboardingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<OnboardingChecklistDto>> GetChecklistsAsync(string? type, string? status, int? employeeId)
        {
            var query = _context.OnboardingChecklists
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Department)
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Position)
                .Include(c => c.AssignedHr)
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.AssignedToUser)
                .Where(c => !c.IsDeleted)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(c => c.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            if (employeeId.HasValue)
            {
                query = query.Where(c => c.EmployeeId == employeeId.Value);
            }

            var list = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return list.Select(MapToChecklistDto).ToList();
        }

        public async Task<OnboardingChecklistDto?> GetChecklistByIdAsync(int id)
        {
            var item = await _context.OnboardingChecklists
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Department)
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Position)
                .Include(c => c.AssignedHr)
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.AssignedToUser)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (item == null) return null;
            return MapToChecklistDto(item);
        }

        public async Task<Result<int>> CreateChecklistAsync(CreateChecklistDto dto, int currentUserId)
        {
            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
            {
                return Result<int>.Failure("Không tìm thấy nhân viên được chỉ định.");
            }

            var checklist = new OnboardingChecklist
            {
                EmployeeId = dto.EmployeeId,
                Type = string.IsNullOrWhiteSpace(dto.Type) ? "Onboarding" : dto.Type,
                Title = dto.Title,
                StartDate = dto.StartDate,
                TargetCompletionDate = dto.TargetCompletionDate ?? dto.StartDate.AddDays(14),
                Status = "InProgress",
                AssignedHrId = dto.AssignedHrId,
                Note = dto.Note,
                CreatedAt = VietnamTime.Now,
                CreatedBy = currentUserId.ToString()
            };

            _context.OnboardingChecklists.Add(checklist);
            await _context.SaveChangesAsync();

            if (checklist.Type == "Onboarding")
            {
                await SeedDefaultOnboardingTasksAsync(checklist.Id, dto.AssignedHrId, null, null);
            }
            else
            {
                await SeedDefaultOffboardingTasksAsync(checklist.Id, dto.AssignedHrId, null);
            }

            return Result<int>.Success(checklist.Id);
        }

        public async Task<Result> UpdateTaskProgressAsync(int taskId, bool isCompleted, string? note, int currentUserId)
        {
            var task = await _context.OnboardingTasks
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Tasks)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);

            if (task == null)
            {
                return Result.Failure("Không tìm thấy công việc hội nhập.");
            }

            task.IsCompleted = isCompleted;
            task.Status = isCompleted ? "Completed" : "Pending";
            task.CompletedDate = isCompleted ? VietnamTime.Now : null;
            if (!string.IsNullOrWhiteSpace(note))
            {
                task.Note = note;
            }
            task.UpdatedAt = VietnamTime.Now;
            task.UpdatedBy = currentUserId.ToString();

            // Kiểm tra toàn bộ task trong Checklist
            var allTasks = task.Checklist.Tasks.Where(t => !t.IsDeleted).ToList();
            bool allDone = allTasks.All(t => t.Id == taskId ? isCompleted : t.IsCompleted);

            if (allDone)
            {
                task.Checklist.Status = "Completed";
                task.Checklist.CompletedDate = VietnamTime.Now;
            }
            else
            {
                task.Checklist.Status = "InProgress";
                task.Checklist.CompletedDate = null;
            }
            task.Checklist.UpdatedAt = VietnamTime.Now;
            task.Checklist.UpdatedBy = currentUserId.ToString();

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task SeedDefaultOnboardingTasksAsync(int checklistId, int? hrUserId, int? itUserId, int? deptHeadUserId)
        {
            var defaultTasks = new List<OnboardingTask>
            {
                new OnboardingTask
                {
                    ChecklistId = checklistId,
                    TaskName = "Cấp tài khoản email và quyền truy cập hệ thống",
                    DepartmentRole = "IT",
                    AssignedToUserId = itUserId,
                    OrderIndex = 1,
                    Status = "Pending",
                    CreatedAt = VietnamTime.Now
                },
                new OnboardingTask
                {
                    ChecklistId = checklistId,
                    TaskName = "Bàn giao máy tính xách tay và thiết bị làm việc",
                    DepartmentRole = "IT",
                    AssignedToUserId = itUserId,
                    OrderIndex = 2,
                    Status = "Pending",
                    CreatedAt = VietnamTime.Now
                },
                new OnboardingTask
                {
                    ChecklistId = checklistId,
                    TaskName = "Đào tạo nội quy doanh nghiệp và văn hóa công ty",
                    DepartmentRole = "HR",
                    AssignedToUserId = hrUserId,
                    OrderIndex = 3,
                    Status = "Pending",
                    CreatedAt = VietnamTime.Now
                },
                new OnboardingTask
                {
                    ChecklistId = checklistId,
                    TaskName = "Giao việc tuần đầu và chỉ định Mentor hướng dẫn",
                    DepartmentRole = "DepartmentHead",
                    AssignedToUserId = deptHeadUserId,
                    OrderIndex = 4,
                    Status = "Pending",
                    CreatedAt = VietnamTime.Now
                }
            };

            _context.OnboardingTasks.AddRange(defaultTasks);
            await _context.SaveChangesAsync();
        }

        public async Task SeedDefaultOffboardingTasksAsync(int checklistId, int? hrUserId, int? itUserId)
        {
            var defaultTasks = new List<OnboardingTask>
            {
                new OnboardingTask
                {
                    ChecklistId = checklistId,
                    TaskName = "Thu hồi máy tính, thiết bị và thẻ ra vào",
                    DepartmentRole = "IT",
                    AssignedToUserId = itUserId,
                    OrderIndex = 1,
                    Status = "Pending",
                    CreatedAt = VietnamTime.Now
                },
                new OnboardingTask
                {
                    ChecklistId = checklistId,
                    TaskName = "Hủy quyền truy cập hệ thống, email và VPN",
                    DepartmentRole = "IT",
                    AssignedToUserId = itUserId,
                    OrderIndex = 2,
                    Status = "Pending",
                    CreatedAt = VietnamTime.Now
                },
                new OnboardingTask
                {
                    ChecklistId = checklistId,
                    TaskName = "Bàn giao tài liệu công việc và dự án đang thực hiện",
                    DepartmentRole = "DepartmentHead",
                    OrderIndex = 3,
                    Status = "Pending",
                    CreatedAt = VietnamTime.Now
                },
                new OnboardingTask
                {
                    ChecklistId = checklistId,
                    TaskName = "Quyết toán lương, phép tồn và hoàn tất thủ tục thôi việc",
                    DepartmentRole = "HR",
                    AssignedToUserId = hrUserId,
                    OrderIndex = 4,
                    Status = "Pending",
                    CreatedAt = VietnamTime.Now
                }
            };

            _context.OnboardingTasks.AddRange(defaultTasks);
            await _context.SaveChangesAsync();
        }

        private static OnboardingChecklistDto MapToChecklistDto(OnboardingChecklist c)
        {
            var tasks = c.Tasks.Where(t => !t.IsDeleted).OrderBy(t => t.OrderIndex).ToList();
            int total = tasks.Count;
            int completed = tasks.Count(t => t.IsCompleted);

            return new OnboardingChecklistDto
            {
                Id = c.Id,
                EmployeeId = c.EmployeeId,
                EmployeeName = c.Employee?.FullName ?? string.Empty,
                EmployeeCode = c.Employee?.EmployeeCode ?? string.Empty,
                DepartmentName = c.Employee?.Department?.Name ?? string.Empty,
                PositionName = c.Employee?.Position?.Name ?? string.Empty,
                Type = c.Type,
                Title = c.Title,
                StartDate = c.StartDate,
                TargetCompletionDate = c.TargetCompletionDate,
                CompletedDate = c.CompletedDate,
                Status = c.Status,
                AssignedHrId = c.AssignedHrId,
                AssignedHrName = c.AssignedHr?.Employee?.FullName ?? c.AssignedHr?.Username,
                Note = c.Note,
                TotalTasks = total,
                CompletedTasks = completed,
                Tasks = tasks.Select(t => new OnboardingTaskDto
                {
                    Id = t.Id,
                    ChecklistId = t.ChecklistId,
                    TaskName = t.TaskName,
                    DepartmentRole = t.DepartmentRole,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToUserName = t.AssignedToUser?.Employee?.FullName ?? t.AssignedToUser?.Username,
                    DueDate = t.DueDate,
                    CompletedDate = t.CompletedDate,
                    IsCompleted = t.IsCompleted,
                    Status = t.Status,
                    Note = t.Note,
                    OrderIndex = t.OrderIndex
                }).ToList()
            };
        }
    }
}
