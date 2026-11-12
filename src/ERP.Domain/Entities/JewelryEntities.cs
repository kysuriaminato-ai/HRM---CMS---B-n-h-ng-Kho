using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP_HRM_CMS.Helpers;

namespace ERP_HRM_CMS.Models.Entities
{
    [Table("JewelryMaterials")]
    public class JewelryMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Metal { get; set; } = "Gold"; // Gold, Silver, Platinum, Other

        public int? Karat { get; set; }

        [Column(TypeName = "decimal(5,4)")]
        public decimal? PurityFactor { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }

    [Table("ProductVariants")]
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required, MaxLength(100)]
        public string Sku { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Size { get; set; }

        public int? MaterialId { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal? WeightGram { get; set; }

        [Required, MaxLength(50)]
        public string PricingMode { get; set; } = "Fixed"; // Fixed, GoldFormula

        [Column(TypeName = "decimal(18,0)")]
        public decimal? FixedPrice { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal LaborFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,0)")]
        public decimal StoneFee { get; set; } = 0;

        public bool IsDefault { get; set; } = false;

        public bool IsActive { get; set; } = true;

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("MaterialId")]
        public virtual JewelryMaterial? Material { get; set; }

        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
        public virtual ICollection<StocktakingDetail> StocktakingDetails { get; set; } = new List<StocktakingDetail>();
    }

    [Table("GoldPrices")]
    public class GoldPrice
    {
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "date")]
        public DateTime EffectiveDate { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal BuyPricePerGram { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal SellPricePerGram { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

