namespace ERP_HRM_CMS.Models.ViewModels
{
    public class JewelryCardVm
    {
        public int Id { get; set; } = 0;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Carat { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Clarity { get; set; } = string.Empty;
        public string Cut { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0m;
        public decimal OldPrice { get; set; } = 0m;
        public string Image { get; set; } = string.Empty;
        public string GiaNumber { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
    }
}

