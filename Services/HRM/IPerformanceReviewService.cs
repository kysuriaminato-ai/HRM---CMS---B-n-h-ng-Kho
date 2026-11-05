using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.Application.Common.Models;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface IPerformanceReviewService
    {
        Task<List<KpiReviewDto>> GetKpiReviewsAsync(int? month, int? year, int? departmentId, string? grade, string? status);
        Task<KpiReviewDto?> GetKpiReviewByIdAsync(int id);
        Task<List<KpiReviewDto>> GetEmployeeKpiHistoryAsync(int employeeId);
        Task<Result<int>> CreateKpiReviewAsync(CreateKpiReviewDto dto, int currentUserId);
        Task<Result> SelfEvaluateAsync(SelfEvaluateKpiDto dto, int employeeId);
        Task<Result> ManagerEvaluateAsync(ManagerEvaluateKpiDto dto, int reviewerUserId);
        Task<Result> ApproveKpiAsync(ApproveKpiDto dto, int directorUserId);
        Task<NineBoxTalentMatrixDto> GetNineBoxMatrixAsync(int month, int year, int? departmentId);
        (bool IsValid, string? ErrorMessage) ValidateCriteriaWeights(IEnumerable<KpiCriteriaDto> criteria);
    }
}

