namespace ERP_HRM_CMS.Models.ViewModels
{
    public class TaiKhoanIndexViewModel
    {
        public List<TaiKhoanIndexItem> Items { get; set; } = new();
        public int SoChoDuyet { get; set; }
    }

    public class TaiKhoanIndexItem
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public bool IsSuperAdmin { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> RoleNames { get; set; } = new();
    }

    public class RoleCheckItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Checked { get; set; }
    }

    public class EmployeeSelectOption
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public string? Email { get; set; }
        public bool HasAccount { get; set; }
    }

    public class MenuFeatureCheckItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Checked { get; set; } = true;
        public List<string> PermissionCodes { get; set; } = new();
    }

    public class TaiKhoanFormViewModel
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeFullName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string Username { get; set; } = string.Empty;
        /// <summary>Khi tạo mới: bắt buộc. Khi sửa: để trống nếu không đổi mật khẩu.</summary>
        public string? Password { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSuperAdmin { get; set; }
        public bool MustChangePassword { get; set; } = false;
        public List<RoleCheckItem> Roles { get; set; } = new();
        public List<EmployeeSelectOption> Employees { get; set; } = new();
        public List<MenuFeatureCheckItem> MenuFeatures { get; set; } = new();
    }

    public class MenuNhanVienViewModel
    {
        public int UserId { get; set; }
        public int? EmployeeId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public bool IsSuperAdmin { get; set; }
        public List<MenuFeatureCheckItem> Features { get; set; } = new();
    }
}
