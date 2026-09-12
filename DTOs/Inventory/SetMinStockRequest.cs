using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Inventory
{
    public class SetMinStockRequest
    {
        /// <summary>
        /// Low-stock alert threshold. Null clears it (no alert).
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? MinStock { get; set; }
    }
}
