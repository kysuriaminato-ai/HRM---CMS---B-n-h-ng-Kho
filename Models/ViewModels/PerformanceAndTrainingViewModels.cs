using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERP_HRM_CMS.Models.ViewModels
{
    // ═══════════════════════════════════════════════════════════════
    // DTO CHO ĐÁNH GIÁ KPI & MA TRẬN 9-BOX
    // ═══════════════════════════════════════════════════════════════
    public class KpiReviewDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public int ReviewerId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public decimal TotalScore { get; set; }
        public string? Grade { get; set; } // A+, A, B, C, D
        public string? OverallComment { get; set; }
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected
        public DateTime CreatedAt { get; set; }
        public decimal PotentialScore { get; set; } = 70; // 0 - 100
        public string NineBoxCategory { get; set; } = "Core Employee";
        public List<KpiCriteriaDto> Criteria { get; set; } = new();
    }

    public class KpiCriteriaDto
    {
        public int Id { get; set; }
        public int KpiReviewId { get; set; }

        [Required(ErrorMessage = "Tên tiêu chí không được để trống")]
        public string CriteriaName { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Trọng số từ 1% đến 100%")]
        public decimal Weight { get; set; }

        [Range(0, 100, ErrorMessage = "Điểm từ 0 đến 100")]
        public decimal Score { get; set; }

        public decimal SelfScore { get; set; }

        public string? Comment { get; set; }
        public string? EvidenceUrl { get; set; }
    }

    public class CreateKpiReviewDto
    {
        [Required(ErrorMessage = "Vui lòng chọn nhân viên")]
        public int EmployeeId { get; set; }

        public int ReviewerId { get; set; }

        [Range(1, 12, ErrorMessage = "Tháng từ 1 đến 12")]
        public int PeriodMonth { get; set; } = DateTime.Today.Month;

        [Range(2020, 2099, ErrorMessage = "Năm không hợp lệ")]
        public int PeriodYear { get; set; } = DateTime.Today.Year;

        public List<KpiCriteriaDto> Criteria { get; set; } = new();
    }

    public class SelfEvaluateKpiDto
    {
        [Required]
        public int KpiReviewId { get; set; }

        public List<CriteriaScoreItem> Scores { get; set; } = new();
        public string? EmployeeComment { get; set; }
    }

    public class ManagerEvaluateKpiDto
    {
        [Required]
        public int KpiReviewId { get; set; }

        public List<CriteriaScoreItem> Scores { get; set; } = new();
        public decimal PotentialScore { get; set; } = 70; // Thang điểm tiềm năng (0 - 100)
        public string? ManagerComment { get; set; }
    }

    public class CriteriaScoreItem
    {
        public int CriteriaId { get; set; }
        public decimal Score { get; set; }
        public string? Comment { get; set; }
        public string? EvidenceUrl { get; set; }
    }

    public class ApproveKpiDto
    {
        [Required]
        public int KpiReviewId { get; set; }

        public string? Grade { get; set; } // A+, A, B, C, D
        public string? DirectorComment { get; set; }
        public bool IsApproved { get; set; } = true;
    }

    public class NineBoxTalentMatrixDto
    {
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public int? DepartmentId { get; set; }
        public List<NineBoxItemDto> StarTalents { get; set; } = new(); // High Perf × High Pot
        public List<NineBoxItemDto> HighPerformers { get; set; } = new(); // High Perf × Med Pot
        public List<NineBoxItemDto> Specialists { get; set; } = new(); // High Perf × Low Pot
        public List<NineBoxItemDto> GrowthTalents { get; set; } = new(); // Med Perf × High Pot
        public List<NineBoxItemDto> CoreEmployees { get; set; } = new(); // Med Perf × Med Pot
        public List<NineBoxItemDto> SteadyPerformers { get; set; } = new(); // Med Perf × Low Pot
        public List<NineBoxItemDto> Enigmas { get; set; } = new(); // Low Perf × High Pot
        public List<NineBoxItemDto> Dilemmas { get; set; } = new(); // Low Perf × Med Pot
        public List<NineBoxItemDto> UnderPerformers { get; set; } = new(); // Low Perf × Low Pot
    }

    public class NineBoxItemDto
    {
        public int KpiReviewId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public decimal PerformanceScore { get; set; }
        public decimal PotentialScore { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string BoxCategory { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════
    // DTO CHO ĐÀO TẠO NỘI BỘ (LMS)
    // ═══════════════════════════════════════════════════════════════
    public class TrainingDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? TrainerName { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Budget { get; set; }
        public string Status { get; set; } = "Planned"; // Planned, InProgress, Completed, Cancelled
        public int ParticipantCount { get; set; }
        public int PassedCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TrainingParticipantDto> Participants { get; set; } = new();
    }

    public class TrainingCreateEditDto
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tiêu đề khóa đào tạo là bắt buộc")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TrainerName { get; set; }

        [MaxLength(300)]
        public string? Location { get; set; }

        public string? Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(7);
        public DateTime? EndDate { get; set; }

        public decimal Budget { get; set; } = 0;
        public string Status { get; set; } = "Planned";

        public string? TargetSkillName { get; set; }
        public string? TargetCertificateName { get; set; }
        public string? CertificateName { get => TargetCertificateName; set => TargetCertificateName = value; }
    }

    public class TrainingParticipantDto
    {
        public int TrainingId { get; set; }
        public string TrainingTitle { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? Result { get; set; } // Passed, Failed, Attended, Absent
        public decimal? Score { get; set; }
        public string? Comment { get; set; }
        public bool IsCertified { get; set; }
    }

    public class AssignTrainingParticipantsDto
    {
        [Required]
        public int TrainingId { get; set; }

        public List<int> EmployeeIds { get; set; } = new();
        public int? DepartmentId { get; set; }
    }

    public class GradeParticipantDto
    {
        [Required]
        public int TrainingId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Range(0, 100, ErrorMessage = "Điểm kiểm tra từ 0 đến 100")]
        public decimal Score { get; set; }

        public string? Result { get; set; } // Passed, Failed

        public string? Comment { get; set; }

        public bool AutoGrantSkillOrCertificate { get; set; } = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // DTO CHO XUẤT BÁO CÁO C&B & ỦY NHIỆM CHI
    // ═══════════════════════════════════════════════════════════════
    public class BankTransferItemDto
    {
        public int SlipId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty; // Sẽ được giải mã
        public decimal NetSalary { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class SocialInsuranceReportItemDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SocialInsuranceCode { get; set; } = string.Empty;
        public decimal InsuranceSalary { get; set; }
        public decimal EmployeeSocial { get; set; } // 8%
        public decimal EmployeeHealth { get; set; } // 1.5%
        public decimal EmployeeUnemployment { get; set; } // 1%
        public decimal EmployeeTotal { get; set; } // 10.5%
        public decimal EmployerSocial { get; set; } // 17.5%
        public decimal EmployerHealth { get; set; } // 3%
        public decimal EmployerUnemployment { get; set; } // 1%
        public decimal EmployerTotal { get; set; } // 21.5%
        public decimal GrandTotalInsurance { get; set; } // 32%
    }

    public class PayrollComparisonDto
    {
        public int CurrentMonth { get; set; }
        public int CurrentYear { get; set; }
        public decimal CurrentTotalNet { get; set; }
        public decimal CurrentTotalGross { get; set; }
        public int CurrentHeadcount { get; set; }

        public decimal PreviousTotalNet { get; set; }
        public decimal PreviousTotalGross { get; set; }
        public int PreviousHeadcount { get; set; }

        public decimal NetDifference => CurrentTotalNet - PreviousTotalNet;
        public decimal NetPercentChange => PreviousTotalNet > 0 ? Math.Round((NetDifference * 100) / PreviousTotalNet, 2) : 0;

        public string PreviousPeriodName => $"Tháng {(CurrentMonth == 1 ? 12 : CurrentMonth - 1):D2}/{(CurrentMonth == 1 ? CurrentYear - 1 : CurrentYear)}";
        public decimal TotalNetChange => NetDifference;
        public decimal TotalNetChangePercent => NetPercentChange;
        public int CurrentEmployeeCount => CurrentHeadcount;
        public int EmployeeCountChange => CurrentHeadcount - PreviousHeadcount;
    }
}

