using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Inventory
{
    public class CreateInventoryMovementRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        /// <summary>
        /// "In", "Out", "Adjustment", "Purchase", "Sale" or "Return". "Purchase" has the same
        /// effect as "In", "Sale" has the same effect as "Out", and "Return" has the same
        /// effect as "In" — they're just a more specific label for the movement's cause.
        /// </summary>
        [Required, RegularExpression("^(In|Out|Adjustment|Purchase|Sale|Return)$",
            ErrorMessage = "Type debe ser 'In', 'Out', 'Adjustment', 'Purchase', 'Sale' o 'Return'.")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Signed change in stock. "In"/"Purchase"/"Return" require a positive value,
        /// "Out"/"Sale" require a negative value, "Adjustment" allows either (a positive or
        /// negative correction).
        /// </summary>
        [Required]
        public decimal Quantity { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Who delivered this stock — only meaningful for Type == "Purchase" (regular buy-in
        /// or a consignment receipt).
        /// </summary>
        public int? SupplierId { get; set; }
    }
}
