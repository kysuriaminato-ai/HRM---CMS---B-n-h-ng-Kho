using System.ComponentModel.DataAnnotations;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Models.ViewModels
{
    // --- 1. VIEWMODELS CHO SẢN PHẨM & DANH MỤC ---
    public class ProductListViewModel
    {
        public List<ProductStockDto> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string? Keyword { get; set; }
        public int? CategoryId { get; set; }
        public int SelectedBranchId { get; set; }
        public bool? LowStockOnly { get; set; }
    }

    public class ProductStockDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Cái";
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockAlert { get; set; }
        public bool IsLowStock => StockQuantity <= MinStockAlert;
    }

    public class ProductCreateEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục sản phẩm")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã SKU")]
        [Display(Name = "Mã SKU")]
        public string SKU { get; set; } = string.Empty;

        [Display(Name = "Mã vạch (Barcode)")]
        public string? Barcode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [Display(Name = "Tên hàng hóa / sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Slug đường dẫn")]
        public string? Slug { get; set; }

        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; } = "Cái";

        [Required(ErrorMessage = "Vui lòng nhập giá vốn")]
        [Display(Name = "Giá vốn (Cost Price)")]
        [Range(0, 100000000000, ErrorMessage = "Giá vốn không hợp lệ")]
        public decimal CostPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá niêm yết")]
        [Display(Name = "Giá bán niêm yết")]
        [Range(0, 100000000000, ErrorMessage = "Giá bán không hợp lệ")]
        public decimal SalePrice { get; set; }

        [Display(Name = "Mô tả sản phẩm")]
        public string? Description { get; set; }

        [Display(Name = "Đường dẫn ảnh đại diện")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Trạng thái kinh doanh")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Số lượng tồn khởi tạo (tại chi nhánh hiện tại)")]
        public int InitialStock { get; set; } = 0;

        [Display(Name = "Ngưỡng cảnh báo tồn tối thiểu")]
        public int MinStockAlert { get; set; } = 5;
    }

    // --- 2. VIEWMODELS CHO GIỎ HÀNG (DATA PRESENTATION) ---
    public class CartViewModel
    {
        public List<CartItemDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FinalTotal { get; set; }
        public int TotalItemsCount { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Unit { get; set; } = "Cái";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public int AvailableStock { get; set; }
        public bool IsOverStock => Quantity > AvailableStock;
        public string? StockWarning { get; set; }
    }

    public class AddToCartRequestDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public int? BranchId { get; set; }
    }

    public class UpdateCartQuantityDto
    {
        public int ProductId { get; set; }
        public int NewQuantity { get; set; }
        public int? BranchId { get; set; }
    }

    public class CartOperationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal SubTotal { get; set; }
        public decimal FinalTotal { get; set; }
        public decimal ItemTotal { get; set; }
        public int AdjustedQuantity { get; set; }
        public bool HasStockAlert { get; set; }
    }

    // --- 3. VIEWMODELS CHO ĐƠN HÀNG & POS ---
    public class POSCheckoutDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
        public string CustomerName { get; set; } = "Khách lẻ tại quầy";

        [Required(ErrorMessage = "Vui lòng nhập SĐT khách hàng")]
        public string CustomerPhone { get; set; } = "0900000000";

        public string? CustomerAddress { get; set; }
        public string PaymentMethod { get; set; } = "CASH"; // CASH, BANK_TRANSFER, POS_CARD
        public decimal CustomerCashGiven { get; set; } // Tiền khách đưa
        public decimal DiscountAmount { get; set; } = 0;
        public string? Note { get; set; }
        public int BranchId { get; set; }
    }

    public class OrderListViewModel
    {
        public List<Order> Orders { get; set; } = new();
        public string? Keyword { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrdersCount { get; set; }
    }

    public class SalesDashboardViewModel
    {
        public decimal TodayRevenue { get; set; }
        public int TodayOrderCount { get; set; }
        public decimal MonthRevenue { get; set; }
        public int MonthOrderCount { get; set; }
        public List<TopProductDto> TopSellingProducts { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalSalesValue { get; set; }
    }

    public class RecentOrderDto
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal FinalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
