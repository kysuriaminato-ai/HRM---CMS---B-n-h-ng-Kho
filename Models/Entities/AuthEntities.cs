using ERP_HRM_CMS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string PasswordSalt { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsSuperAdmin { get; set; } = false;
        /// <summary>Đổi giá trị khi thay đổi IsActive, vai trò, IsSuperAdmin hoặc mật khẩu; cookie bị thu hồi nếu lệch.</summary>
        public Guid SecurityStamp { get; set; } = Guid.NewGuid();
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }

    [Table("Roles")]
    public class Role
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(300)]
        public string? Description { get; set; }
        public bool IsSystem { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    [Table("Modules")]
    public class Module
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? Icon { get; set; }
        public int OrderIndex { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }

    [Table("Permissions")]
    public class Permission
    {
        [Key]
        public int Id { get; set; }
        public int ModuleId { get; set; }
        [Required, MaxLength(100)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(300)]
        public string? Description { get; set; }
        public int OrderIndex { get; set; } = 0;

        [ForeignKey("ModuleId")]
        public virtual Module Module { get; set; } = null!;
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }

    [Table("UserRoles")]
    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
    }

    [Table("RolePermissions")]
    public class RolePermission
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; } = null!;
    }

    [Table("UserPermissions")]
    public class UserPermission
    {
        public int UserId { get; set; }
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; } = true;
        public DateTime AssignedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; } = null!;
    }
}


