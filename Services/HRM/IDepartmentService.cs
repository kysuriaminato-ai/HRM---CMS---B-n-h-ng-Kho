using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Services.HRM
{
    /// <summary>Dịch vụ quản lý phòng ban — cây đệ quy & cơ cấu tổ chức</summary>
    public interface IDepartmentService
    {
        /// <summary>Lấy tất cả phòng ban dạng cây (gộp theo chi nhánh)</summary>
        Task<List<DepartmentTreeNodeDto>> GetTreeAsync();

        /// <summary>Lấy danh sách phẳng (flat) phòng ban cho dropdown</summary>
        Task<List<DepartmentFlatDto>> GetFlatListAsync();

        Task<Department?> GetByIdAsync(int id);
        Task<DepartmentDrawerDetailDto?> GetDrawerDetailAsync(int departmentId);
        Task<(bool Success, string? Error)> CreateAsync(Department dept);
        Task<(bool Success, string? Error)> UpdateAsync(int id, Department dept);
        Task<(bool Success, string? Error)> ToggleActiveAsync(int id);
        Task<(bool Success, string? Error)> MoveAsync(int id, int newParentId, int newBranchId);
        Task<(bool Success, string? Error)> DeleteAsync(int id);

        Task<List<Branch>> GetBranchesAsync();
        Task<List<Position>> GetPositionsAsync();
        Task<(bool Success, string? Error)> CreatePositionAsync(Position pos);
        Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetDepartmentSelectListAsync(int? selectedId = null);
    }

    /// <summary>Node cây phòng ban</summary>
    public class DepartmentTreeNodeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int EmployeeCount { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerTitle { get; set; }
        public string? ManagerAvatar { get; set; }
        public List<DepartmentTreeNodeDto> Children { get; set; } = new();
    }

    /// <summary>Phòng ban dạng phẳng (cho dropdown, có thụt lề)</summary>
    public class DepartmentFlatDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty; // Có thụt lề theo cấp
        public int Level { get; set; }
        public bool IsActive { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }

    /// <summary>Thông tin chi tiết hiển thị trong Drawer bên phải khi click node phòng ban</summary>
    public class DepartmentDrawerDetailDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string? ParentDepartmentName { get; set; }
        public bool IsActive { get; set; }
        public int StaffingQuota { get; set; } = 20; // Định biên nhân sự mặc định hoặc cấu hình
        public int CurrentHeadcount { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerTitle { get; set; }
        public string? ManagerAvatar { get; set; }
        public string? ManagerEmail { get; set; }
        public string? ManagerPhone { get; set; }
        public List<DepartmentMemberDto> Members { get; set; } = new();
    }

    public class DepartmentMemberDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime HireDate { get; set; }
    }
}
