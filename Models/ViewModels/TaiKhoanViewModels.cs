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
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
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

    public class TaiKhoanFormViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        /// <summary>Khi tạo mới: bắt buộc. Khi sửa: để trống nếu không đổi mật khẩu.</summary>
        public string? Password { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSuperAdmin { get; set; }
        public List<RoleCheckItem> Roles { get; set; } = new();
    }
}
