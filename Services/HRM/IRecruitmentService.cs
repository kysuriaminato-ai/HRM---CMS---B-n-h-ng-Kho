using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.Application.Common.Models;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface IRecruitmentService
    {
        Task<PagedResult<JobPostingDto>> GetJobPostingsAsync(JobPostingFilterDto filter);
        Task<JobPostingDto?> GetJobPostingByIdAsync(int id);
        Task<JobPostingDto?> GetJobPostingBySlugAsync(string slug);
        Task<Result<int>> CreateJobPostingAsync(JobPostingCreateEditDto dto, int currentUserId);
        Task<Result> UpdateJobPostingAsync(JobPostingCreateEditDto dto, int currentUserId);
        Task<Result> DeleteJobPostingAsync(int id, int currentUserId);

        Task<List<CandidateKanbanDto>> GetKanbanCandidatesAsync(int? jobPostingId, string? keyword, decimal? minScore);
        Task<CandidateKanbanDto?> GetCandidateByIdAsync(int candidateId);
        Task<Result> UpdateCandidateStatusAsync(UpdateCandidateStatusDto dto, int currentUserId);

        Task<Result<int>> ScheduleInterviewAsync(InterviewScheduleCreateDto dto, int currentUserId);
        Task<Result> EvaluateInterviewAsync(InterviewEvaluationDto dto, int currentUserId);
        Task<List<InterviewScheduleDto>> GetInterviewSchedulesAsync(int? candidateId, int? interviewerUserId);

        Task<Result<int>> ConvertToEmployeeAsync(ConvertCandidateToEmployeeDto dto, int currentUserId);
        Task<Result<int>> SubmitPublicApplicationAsync(PublicApplyCandidateDto dto, string? clientIp);

        Task<PublicCareersIndexViewModel> GetPublicCareersIndexAsync(string? keyword, int? departmentId, int? branchId);
        Task<PublicJobDetailViewModel?> GetPublicJobDetailAsync(string slug);
    }
}

