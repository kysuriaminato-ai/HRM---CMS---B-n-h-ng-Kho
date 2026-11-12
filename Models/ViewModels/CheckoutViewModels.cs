using System.ComponentModel.DataAnnotations;

namespace ERP_HRM_CMS.Models.ViewModels
{
    /// <summary>
    /// Dữ liệu hiển thị trang thanh toán dành cho khách hàng (khu vực Shop).
    /// Khác với màn hình POS của nhân viên: không có giảm giá thủ công,
    /// không chọn chi nhánh xuất hàng, và không yêu cầu quyền nội bộ.
    /// </summary>
    public class CustomerCheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new();

        public CustomerCheckoutDto Form { get; set; } = new();

        /// <summary>Hạng thẻ hiện tại nếu khách đã có hồ sơ, để hiển thị ưu đãi.</summary>
        public string? LoyaltyTierCode { get; set; }

        public string? LoyaltyTierName { get; set; }

        /// <summary>Phần trăm chiết khấu áp dụng theo hạng thẻ.</summary>
        public decimal LoyaltyDiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal FinalAmount { get; set; }
    }

    /// <summary>Thông tin khách nhập khi đặt hàng.</summary>
    public class CustomerCheckoutDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên người nhận.")]
        [StringLength(150, ErrorMessage = "Họ và tên không vượt quá 150 ký tự.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại để chúng tôi liên hệ giao hàng.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.")]
        public string CustomerPhone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(150)]
        public string? CustomerEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
        [StringLength(400, ErrorMessage = "Địa chỉ không vượt quá 400 ký tự.")]
        public string CustomerAddress { get; set; } = string.Empty;

        /// <summary>COD, BANK hoặc SHOWROOM.</summary>
        public string PaymentMethod { get; set; } = "COD";

        [StringLength(500, ErrorMessage = "Ghi chú không vượt quá 500 ký tự.")]
        public string? Note { get; set; }
    }

    /// <summary>Dữ liệu trang xác nhận sau khi đặt hàng thành công.</summary>
    public class CheckoutCompletedViewModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string PaymentMethodLabel { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CheckoutCompletedLine> Lines { get; set; } = new();

        /// <summary>Hạng thẻ sau khi cộng doanh số của đơn này.</summary>
        public string? LoyaltyTierName { get; set; }
        public bool LoyaltyTierUpgraded { get; set; }
    }

    public class CheckoutCompletedLine
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
