using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.Application.Common.Models;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface IOnboardingService
    {
        Task<List<OnboardingChecklistDto>> GetChecklistsAsync(string? type, string? status, int? employeeId);
        Task<OnboardingChecklistDto?> GetChecklistByIdAsync(int id);
        Task<Result<int>> CreateChecklistAsync(CreateChecklistDto dto, int currentUserId);
        Task<Result> UpdateTaskProgressAsync(int taskId, bool isCompleted, string? note, int currentUserId);
        Task SeedDefaultOnboardingTasksAsync(int checklistId, int? hrUserId, int? itUserId, int? deptHeadUserId);
        Task SeedDefaultOffboardingTasksAsync(int checklistId, int? hrUserId, int? itUserId);
    }
}

