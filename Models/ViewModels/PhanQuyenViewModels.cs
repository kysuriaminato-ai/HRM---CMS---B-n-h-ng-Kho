namespace ERP_HRM_CMS.Models.ViewModels
{
    public class MaTranPhanQuyenViewModel
    {
        public int SelectedRoleId { get; set; }
        public List<RoleOptionDto> Roles { get; set; } = new();
        public List<ModulePermissionGroupDto> Modules { get; set; } = new();
        public List<int> ActivePermissionIds { get; set; } = new();
    }

    public class RoleOptionDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ModulePermissionGroupDto
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string? ModuleIcon { get; set; }
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    public class PermissionItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CapNhatQuyenRoleDto
    {
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = new();
    }

    public class PhanQuyenCaNhanViewModel
    {
        public int SelectedUserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<UserOptionDto> Users { get; set; } = new();
        public List<ModulePermissionGroupDto> Modules { get; set; } = new();
        public List<int> RoleInheritedPermissionIds { get; set; } = new();
        public Dictionary<int, bool> UserAclOverrides { get; set; } = new(); // PermissionId -> IsGranted
    }

    public class UserOptionDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class CapNhatQuyenCaNhanDto
    {
        public int UserId { get; set; }
        public List<UserPermissionItemDto> Overrides { get; set; } = new();
    }

    public class UserPermissionItemDto
    {
        public int PermissionId { get; set; }
        public bool? State { get; set; } // null = Mặc định theo Role, true = Cấp riêng, false = Chặn riêng
    }
}
