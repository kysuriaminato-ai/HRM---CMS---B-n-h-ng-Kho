global using AppUser = ERP_HRM_CMS.Models.Entities.User;
using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    /// <summary>
    /// Thực thể tài khoản người dùng kế thừa <see cref="IdentityUser{TKey}"/> với khóa chính kiểu <see cref="int"/>.
    /// Giữ nguyên toàn bộ cột nghiệp vụ hiện có (EmployeeId, Username, PasswordHash, PasswordSalt, IsActive, IsSuperAdmin,
    /// MustChangePassword, SecurityStamp, LastLoginAt, CreatedAt) để không gãy khóa ngoại (UserRoles, Notifications, AuditLogs...).
    /// </summary>
    [Table("AspNetUsers")]
    public class User : IdentityUser<int>
    {
        public int? EmployeeId { get; set; }

        [Required, MaxLength(50)]
        public string Username
        {
            get => base.UserName ?? string.Empty;
            set
            {
                var normalized = value ?? string.Empty;
                base.UserName = normalized;
                base.NormalizedUserName = normalized.ToUpperInvariant();
            }
        }

        public override string? UserName
        {
            get => base.UserName ?? string.Empty;
            set
            {
                var normalized = value ?? string.Empty;
                base.UserName = normalized;
                base.NormalizedUserName = normalized.ToUpperInvariant();
            }
        }

        [Required, MaxLength(512)]
        public new string PasswordHash
        {
            get => base.PasswordHash ?? string.Empty;
            set => base.PasswordHash = value ?? string.Empty;
        }

        [Required, MaxLength(100)]
        public string PasswordSalt { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsSuperAdmin { get; set; } = false;
        /// <summary>Bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên (ví dụ sau khi cấp tài khoản nhanh hoặc reset mật khẩu).</summary>
        public bool MustChangePassword { get; set; } = false;
        [NotMapped]
        public bool MustChangePasswordOnFirstLogin
        {
            get => MustChangePassword;
            set => MustChangePassword = value;
        }
        /// <summary>Đổi giá trị khi thay đổi IsActive, vai trò, IsSuperAdmin hoặc mật khẩu; cookie bị thu hồi nếu lệch.</summary>
        public new Guid SecurityStamp
        {
            get => Guid.TryParse(base.SecurityStamp, out var g) ? g : Guid.Empty;
            set => base.SecurityStamp = value.ToString();
        }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        public User()
        {
            base.SecurityStamp = Guid.NewGuid().ToString();
            base.ConcurrencyStamp = Guid.NewGuid().ToString();
            base.LockoutEnabled = true;
        }

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
