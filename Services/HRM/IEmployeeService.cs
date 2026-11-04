using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services.HRM
{
    /// <summary>Dịch vụ quản lý nhân viên — Phiên 5</summary>
    public interface IEmployeeService
    {
        // === DANH SÁCH & TÌM KIẾM ===
        Task<PaginatedList<EmployeeListItemDto>> GetPagedAsync(EmployeeFilterDto filter);
        Task<EmployeeDetailDto?> GetDetailAsync(int id);
        Task<Employee?> GetByIdAsync(int id);

        // === CRUD ===
        Task<(bool Success, string? Error, int EmployeeId)> CreateAsync(EmployeeCreateEditViewModel model, int? currentUserId);
        Task<(bool Success, string? Error)> UpdateAsync(int id, EmployeeCreateEditViewModel model, int? currentUserId);
        Task<(bool Success, string? Error)> SoftDeleteAsync(int id, int? currentUserId);

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

        // === THỐNG KÊ ===
        Task<HrDashboardDto> GetDashboardAsync();

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

    // DTO chi tiết nhân viên (cho trang Details)
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
        public List<AttendanceDaily> RecentAttendance { get; set; } = new();
        public User? UserAccount { get; set; }
        public Contract? ActiveContract { get; set; }
    }

    // DTO Dashboard HR
    public class HrDashboardDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int ProbationEmployees { get; set; }
        public int OnLeaveEmployees { get; set; }
        public int ResignedEmployees { get; set; }
        public int NewHiresThisMonth { get; set; }
        public int ExpiringContracts30Days { get; set; }

        // Phân bố theo phòng ban: tên phòng ban -> số nhân viên
        public Dictionary<string, int> ByDepartment { get; set; } = new();
        // Phân bố theo trạng thái
        public Dictionary<string, int> ByStatus { get; set; } = new();
        // Phân bố theo giới tính
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }

        // Nhân viên mới nhất
        public List<EmployeeListItemDto> RecentHires { get; set; } = new();
        // Hợp đồng sắp hết hạn
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
    }
}
