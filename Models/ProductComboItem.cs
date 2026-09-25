namespace SecureSistem.Models
{
    /// <summary>
    /// One component product (and how many of it) that makes up a combo/kit Product
    /// (Product.IsCombo == true). Selling the combo doesn't touch its own stock — it has
    /// none — it deducts each component's stock instead, scaled by this Quantity times how
    /// many combos were sold.
    /// </summary>
    public class ProductComboItem
    {
        public int Id { get; set; }
        public int ComboProductId { get; set; }
        public int ComponentProductId { get; set; }
        public decimal Quantity { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Product ComboProduct { get; set; } = null!;
        public Product ComponentProduct { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }
}
