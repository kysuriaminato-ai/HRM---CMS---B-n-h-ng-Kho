using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    [Table("SiteLayouts")]
    public class SiteLayout
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string LayoutName { get; set; } = "Default";
        [Required, MaxLength(50)]
        public string BlockKey { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string BlockTitle { get; set; } = string.Empty;
        public int OrderIndex { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        [MaxLength(100)]
        public string? CustomCssClass { get; set; }
        [MaxLength(50)]
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    [Table("Menus")]
    public class Menu
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        [Required, MaxLength(50)]
        public string LinkType { get; set; } = "FunctionPage";
        public int? ReferenceId { get; set; }
        [MaxLength(500)]
        public string? CustomUrl { get; set; }
        [Required, MaxLength(20)]
        public string Target { get; set; } = "_self";
        [Required, MaxLength(50)]
        public string Position { get; set; } = "MainNav";
        public int OrderIndex { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        [MaxLength(100)]
        public string? IconClass { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ParentId")]
        public virtual Menu? Parent { get; set; }
        public virtual ICollection<Menu> SubMenus { get; set; } = new List<Menu>();
    }

    [Table("Contents")]
    public class Content
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;
        [Required, MaxLength(300)]
        public string Slug { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Summary { get; set; }
        [Required]
        public string BodyHtml { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Published";
        public int ViewCount { get; set; } = 0;
        public int? AuthorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("AuthorId")]
        public virtual User? Author { get; set; }
    }

    [Table("Banners")]
    public class Banner
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(300)]
        public string? SubTitle { get; set; }
        [Required, MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? LinkUrl { get; set; }
        [Required, MaxLength(50)]
        public string Position { get; set; } = "HomeSlider";
        public int OrderIndex { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("SiteSettings")]
    public class SiteSetting
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;
        public string? SettingValue { get; set; }
        [Required, MaxLength(50)]
        public string SettingGroup { get; set; } = "General";
        [MaxLength(250)]
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
