using System.ComponentModel.DataAnnotations;

namespace ERP_HRM_CMS.Models.ViewModels
{
    /// <summary>Form tạo/sửa nhóm quyền (vai trò).</summary>
    public class VaiTroFormModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã nhóm quyền.")]
        [MaxLength(50)]
        [Display(Name = "Mã nhóm (ví dụ: ADMIN_KHUYENMAI)")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên nhóm quyền.")]
        [MaxLength(150)]
        [Display(Name = "Tên nhóm quyền")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public bool IsSystem { get; set; }
    }
}
