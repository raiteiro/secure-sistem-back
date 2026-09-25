namespace SecureSistem.DTOs.Products
{
    public class ComboItemResponse
    {
        public int ComponentProductId { get; set; }
        public string ComponentProductName { get; set; } = string.Empty;
        public string? ComponentProductSku { get; set; }
        public decimal Quantity { get; set; }
    }
}
