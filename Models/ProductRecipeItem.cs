namespace SecureSistem.Models
{
    /// <summary>
    /// One supply (and how much of it) needed to make a single unit of a manufactured
    /// Product (Product.IsManufactured == true). Same shape as ProductComboItem, but it's
    /// only consumed when producing (ProductionController), never when selling.
    /// </summary>
    public class ProductRecipeItem
    {
        public int Id { get; set; }

        /// <summary>
        /// The manufactured product.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// The supply (Product with Kind == "Supply").
        /// </summary>
        public int SupplyProductId { get; set; }

        /// <summary>
        /// Amount of the supply needed per 1 unit of the product.
        /// </summary>
        public decimal Quantity { get; set; }

        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Product Product { get; set; } = null!;
        public Product SupplyProduct { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }
}
