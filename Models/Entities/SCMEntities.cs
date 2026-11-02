using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    // =========================================================================
    // 1. NHÀ CUNG CẤP & ĐƠN ĐẶT HÀNG NHẬP (PO - PURCHASE ORDER)
    // =========================================================================

    [Table("Suppliers")]
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TaxCode { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDebt { get; set; } = 0; // Công nợ hiện tại phải trả NCC

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public virtual ICollection<DebtRecord> DebtRecords { get; set; } = new List<DebtRecord>();
    }

    [Table("PurchaseOrders")]
    public class PurchaseOrder
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string POCode { get; set; } = string.Empty; // Mã đơn nhập: PO261002001

        public int SupplierId { get; set; }

        public int BranchId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public DateTime? ExpectedDeliveryDate { get; set; }

        public DateTime? ReceivedDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DebtAmount { get; set; } = 0;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "PendingApproval"; // PendingApproval, Approved, Received, Cancelled

        public int CreatedBy { get; set; }

        public int? ApprovedBy { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; } = null!;

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;

        [ForeignKey("ApprovedBy")]
        public virtual User? Approver { get; set; }

        public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
    }

    [Table("PurchaseOrderDetails")]
    public class PurchaseOrderDetail
    {
        [Key]
        public long Id { get; set; }

        public long PurchaseOrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; } = 0;

        [MaxLength(250)]
        public string? Note { get; set; }

        [ForeignKey("PurchaseOrderId")]
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }

    // =========================================================================
    // 2. QUẢN LÝ CÔNG NỢ DOANH NGHIỆP (DEBT MANAGEMENT & AGING)
    // =========================================================================

    [Table("DebtRecords")]
    public class DebtRecord
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(20)]
        public string DebtType { get; set; } = "RECEIVABLE"; // RECEIVABLE (Phải thu KH), PAYABLE (Phải trả NCC)

        [Required, MaxLength(20)]
        public string PartnerType { get; set; } = "CUSTOMER"; // CUSTOMER, SUPPLIER

        [Required, MaxLength(200)]
        public string PartnerName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PartnerPhone { get; set; }

        public int? SupplierId { get; set; }

        public long? OrderId { get; set; }

        public long? PurchaseOrderId { get; set; }

        [Required, MaxLength(50)]
        public string DocumentCode { get; set; } = string.Empty; // Mã hóa đơn bán hoặc Mã PO

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; } = 0;

        public DateTime DueDate { get; set; } // Ngày đáo hạn công nợ

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Unpaid"; // Unpaid, PartiallyPaid, Paid, Overdue

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("PurchaseOrderId")]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        public virtual ICollection<DebtPayment> DebtPayments { get; set; } = new List<DebtPayment>();
    }

    [Table("DebtPayments")]
    public class DebtPayment
    {
        [Key]
        public long Id { get; set; }

        public long DebtRecordId { get; set; }

        [Required, MaxLength(50)]
        public string PaymentCode { get; set; } = string.Empty; // PT261002001 (Thu) hoặc PC261002001 (Chi)

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0;

        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = "BANK_TRANSFER"; // CASH, BANK_TRANSFER

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Note { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey("DebtRecordId")]
        public virtual DebtRecord DebtRecord { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;
    }

    // =========================================================================
    // 3. KIỂM KÊ KHO & CÂN BẰNG KHO (STOCKTAKING & RECONCILIATION)
    // =========================================================================

    [Table("StocktakingSheets")]
    public class StocktakingSheet
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty; // KK261002001

        public int BranchId { get; set; }

        public DateTime CheckDate { get; set; } = DateTime.Now;

        public int TotalSystemQty { get; set; } = 0;

        public int TotalActualQty { get; set; } = 0;

        public int TotalDifferenceQty { get; set; } = 0; // Actual - System

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDifferenceValue { get; set; } = 0; // Giá trị lệch (VNĐ)

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Draft"; // Draft, Balanced, Cancelled

        public int CreatedBy { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;

        public virtual ICollection<StocktakingDetail> StocktakingDetails { get; set; } = new List<StocktakingDetail>();
    }

    [Table("StocktakingDetails")]
    public class StocktakingDetail
    {
        [Key]
        public long Id { get; set; }

        public long StocktakingSheetId { get; set; }

        public int ProductId { get; set; }

        public int SystemQuantity { get; set; } = 0;

        public int ActualQuantity { get; set; } = 0;

        public int DifferenceQuantity { get; set; } = 0; // Actual - System

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DifferenceValue { get; set; } = 0; // DiffQty * CostPrice

        [MaxLength(250)]
        public string? Reason { get; set; } // Hư hỏng, rơi vỡ, thất thoát, chưa nhập kịp

        [ForeignKey("StocktakingSheetId")]
        public virtual StocktakingSheet StocktakingSheet { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}
