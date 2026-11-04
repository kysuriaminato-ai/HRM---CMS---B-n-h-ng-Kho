using ERP_HRM_CMS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    // ═══════════════════════════════════════════════════════════════
    // KHÁCH HÀNG
    // ═══════════════════════════════════════════════════════════════
    [Table("Customers")]
    public class Customer : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? TaxCode { get; set; }

        /// <summary>Nhóm khách hàng: Individual, Company, VIP</summary>
        [MaxLength(50)]
        public string CustomerType { get; set; } = "Individual";

        /// <summary>Công nợ hiện tại (denormalized)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDebt { get; set; } = 0;

        /// <summary>Tổng doanh thu từ khách hàng (denormalized)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRevenue { get; set; } = 0;

        [MaxLength(500)]
        public string? Note { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<DebtRecord> DebtRecords { get; set; } = new List<DebtRecord>();
    }

    // ═══════════════════════════════════════════════════════════════
    // ĐƠN VỊ TÍNH
    // ═══════════════════════════════════════════════════════════════
    [Table("Units")]
    public class Unit : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    // ═══════════════════════════════════════════════════════════════
    // KHO HÀNG (tách riêng khỏi Branch)
    // ═══════════════════════════════════════════════════════════════
    [Table("Warehouses")]
    public class Warehouse : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>Người quản lý kho</summary>
        public int? ManagerId { get; set; }

        public int BranchId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; } = null!;
        [ForeignKey("ManagerId")]
        public virtual Employee? Manager { get; set; }
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }

    // ═══════════════════════════════════════════════════════════════
    // HÓA ĐƠN
    // ═══════════════════════════════════════════════════════════════
    [Table("Invoices")]
    public class Invoice : IAuditable
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string InvoiceCode { get; set; } = string.Empty;

        /// <summary>SALES (bán hàng), PURCHASE (mua hàng)</summary>
        [Required, MaxLength(20)]
        public string InvoiceType { get; set; } = "SALES";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        /// <summary>Thuế GTGT</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; } = 0;

        public DateTime InvoiceDate { get; set; } = VietnamTime.Today;

        [MaxLength(500)]
        public string? Note { get; set; }

        /// <summary>Draft, Issued, Cancelled</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Draft";

        // FK nullable: hóa đơn bán hoặc mua
        public long? OrderId { get; set; }
        public long? PurchaseOrderId { get; set; }
        public int? CustomerId { get; set; }
        public int? SupplierId { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        string? IAuditable.CreatedBy { get; set; }

        // Navigation
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
        [ForeignKey("PurchaseOrderId")]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }
        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // THANH TOÁN (chung cho đơn hàng, hóa đơn, công nợ)
    // ═══════════════════════════════════════════════════════════════
    [Table("Payments")]
    public class Payment : IAuditable
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string PaymentCode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0;

        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        public DateTime PaymentDate { get; set; } = VietnamTime.Today;

        /// <summary>ORDER, INVOICE, DEBT</summary>
        [Required, MaxLength(20)]
        public string ReferenceType { get; set; } = "ORDER";

        /// <summary>Id của Order, Invoice hoặc DebtRecord</summary>
        public long ReferenceId { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        string? IAuditable.CreatedBy { get; set; }

        // Navigation
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}
