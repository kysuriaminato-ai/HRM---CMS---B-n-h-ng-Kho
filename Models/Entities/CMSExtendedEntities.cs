using ERP_HRM_CMS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    // ═══════════════════════════════════════════════════════════════
    // TAG (Thẻ gắn bài viết)
    // ═══════════════════════════════════════════════════════════════
    [Table("Tags")]
    public class Tag : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Slug { get; set; } = string.Empty;

        /// <summary>Đếm số bài dùng tag (denormalized, cập nhật khi thêm/xóa)</summary>
        public int UsageCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        public virtual ICollection<ContentTag> ContentTags { get; set; } = new List<ContentTag>();
    }

    /// <summary>Bảng trung gian Content ↔ Tag (many-to-many)</summary>
    [Table("ContentTags")]
    public class ContentTag
    {
        public int ContentId { get; set; }
        public int TagId { get; set; }

        [ForeignKey("ContentId")]
        public virtual Content Content { get; set; } = null!;
        [ForeignKey("TagId")]
        public virtual Tag Tag { get; set; } = null!;
    }

    // ═══════════════════════════════════════════════════════════════
    // MEDIA (Thư viện media)
    // ═══════════════════════════════════════════════════════════════
    [Table("Media")]
    public class MediaFile : IAuditable
    {
        [Key]
        public long Id { get; set; }

        /// <summary>Tên file trên server (uuid.ext)</summary>
        [Required, MaxLength(300)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Tên file gốc do người dùng upload</summary>
        [Required, MaxLength(300)]
        public string OriginalName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        /// <summary>Kích thước file (bytes)</summary>
        public long FileSize { get; set; }

        /// <summary>URL truy cập (relative hoặc CDN)</summary>
        [Required, MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        [MaxLength(300)]
        public string? AltText { get; set; }

        /// <summary>Thư mục phân loại (general, avatar, product, content)</summary>
        [MaxLength(100)]
        public string Folder { get; set; } = "general";

        public int? UploadedBy { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("UploadedBy")]
        public virtual User? Uploader { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // SEO META (Meta tags cho SEO)
    // ═══════════════════════════════════════════════════════════════
    [Table("SeoMeta")]
    public class SeoMeta : IAuditable
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Loại entity: Content, Category, Product, Page</summary>
        [Required, MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>Id của entity được gắn SEO</summary>
        public int EntityId { get; set; }

        [MaxLength(200)]
        public string? MetaTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        [MaxLength(500)]
        public string? MetaKeywords { get; set; }

        [MaxLength(500)]
        public string? CanonicalUrl { get; set; }

        [MaxLength(500)]
        public string? OgImage { get; set; }

        /// <summary>JSON-LD structured data</summary>
        public string? StructuredData { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }
    }
}
