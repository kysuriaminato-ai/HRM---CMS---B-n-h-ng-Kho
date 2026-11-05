using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP_HRM_CMS.Services.HRM
{
    /// <summary>Dịch vụ quản lý nhân viên — Phiên 4: Hồ sơ 360°, Cảnh báo, Import/Export Excel, Clean Architecture</summary>
    public interface IEmployeeService
    {
        // === DANH SÁCH & TÌM KIẾM ===
        Task<PaginatedList<EmployeeListItemDto>> GetPagedAsync(EmployeeFilterDto filter);
        Task<EmployeeDetailDto?> GetDetailAsync(int id);
        Task<Employee?> GetByIdAsync(int id);
        Task<EmployeeStatusCountsDto> GetStatusCountsAsync();
        Task<EmployeeDropdownDto> GetDropdownDataAsync(int? selectedDeptId = null, int? selectedPosId = null);

        // === CRUD ===
        Task<(bool Success, string? Error, int EmployeeId)> CreateAsync(EmployeeCreateEditViewModel model, int? currentUserId);
        Task<(bool Success, string? Error)> UpdateAsync(int id, EmployeeCreateEditViewModel model, int? currentUserId);
        Task<(bool Success, string? Error)> SoftDeleteAsync(int id, int? currentUserId);

        // === CẤP TÀI KHOẢN AN TOÀN ===
        Task<(bool Success, string? Error, string? Username, string? TempPassword)> ProvisionUserAccountAsync(int employeeId, int? currentUserId, string? clientIp);

        // === HỒ SƠ MỞ RỘNG ===
        Task<List<EmployeeRelative>> GetRelativesAsync(int employeeId);
        Task AddRelativeAsync(EmployeeRelative relative);
        Task RemoveRelativeAsync(int id);

        Task<List<EmployeeSkill>> GetSkillsAsync(int employeeId);
        Task AddSkillAsync(EmployeeSkill skill);
        Task RemoveSkillAsync(int id);

        Task<List<EmployeeCertificate>> GetCertificatesAsync(int employeeId);
        Task AddCertificateAsync(EmployeeCertificate cert);
        Task RemoveCertificateAsync(int id);

        Task<List<EmployeeDocument>> GetDocumentsAsync(int employeeId);
        Task AddDocumentAsync(EmployeeDocument doc);
        Task RemoveDocumentAsync(int id);

        // === LỊCH SỬ THAY ĐỔI ===
        Task<List<EmployeeChangeHistory>> GetChangeHistoryAsync(int employeeId);

        // === THỐNG KÊ & CẢNH BÁO THÔNG MINH ===
        Task<HrDashboardDto> GetDashboardAsync();
        Task<HrSmartAlertsDto> GetSmartAlertsAsync();

        // === IMPORT / EXPORT EXCEL ===
        Task<byte[]> ExportToExcelAsync(EmployeeFilterDto filter);
        byte[] GenerateImportTemplate();
        Task<EmployeeImportResultDto> ImportFromExcelAsync(Stream fileStream, int? currentUserId);

        // === IN ẤN CHUẨN HÀNH CHÍNH VIỆT NAM (SƠ YẾU LÝ LỊCH) ===
        Task<EmployeePrintModel?> GetPrintableResumeAsync(int employeeId);

        // === UPLOAD AVATAR ===
        Task<string?> SaveAvatarAsync(int employeeId, Stream fileStream, string fileName);
    }

    // DTO cho danh sách nhân viên (projection, tránh N+1)
    public class EmployeeListItemDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime HireDate { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }

    // Bộ lọc danh sách nhân viên
    public class EmployeeFilterDto
    {
        public string? Keyword { get; set; }
        public int? DepartmentId { get; set; }
        public int? PositionId { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; } = "HireDate";
        public bool SortDesc { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // DTO chi tiết nhân viên (Hồ sơ 360°)
    public class EmployeeDetailDto
    {
        public Employee Employee { get; set; } = null!;
        public EmployeeDetail? Detail { get; set; }
        public List<Contract> Contracts { get; set; } = new();
        public List<EmployeeRelative> Relatives { get; set; } = new();
        public List<EmployeeSkill> Skills { get; set; } = new();
        public List<EmployeeCertificate> Certificates { get; set; } = new();
        public List<EmployeeDocument> Documents { get; set; } = new();
        public List<EmployeeChangeHistory> ChangeHistories { get; set; } = new();
        public List<HrDecision> Decisions { get; set; } = new();
        public List<EmployeeAssetAssignment> Assets { get; set; } = new();
        public List<AttendanceDaily> RecentAttendance { get; set; } = new();
        public User? UserAccount { get; set; }
        public Contract? ActiveContract { get; set; }
    }

    public class EmployeeStatusCountsDto
    {
        public int TotalActive { get; set; }
        public int TotalProbation { get; set; }
        public int TotalOnLeave { get; set; }
        public int TotalResigned { get; set; }
    }

    public class EmployeeDropdownDto
    {
        public SelectList Departments { get; set; } = null!;
        public SelectList Positions { get; set; } = null!;
        public SelectList Employees { get; set; } = null!;
    }

    public class HrDashboardDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int ProbationEmployees { get; set; }
        public int OnLeaveEmployees { get; set; }
        public int ResignedEmployees { get; set; }
        public int NewHiresThisMonth { get; set; }
        public int ExpiringContracts30Days { get; set; }

        public Dictionary<string, int> ByDepartment { get; set; } = new();
        public Dictionary<string, int> ByStatus { get; set; } = new();
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }

        public List<EmployeeListItemDto> RecentHires { get; set; } = new();
        public List<ExpiringContractDto> ExpiringContractList { get; set; } = new();
    }

    public class ExpiringContractDto
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime? EndDate { get; set; }
        public int DaysLeft { get; set; }
        public string UrgencyLevel { get; set; } = "Normal"; // Critical (<=7d), Warning (<=15d), Normal (<=30d)
    }

    public class ProbationEndingDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public DateTime ExpectedProbationEndDate { get; set; }
        public int DaysLeft { get; set; }
    }

    public class ExpiringCertificateDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CertificateName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysLeft { get; set; }
    }

    public class UpcomingBirthdayDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Day { get; set; }
        public int Age { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class HrSmartAlertsDto
    {
        public List<ExpiringContractDto> ExpiringContracts { get; set; } = new();
        public List<ProbationEndingDto> EndingProbations { get; set; } = new();
        public List<ExpiringCertificateDto> ExpiringCertificates { get; set; } = new();
        public List<UpcomingBirthdayDto> BirthdaysInMonth { get; set; } = new();
    }

    public class EmployeeImportResultDto
    {
        public bool Success { get; set; }
        public int TotalRows { get; set; }
        public int ImportedCount { get; set; }
        public int ErrorCount => Errors.Count;
        public List<RowValidationErrorDto> Errors { get; set; } = new();
    }

    public class RowValidationErrorDto
    {
        public int RowIndex { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Field { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class EmployeePrintModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? IdentityCard { get; set; }
        public string? AvatarUrl { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public string Status { get; set; } = "Active";

        public EmployeeDetail? Detail { get; set; }
        public List<Contract> Contracts { get; set; } = new();
        public List<EmployeeRelative> Relatives { get; set; } = new();
        public List<EmployeeSkill> Skills { get; set; } = new();
        public List<EmployeeCertificate> Certificates { get; set; } = new();
        public List<EmployeeChangeHistory> ChangeHistories { get; set; } = new();
        public List<HrDecision> Decisions { get; set; } = new();
        public List<EmployeeAssetAssignment> Assets { get; set; } = new();
    }
}
