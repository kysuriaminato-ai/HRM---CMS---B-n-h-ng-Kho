using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [Required, MaxLength(250)]
        public string Slug { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        public int OrderIndex { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public int CategoryId { get; set; }
        [Required, MaxLength(50)]
        public string SKU { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? Barcode { get; set; }
        [Required, MaxLength(250)]
        public string Name { get; set; } = string.Empty;
        [Required, MaxLength(300)]
        public string Slug { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string Unit { get; set; } = "Cái";
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; } = 0;
        public string? Description { get; set; }
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }

    [Table("Inventories")]
    public class Inventory
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int BranchId { get; set; }
        public int StockQuantity { get; set; } = 0;
        public int MinStockAlert { get; set; } = 5;
        public int MaxStockLimit { get; set; } = 1000;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; } = null!;
    }

    [Table("InventoryTransactions")]
    public class InventoryTransaction
    {
        [Key]
        public long Id { get; set; }
        [Required, MaxLength(50)]
        public string TransactionCode { get; set; } = string.Empty;
        [Required, MaxLength(20)]
        public string TransactionType { get; set; } = "IN";
        public int BranchId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? TotalAmount { get; private set; }
        [MaxLength(500)]
        public string? Note { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; } = null!;
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;
    }

    [Table("Orders")]
    public class Order
    {
        [Key]
        public long Id { get; set; }
        [Required, MaxLength(50)]
        public string OrderCode { get; set; } = string.Empty;
        public int BranchId { get; set; }
        [Required, MaxLength(150)]
        public string CustomerName { get; set; } = string.Empty;
        [Required, MaxLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;
        [MaxLength(300)]
        public string? CustomerAddress { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; } = 0;
        [Required, MaxLength(50)]
        public string OrderStatus { get; set; } = "Pending";
        [Required, MaxLength(50)]
        public string PaymentStatus { get; set; } = "Unpaid";
        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = "CASH";
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [MaxLength(500)]
        public string? Note { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; } = null!;
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        public long Id { get; set; }
        public long OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? TotalPrice { get; private set; }
        [MaxLength(250)]
        public string? Note { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }

    [Table("CartItems")]
    public class CartItem
    {
        [Key]
        public long Id { get; set; }
        [MaxLength(100)]
        public string? SessionId { get; set; }
        public int? UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime AddedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}
