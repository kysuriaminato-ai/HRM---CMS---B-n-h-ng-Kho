using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Services.HRM
{
    /// <summary>Dịch vụ quản lý phòng ban — cây đệ quy</summary>
    public interface IDepartmentService
    {
        /// <summary>Lấy tất cả phòng ban dạng cây (gộp theo chi nhánh)</summary>
        Task<List<DepartmentTreeNodeDto>> GetTreeAsync();

        /// <summary>Lấy danh sách phẳng (flat) phòng ban cho dropdown</summary>
        Task<List<DepartmentFlatDto>> GetFlatListAsync();

        Task<Department?> GetByIdAsync(int id);
        Task<(bool Success, string? Error)> CreateAsync(Department dept);
        Task<(bool Success, string? Error)> UpdateAsync(int id, Department dept);
        Task<(bool Success, string? Error)> ToggleActiveAsync(int id);
        Task<(bool Success, string? Error)> MoveAsync(int id, int newParentId, int newBranchId);
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
}
