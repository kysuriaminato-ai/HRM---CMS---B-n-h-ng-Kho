using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface IAssetManagementService
    {
        Task<PaginatedList<AssetListItemDto>> GetAssetsPagedAsync(AssetFilterDto filter);
        Task<CompanyAsset?> GetAssetByIdAsync(int id);
        Task<List<AssetAssignmentDto>> GetAssetAssignmentsAsync(int assetId);
        Task<List<AssetAssignmentDto>> GetEmployeeAssetsAsync(int employeeId);
        Task<(bool Success, string? Error, int Id)> CreateAssetAsync(CompanyAsset asset, int? currentUserId);
        Task<(bool Success, string? Error)> UpdateAssetAsync(int id, CompanyAsset asset, int? currentUserId);
        Task<(bool Success, string? Error)> AssignAssetAsync(int assetId, int employeeId, string? conditionOnAssign, int? assignedByUserId, string? note);
        Task<(bool Success, string? Error)> ReturnAssetAsync(int assignmentId, string conditionOnReturn, string returnStatus, int? returnedByUserId, string? note);
        Task<bool> HasUnreturnedAssetsAsync(int employeeId);
        Task<List<AssetAssignmentDto>> GetUnreturnedAssetsAsync(int employeeId);
        Task<AssetDropdownsDto> GetDropdownsAsync();
    }

    public class AssetFilterDto
    {
        public string? Keyword { get; set; }
        public string? AssetType { get; set; }
        public string? Status { get; set; }
        public int? BranchId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AssetListItemDto
    {
        public int Id { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? Specification { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }
        public string Status { get; set; } = "Available";
        public string? BranchName { get; set; }
        public int? CurrentEmployeeId { get; set; }
        public string? CurrentEmployeeName { get; set; }
        public string? CurrentEmployeeCode { get; set; }
        public DateTime? CurrentAssignedDate { get; set; }
    }

    public class AssetAssignmentDto
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public string? ConditionOnAssign { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public string? ConditionOnReturn { get; set; }
        public string Status { get; set; } = "Active";
        public string? AssignedByName { get; set; }
        public string? ReturnedByName { get; set; }
        public string? Note { get; set; }
    }

    public class AssetDropdownsDto
    {
        public List<SelectListItem> Branches { get; set; } = new();
        public List<SelectListItem> AvailableAssets { get; set; } = new();
        public List<SelectListItem> Employees { get; set; } = new();
    }
}
