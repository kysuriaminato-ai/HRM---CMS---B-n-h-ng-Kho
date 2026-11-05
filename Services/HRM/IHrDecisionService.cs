using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface IHrDecisionService
    {
        Task<PaginatedList<HrDecisionListItemDto>> GetPagedAsync(HrDecisionFilterDto filter);
        Task<HrDecision?> GetByIdAsync(int id);
        Task<HrDecisionPrintModel?> GetPrintModelAsync(int id);
        Task<(bool Success, string? Error, int Id)> CreateAsync(HrDecision decision, int? currentUserId);
        Task<(bool Success, string? Error)> ApproveDecisionAsync(int decisionId, int approvedByUserId);
        Task<(bool Success, string? Error)> CancelDecisionAsync(int decisionId, int cancelledByUserId, string? reason);
        Task<HrDecisionDropdownsDto> GetDropdownsAsync();
    }

    public class HrDecisionFilterDto
    {
        public string? Keyword { get; set; }
        public string? DecisionType { get; set; }
        public string? Status { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class HrDecisionListItemDto
    {
        public int Id { get; set; }
        public string DecisionNumber { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CurrentDepartmentName { get; set; } = string.Empty;
        public string DecisionType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public DateTime SignDate { get; set; }
        public string? OldDepartmentName { get; set; }
        public string? NewDepartmentName { get; set; }
        public string? OldPositionName { get; set; }
        public string? NewPositionName { get; set; }
        public decimal? OldSalary { get; set; }
        public decimal? NewSalary { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Issued";
        public string? SignerName { get; set; }
        public string? SignerTitle { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Reason { get; set; }
    }

    public class HrDecisionPrintModel
    {
        public int Id { get; set; }
        public string DecisionNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = "CÔNG TY CỔ PHẦN CÔNG NGHỆ & THƯƠNG MẠI VIỆT NAM";
        public string SignLocation { get; set; } = "Hà Nội";
        public DateTime SignDate { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DecisionType { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;

        public DateTime EffectiveDate { get; set; }
        public string? Reason { get; set; }

        public string? OldDepartmentName { get; set; }
        public string? NewDepartmentName { get; set; }
        public string? OldPositionName { get; set; }
        public string? NewPositionName { get; set; }
        public decimal? OldSalary { get; set; }
        public decimal? NewSalary { get; set; }
        public decimal Amount { get; set; }

        public string SignerName { get; set; } = "Nguyễn Văn Giám Đốc";
        public string SignerTitle { get; set; } = "Tổng Giám đốc";
    }

    public class HrDecisionDropdownsDto
    {
        public List<SelectListItem> Employees { get; set; } = new();
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Positions { get; set; } = new();
    }
}
