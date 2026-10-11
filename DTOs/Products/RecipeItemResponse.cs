namespace SecureSistem.DTOs.Products
{
    public class RecipeItemResponse
    {
        public int SupplyProductId { get; set; }
        public string SupplyProductName { get; set; } = string.Empty;
        public string? SupplyProductSku { get; set; }
        /// <summary>
        /// Abbreviation of the supply's unit of measure (e.g. "kg").
        /// </summary>
        public string SupplyUnit { get; set; } = string.Empty;

        public int SupplyUnitDecimalPlaces { get; set; }
        public decimal Quantity { get; set; }
    }
}
