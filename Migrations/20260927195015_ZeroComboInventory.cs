using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <summary>
    /// Zeroes out any stock recorded against combo products. A combo carries no stock of its
    /// own (sales deduct its components), so that stock was never used and only inflated the
    /// inventory. Rows are kept (never physically deleted) at Quantity = 0, and each one gets
    /// an "Adjustment" movement so the ledger still explains where the stock went.
    /// Naturally idempotent: rows already at 0 are left alone.
    /// </summary>
    public partial class ZeroComboInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Business timestamps are stored as naive Mexico City time (see Common/DateTimeHelper.cs).
DECLARE @now datetime2 = DATEADD(HOUR, -6, SYSUTCDATETIME());

INSERT INTO [InventoryMovements] ([ProductId], [WarehouseId], [CompanyId], [Type], [Quantity], [ResultingQuantity], [Notes], [CreatedAt], [CreatedBy])
SELECT i.[ProductId], i.[WarehouseId], i.[CompanyId], N'Adjustment', -i.[Quantity], 0,
       N'Ajuste automático: un combo no tiene stock propio, su disponibilidad se calcula con el stock de sus componentes.',
       @now, N'system'
FROM [Inventories] i
JOIN [Products] p ON p.[Id] = i.[ProductId]
WHERE p.[IsCombo] = 1 AND i.[Quantity] <> 0;

UPDATE i
SET i.[Quantity] = 0, i.[ModifiedAt] = @now, i.[ModifiedBy] = N'system'
FROM [Inventories] i
JOIN [Products] p ON p.[Id] = i.[ProductId]
WHERE p.[IsCombo] = 1 AND i.[Quantity] <> 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally a no-op: the zeroed stock was never usable, and the Adjustment
            // movements above record what each row held if it ever needs to be reviewed.
        }
    }
}
