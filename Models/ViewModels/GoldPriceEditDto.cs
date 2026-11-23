using System.ComponentModel.DataAnnotations;
using ERP_HRM_CMS.Helpers;

namespace ERP_HRM_CMS.Models.ViewModels
{
    public class GoldPriceEditDto : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ngày áp dụng không được để trống")]
        public DateTime EffectiveDate { get; set; } = VietnamTime.Today;

        [Range(1, double.MaxValue, ErrorMessage = "Giá mua vào/gram phải lớn hơn 0")]
        public decimal BuyPricePerGram { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Giá bán ra/gram phải lớn hơn 0")]
        public decimal SellPricePerGram { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SellPricePerGram < BuyPricePerGram)
            {
                yield return new ValidationResult(
                    "Giá bán ra trên mỗi gram phải lớn hơn hoặc bằng giá mua vào.",
                    new[] { nameof(SellPricePerGram) });
            }
        }
    }
}

