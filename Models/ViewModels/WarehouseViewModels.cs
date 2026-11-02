using System.ComponentModel.DataAnnotations;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Models.ViewModels
{
    // --- 1. DASHBOARD TỒN KHO & CẢNH BÁO ---
    public class WarehouseDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalBranches { get; set; }
        public int TotalLowStockItems { get; set; }
        public decimal TotalInventoryAssetValue { get; set; }
        public int TodayTransactionCount { get; set; }
        public List<InventoryStockDto> StockList { get; set; } = new();
        public List<Branch> Branches { get; set; } = new();
        public int SelectedBranchId { get; set; }
        public string? Keyword { get; set; }
        public bool? LowStockOnly { get; set; }
    }

    public class InventoryStockDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Cái";
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int MinStockAlert { get; set; }
        public int MaxStockLimit { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsLowStock => StockQuantity <= MinStockAlert;
        public bool IsOverStock => StockQuantity >= MaxStockLimit;
        public decimal TotalStockAsset => StockQuantity * CostPrice;
    }

    // --- 2. DTO LẬP PHIẾU NHẬP / XUẤT / ĐIỀU CHỈNH KHO ---
    public class StockTransactionCreateDto
    {
        [Required(ErrorMessage = "Vui lòng chọn chi nhánh")]
        [Display(Name = "Chi nhánh kho")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        [Display(Name = "Sản phẩm / Hàng hóa")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng xác định loại giao dịch")]
        [Display(Name = "Loại giao dịch")]
        public string TransactionType { get; set; } = "IN"; // "IN" (Nhập kho), "OUT" (Xuất kho), "ADJUST" (Kiểm kê cân bằng)

        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Range(1, 1000000, ErrorMessage = "Số lượng phải lớn hơn 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng nhập đơn giá áp dụng")]
        [Range(0, 100000000000, ErrorMessage = "Đơn giá không hợp lệ")]
        [Display(Name = "Đơn giá chứng từ")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Ghi chú chứng từ")]
        [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
        public string? Note { get; set; }
    }

    // --- 3. DTO LỊCH SỬ THẺ KHO & GIAO DỊCH ---
    public class InventoryTransactionListViewModel
    {
        public List<InventoryTransaction> Transactions { get; set; } = new();
        public List<Branch> Branches { get; set; } = new();
        public int? SelectedBranchId { get; set; }
        public string? TransactionType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Keyword { get; set; }
        public int TotalInQuantity { get; set; }
        public int TotalOutQuantity { get; set; }
        public decimal TotalInValue { get; set; }
        public decimal TotalOutValue { get; set; }
    }
}
