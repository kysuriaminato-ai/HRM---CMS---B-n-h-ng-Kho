using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.Application.Common.Models;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface ITrainingService
    {
        Task<List<TrainingDto>> GetTrainingsAsync(string? keyword, string? status);
        Task<TrainingDto?> GetTrainingByIdAsync(int id);
        Task<Result<int>> CreateTrainingAsync(TrainingCreateEditDto dto, int currentUserId);
        Task<Result> UpdateTrainingAsync(TrainingCreateEditDto dto, int currentUserId);
        Task<Result> DeleteTrainingAsync(int id, int currentUserId);

        Task<Result> AssignParticipantsAsync(AssignTrainingParticipantsDto dto, int currentUserId);
        Task<Result> GradeParticipantAsync(GradeParticipantDto dto, int currentUserId);
        Task<List<TrainingParticipantDto>> GetEmployeeTrainingsAsync(int employeeId);
    }
}

