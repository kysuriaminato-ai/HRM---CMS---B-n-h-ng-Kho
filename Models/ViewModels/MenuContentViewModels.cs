using System.ComponentModel.DataAnnotations;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho form menu. Chỉ các trường trong danh sách được map vào entity;
    /// không nhận Id, CreatedAt hay bất kỳ trường hệ thống nào từ client (chống mass-assignment).
    /// </summary>
    public class MenuFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề menu.")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        [Required, MaxLength(50)]
        public string LinkType { get; set; } = "FunctionPage";

        public int? ReferenceId { get; set; }

        [MaxLength(500)]
        [UrlAnToan]
        public string? CustomUrl { get; set; }

        [Required, RegularExpression("^(_self|_blank)$", ErrorMessage = "Kiểu mở liên kết không hợp lệ.")]
        public string Target { get; set; } = "_self";

        [Required, MaxLength(50)]
        public string Position { get; set; } = "MainNav";

        public int OrderIndex { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? IconClass { get; set; }

        public static MenuFormViewModel TuEntity(Menu menu)
        {
            return new MenuFormViewModel
            {
                Id = menu.Id,
                Title = menu.Title,
                ParentId = menu.ParentId,
                LinkType = menu.LinkType,
                ReferenceId = menu.ReferenceId,
                CustomUrl = menu.CustomUrl,
                Target = menu.Target,
                Position = menu.Position,
                OrderIndex = menu.OrderIndex,
                IsActive = menu.IsActive,
                IconClass = menu.IconClass
            };
        }

        /// <summary>Ghi các trường được phép vào entity đã tải từ database.</summary>
        public void GhiVaoEntity(Menu menu)
        {
            menu.Title = Title.Trim();
            menu.ParentId = ParentId;
            menu.LinkType = LinkType.Trim();
            menu.ReferenceId = ReferenceId;
            menu.CustomUrl = string.IsNullOrWhiteSpace(CustomUrl) ? null : CustomUrl.Trim();
            menu.Target = Target;
            menu.Position = Position.Trim();
            menu.OrderIndex = OrderIndex;
            menu.IsActive = IsActive;
            menu.IconClass = string.IsNullOrWhiteSpace(IconClass) ? null : IconClass.Trim();
        }
    }

    /// <summary>ViewModel cho form bài viết. Không nhận Id, AuthorId, CreatedAt, UpdatedAt từ client.</summary>
    public class ContentFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập slug.")]
        [MaxLength(300)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Summary { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
        public string BodyHtml { get; set; } = string.Empty;

        [MaxLength(500)]
        [UrlAnToan]
        public string? ThumbnailUrl { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Published";

        public static ContentFormViewModel TuEntity(Content content)
        {
            return new ContentFormViewModel
            {
                Id = content.Id,
                Title = content.Title,
                Slug = content.Slug,
                Summary = content.Summary,
                BodyHtml = content.BodyHtml,
                ThumbnailUrl = content.ThumbnailUrl,
                Status = content.Status
            };
        }

        /// <summary>Ghi các trường được phép vào entity đã tải từ database.</summary>
        public void GhiVaoEntity(Content content)
        {
            content.Title = Title.Trim();
            content.Slug = Slug.Trim();
            content.Summary = string.IsNullOrWhiteSpace(Summary) ? null : Summary.Trim();
            content.BodyHtml = BodyHtml;
            content.ThumbnailUrl = string.IsNullOrWhiteSpace(ThumbnailUrl) ? null : ThumbnailUrl.Trim();
            content.Status = Status.Trim();
        }
    }
}
