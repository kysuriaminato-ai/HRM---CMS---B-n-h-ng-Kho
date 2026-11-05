using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ERP_HRM_CMS.Models.ViewModels
{
    // ═══════════════════════════════════════════════════════════════
    // DTO CHO TIN TUYỂN DỤNG (JOB POSTING)
    // ═══════════════════════════════════════════════════════════════
    public class JobPostingDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int Vacancies { get; set; } = 1;
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Benefits { get; set; }
        public string? SalaryRange { get; set; }
        public string? WorkLocation { get; set; }
        public string Status { get; set; } = "Draft"; // Draft, Open, Closed
        public DateTime OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CandidateCount { get; set; }

        // SEO Meta Fields
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? OgImage { get; set; }
    }

    public class JobPostingCreateEditDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề tin tuyển dụng")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(350)]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng ban")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vị trí")]
        public int PositionId { get; set; }

        public int? BranchId { get; set; }

        [Range(1, 1000, ErrorMessage = "Số lượng cần tuyển từ 1 đến 1000")]
        public int Vacancies { get; set; } = 1;

        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Benefits { get; set; }

        [MaxLength(100)]
        public string? SalaryRange { get; set; }

        [MaxLength(200)]
        public string? WorkLocation { get; set; }

        public string Status { get; set; } = "Open";

        public DateTime OpenDate { get; set; } = DateTime.Today;
        public DateTime? CloseDate { get; set; }

        // SEO Fields
        [MaxLength(200)]
        public string? MetaTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        [MaxLength(500)]
        public string? MetaKeywords { get; set; }

        [MaxLength(500)]
        public string? OgImage { get; set; }
    }

    public class JobPostingFilterDto
    {
        public string? Keyword { get; set; }
        public int? DepartmentId { get; set; }
        public int? BranchId { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // ═══════════════════════════════════════════════════════════════
    // DTO CHO KANBAN PIPELINE ỨNG VIÊN
    // ═══════════════════════════════════════════════════════════════
    public class CandidateKanbanDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ResumeUrl { get; set; }
        public string? CoverLetter { get; set; }
        public string Status { get; set; } = "Applied"; // Applied, Screening, Interview, Offered, Hired, Rejected
        public decimal? EvaluationScore { get; set; }
        public string? Note { get; set; }
        public DateTime AppliedDate { get; set; }
        public int JobPostingId { get; set; }
        public string JobPostingTitle { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int InterviewCount { get; set; }
        public DateTime? NextInterviewDate { get; set; }
    }

    public class CandidateFilterDto
    {
        public int? JobPostingId { get; set; }
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public decimal? MinScore { get; set; }
    }

    public class UpdateCandidateStatusDto
    {
        [Required]
        public int CandidateId { get; set; }

        [Required]
        public string NewStatus { get; set; } = string.Empty;

        public string? Note { get; set; }
        public decimal? EvaluationScore { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // DTO CHO LỊCH PHỎNG VẤN (INTERVIEW SCHEDULE)
    // ═══════════════════════════════════════════════════════════════
    public class InterviewScheduleDto
    {
        public int Id { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string? CandidateEmail { get; set; }
        public string? CandidatePhone { get; set; }
        public string JobPostingTitle { get; set; } = string.Empty;
        public int RoundNumber { get; set; } = 1;
        public string Title { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public int DurationMinutes { get; set; } = 45;
        public string InterviewType { get; set; } = "Offline"; // Online, Offline
        public string? LocationOrMeetingLink { get; set; }
        public int? InterviewerUserId { get; set; }
        public string? InterviewerName { get; set; }
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled, NoShow
        public decimal? Score { get; set; }
        public string? Feedback { get; set; }
        public string Result { get; set; } = "Pending"; // Passed, Failed, Pending, Consider
    }

    public class InterviewScheduleCreateDto
    {
        [Required]
        public int CandidateId { get; set; }

        public int RoundNumber { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề phỏng vấn")]
        [MaxLength(200)]
        public string Title { get; set; } = "Phỏng vấn chuyên môn";

        [Required(ErrorMessage = "Vui lòng chọn thời gian")]
        public DateTime ScheduledTime { get; set; } = DateTime.Now.AddDays(1);

        public int DurationMinutes { get; set; } = 45;

        [Required]
        public string InterviewType { get; set; } = "Offline"; // Online, Offline

        [MaxLength(500)]
        public string? LocationOrMeetingLink { get; set; }

        public int? InterviewerUserId { get; set; }
        public int? InterviewerEmployeeId { get; set; }
    }

    public class InterviewEvaluationDto
    {
        [Required]
        public int ScheduleId { get; set; }

        [Range(0, 100, ErrorMessage = "Điểm đánh giá từ 0 đến 100")]
        public decimal? Score { get; set; }

        public string? Feedback { get; set; }

        [Required]
        public string Result { get; set; } = "Passed"; // Passed, Failed, Pending, Consider
    }

    // ═══════════════════════════════════════════════════════════════
    // DTO CHO 1-CLICK CHUYỂN ỨNG VIÊN THÀNH NHÂN VIÊN (GOLDEN PATH)
    // ═══════════════════════════════════════════════════════════════
    public class ConvertCandidateToEmployeeDto
    {
        [Required]
        public int CandidateId { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phòng ban")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vị trí")]
        public int PositionId { get; set; }

        public int? BranchId { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Today;

        public decimal BaseSalary { get; set; } = 10000000;

        public int ProbationMonths { get; set; } = 2;

        public int? AssignedHrUserId { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // DTO CHO ONBOARDING / OFFBOARDING CHECKLIST
    // ═══════════════════════════════════════════════════════════════
    public class OnboardingChecklistDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string Type { get; set; } = "Onboarding"; // Onboarding, Offboarding
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Status { get; set; } = "InProgress"; // InProgress, Completed
        public int? AssignedHrId { get; set; }
        public string? AssignedHrName { get; set; }
        public string? Note { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ProgressPercentage => TotalTasks > 0 ? (int)Math.Round((double)CompletedTasks * 100 / TotalTasks) : 0;
        public List<OnboardingTaskDto> Tasks { get; set; } = new();
    }

    public class OnboardingTaskDto
    {
        public int Id { get; set; }
        public int ChecklistId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string DepartmentRole { get; set; } = "HR"; // HR, IT, DepartmentHead, Employee, Accountant
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool IsCompleted { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Note { get; set; }
        public int OrderIndex { get; set; }
    }

    public class UpdateOnboardingTaskDto
    {
        public int TaskId { get; set; }
        public bool IsCompleted { get; set; }
        public string? Note { get; set; }
    }

    public class CreateChecklistDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public string Type { get; set; } = "Onboarding"; // Onboarding, Offboarding

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime? TargetCompletionDate { get; set; }
        public int? AssignedHrId { get; set; }
        public string? Note { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // DTO CHO TRANG TUYỂN DỤNG CÔNG KHAI (CAREERS PORTAL)
    // ═══════════════════════════════════════════════════════════════
    public class PublicCareersIndexViewModel
    {
        public string? Keyword { get; set; }
        public int? DepartmentId { get; set; }
        public int? BranchId { get; set; }
        public List<DepartmentSelectDto> Departments { get; set; } = new();
        public List<BranchSelectDto> Branches { get; set; } = new();
        public List<JobPostingDto> OpenJobs { get; set; } = new();
        public int TotalOpenings { get; set; }
    }

    public class DepartmentSelectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OpenJobCount { get; set; }
    }

    public class BranchSelectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PublicJobDetailViewModel
    {
        public JobPostingDto Job { get; set; } = null!;
        public List<JobPostingDto> RelatedJobs { get; set; } = new();
        public PublicApplyCandidateDto ApplyModel { get; set; } = new();
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string CanonicalUrl { get; set; } = string.Empty;
        public string JsonLdSchema { get; set; } = string.Empty;
    }

    public class PublicApplyCandidateDto
    {
        public int JobPostingId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ email")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0[0-9]{9,10}$", ErrorMessage = "Số điện thoại Việt Nam hợp lệ gồm 10 chữ số bắt đầu bằng số 0")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? CoverLetter { get; set; }

        [Required(ErrorMessage = "Vui lòng tải lên hồ sơ CV của bạn (.pdf hoặc .docx)")]
        public IFormFile? CvFile { get; set; }

        /// <summary>Anti-spam honeypot field (phải để trống, bot tự điền sẽ bị block)</summary>
        public string? Website { get; set; }
    }
}

