using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface IContractService
    {
        Task<PaginatedList<ContractListItemDto>> GetPagedAsync(ContractFilterDto filter);
        Task<List<ContractListItemDto>> GetExpiringContractsAsync(int days = 30);
        Task<Contract?> GetByIdAsync(int id);
        Task<ContractPrintModel?> GetPrintModelAsync(int id);
        Task<(bool Success, string? Error, int Id)> CreateAsync(Contract contract, int? currentUserId);
        Task<(bool Success, string? Error)> TerminateAsync(int id, int? currentUserId);
        Task<List<SelectListItem>> GetEmployeeSelectListAsync(int? selectedId = null);
    }

    public class ContractFilterDto
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public int? DepartmentId { get; set; }
        public int? ExpiringWithinDays { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ContractListItemDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal InsuranceSalary { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime SignDate { get; set; }
        public int? DaysRemaining => EndDate.HasValue ? (int)(EndDate.Value.Date - DateTime.Today).TotalDays : null;
    }

    public class ContractPrintModel
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = "CÔNG TY CỔ PHẦN CÔNG NGHỆ & THƯƠNG MẠI VIỆT NAM";
        public string CompanyAddress { get; set; } = "Tầng 12, Tòa nhà Enterprise Plaza, Hà Nội";
        public string CompanyPhone { get; set; } = "024.3999.8888";
        public string RepresentativeName { get; set; } = "Nguyễn Văn Giám Đốc";
        public string RepresentativePosition { get; set; } = "Tổng Giám đốc";

        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? IdentityCard { get; set; }
        public string? PermanentAddress { get; set; }
        public string? Phone { get; set; }

        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal InsuranceSalary { get; set; }
        public DateTime SignDate { get; set; }
    }
}
