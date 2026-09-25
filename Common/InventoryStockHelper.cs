using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
using SecureSistem.Models;

namespace SecureSistem.Common
{
    /// <summary>
    /// Shared "adjust stock for one product in one warehouse" logic — upsert Inventory,
    /// snapshot the InventoryMovement — used by every place that deducts or restores stock
    /// (SalesController.Create/Cancel, ReturnsController.Create), including once per
    /// component when the line being sold/cancelled/returned is a combo.
    /// </summary>
    public static class InventoryStockHelper
    {
        /// <summary>
        /// Deducts stock. Returns a Spanish error message if there isn't enough (never
        /// throws for that case, so the caller can turn it into a 400), or null on success.
        /// </summary>
        public static async Task<string?> DeductStockAsync(
            ApplicationDbContext context, int productId, string productName, int warehouseId, int companyId,
            decimal quantity, string movementType, string notes, string currentUser, DateTime now,
            int? supplierId = null)
        {
            var inventory = await context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);
            var currentQuantity = inventory?.Quantity ?? 0m;
            var resultingQuantity = currentQuantity - quantity;

            if (resultingQuantity < 0)
                return $"Stock insuficiente para el producto '{productName}'. Disponible: {currentQuantity}, solicitado: {quantity}.";

            UpsertInventory(context, inventory, productId, warehouseId, companyId, resultingQuantity, currentUser, now);

            context.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                CompanyId = companyId,
                Type = movementType,
                Quantity = -quantity,
                ResultingQuantity = resultingQuantity,
                Notes = notes,
                SupplierId = supplierId,
                CreatedAt = now,
                CreatedBy = currentUser
            });

            return null;
        }

        /// <summary>
        /// Adds stock back (cancellations/returns/purchase receipts) — always succeeds,
        /// restocking never fails a stock check.
        /// </summary>
        public static async Task RestoreStockAsync(
            ApplicationDbContext context, int productId, int warehouseId, int companyId,
            decimal quantity, string movementType, string notes, string currentUser, DateTime now,
            int? supplierId = null)
        {
            var inventory = await context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);
            var currentQuantity = inventory?.Quantity ?? 0m;
            var resultingQuantity = currentQuantity + quantity;

            UpsertInventory(context, inventory, productId, warehouseId, companyId, resultingQuantity, currentUser, now);

            context.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                CompanyId = companyId,
                Type = movementType,
                Quantity = quantity,
                ResultingQuantity = resultingQuantity,
                Notes = notes,
                SupplierId = supplierId,
                CreatedAt = now,
                CreatedBy = currentUser
            });
        }

        private static void UpsertInventory(
            ApplicationDbContext context, Inventory? inventory, int productId, int warehouseId, int companyId,
            decimal resultingQuantity, string currentUser, DateTime now)
        {
            if (inventory is null)
            {
                context.Inventories.Add(new Inventory
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    CompanyId = companyId,
                    Quantity = resultingQuantity,
                    CreatedAt = now,
                    CreatedBy = currentUser
                });
            }
            else
            {
                inventory.Quantity = resultingQuantity;
                inventory.ModifiedAt = now;
                inventory.ModifiedBy = currentUser;
            }
        }
    }
}
