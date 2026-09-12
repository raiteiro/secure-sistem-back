namespace SecureSistem.DTOs.Sales
{
    public class PaymentResponse
    {
        public int Id { get; set; }
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
