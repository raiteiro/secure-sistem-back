using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.Models;

namespace SecureSistem.Services
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _context;

        public SaleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(int? SaleId, string? Error)> CreateSaleAsync(SaleCreationRequest request)
        {
            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == request.BranchId && b.CompanyId == request.CompanyId && b.IsActive);
            if (branch is null)
                return (null, "La sucursal debe pertenecer a la misma empresa.");

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == request.CompanyId && w.IsActive);
            if (warehouse is null)
                return (null, "El almacén debe pertenecer a la misma empresa.");

            if (request.CustomerId is not null)
            {
                var customerValid = await _context.Customers
                    .AnyAsync(c => c.Id == request.CustomerId && c.CompanyId == request.CompanyId && c.IsActive);
                if (!customerValid)
                    return (null, "El cliente debe pertenecer a la misma empresa.");
            }

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Include(p => p.TaxRate)
                .Include(p => p.Supplier)
                .Include(p => p.Unit)
                .Where(p => EF.Constant(productIds).Contains(p.Id) && p.CompanyId == request.CompanyId && p.IsActive)
                .ToListAsync();

            if (products.Count != productIds.Count)
                return (null, "Uno o más productos no son válidos.");

            var supply = products.FirstOrDefault(p => p.Kind == "Supply");
            if (supply is not null)
                return (null, $"'{supply.Name}' es un insumo y no se puede vender.");

            var productsById = products.ToDictionary(p => p.Id);

            // Each line's quantity must fit its product's unit (a combo's own unit, not its
            // components' — their deductions are derived, not typed in).
            foreach (var item in request.Items)
            {
                var product = productsById[item.ProductId];
                var decimalsError = QuantityHelper.ValidateDecimals(item.Quantity, product.Unit, product.Name);
                if (decimalsError is not null)
                    return (null, decimalsError);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTimeHelper.Now;

            var saleItems = new List<SaleItem>();
            var consignmentCandidates = new List<(SaleItem Item, Product Product, decimal ConsignorAmount, decimal StoreAmount)>();
            decimal subtotal = 0, discountTotal = 0, taxTotal = 0;

            foreach (var itemRequest in request.Items)
            {
                var product = productsById[itemRequest.ProductId];
                var unitPrice = itemRequest.UnitPriceOverride ?? product.Price;
                var discountAmount = MoneyHelper.Round(itemRequest.DiscountAmount);
                // Every amount is rounded to cents per line, and header totals are sums of
                // those rounded lines — so Total always equals what the lines show.
                var lineGross = MoneyHelper.Round(itemRequest.Quantity * unitPrice);

                if (discountAmount > lineGross)
                    return (null, $"El descuento no puede exceder el total de la línea para el producto '{product.Name}'.");

                var lineSubtotal = lineGross - discountAmount;
                var taxRateValue = product.TaxRate?.Rate ?? 0m;
                var lineTax = MoneyHelper.Round(lineSubtotal * taxRateValue);
                var lineTotal = lineSubtotal + lineTax;

                subtotal += lineGross;
                discountTotal += discountAmount;
                taxTotal += lineTax;

                var saleItem = new SaleItem
                {
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = unitPrice,
                    DiscountAmount = discountAmount,
                    TaxRateValue = taxRateValue,
                    TaxAmount = lineTax,
                    Subtotal = lineSubtotal,
                    Total = lineTotal
                };
                saleItems.Add(saleItem);

                // Consignment split is based on the pre-tax revenue of the line (lineSubtotal)
                // — tax is a pass-through to the government, not real revenue to split.
                if (product.SupplierId is not null && product.Supplier!.IsConsignor)
                {
                    var consignorAmount = MoneyHelper.Round(product.CommissionType == "FixedAmount"
                        ? (product.CommissionValue ?? 0m) * itemRequest.Quantity
                        : lineSubtotal * (1 - (product.CommissionValue ?? 0m) / 100m));
                    var storeAmount = lineSubtotal - consignorAmount;

                    consignmentCandidates.Add((saleItem, product, consignorAmount, storeAmount));
                }

                // A combo carries no stock of its own — deduct each of its components instead,
                // scaled by how many combos are being sold on this line.
                if (product.IsCombo)
                {
                    var comboItems = await _context.ProductComboItems
                        .Include(ci => ci.ComponentProduct)
                        .Where(ci => ci.ComboProductId == product.Id && ci.IsActive)
                        .ToListAsync();

                    foreach (var comboItem in comboItems)
                    {
                        if (!comboItem.ComponentProduct.IsActive)
                            return (null, $"El componente '{comboItem.ComponentProduct.Name}' del combo '{product.Name}' ya no está activo.");

                        var error = await InventoryStockHelper.DeductStockAsync(
                            _context, comboItem.ComponentProductId, comboItem.ComponentProduct.Name,
                            request.WarehouseId, request.CompanyId, comboItem.Quantity * itemRequest.Quantity,
                            "Out", $"Sale (combo '{product.Name}')", request.CurrentUser, now);

                        if (error is not null)
                            return (null, error);
                    }
                }
                else
                {
                    var error = await InventoryStockHelper.DeductStockAsync(
                        _context, product.Id, product.Name, request.WarehouseId, request.CompanyId,
                        itemRequest.Quantity, "Out", "Sale", request.CurrentUser, now);

                    if (error is not null)
                        return (null, error);
                }
            }

            var total = subtotal - discountTotal + taxTotal;

            if (request.Payments.Any(p => !MoneyHelper.IsWholeCents(p.Amount)))
                return (null, "Los montos de pago no pueden tener más de 2 decimales.");

            var paymentsTotal = request.Payments.Sum(p => p.Amount);
            if (paymentsTotal != total)
                return (null, $"Los pagos deben sumar exactamente {total}, se recibió {paymentsTotal}.");

            var folioNumber = 1 + await _context.Sales
                .Where(s => s.CompanyId == request.CompanyId)
                .Select(s => (int?)s.FolioNumber)
                .MaxAsync() ?? 1;

            var sale = new Sale
            {
                FolioNumber = folioNumber,
                BranchId = request.BranchId,
                WarehouseId = request.WarehouseId,
                CustomerId = request.CustomerId,
                CashSessionId = request.CashSessionId,
                QuoteId = request.QuoteId,
                UserId = request.UserId,
                CompanyId = request.CompanyId,
                Status = "Completed",
                Subtotal = subtotal,
                DiscountTotal = discountTotal,
                TaxTotal = taxTotal,
                Total = total,
                CreatedAt = now,
                CreatedBy = request.CurrentUser,
                Items = saleItems,
                Payments = request.Payments.Select(p => new Payment
                {
                    Method = p.Method,
                    Amount = p.Amount,
                    CompanyId = request.CompanyId,
                    CreatedAt = now,
                    CreatedBy = request.CurrentUser
                }).ToList()
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            // SaleItem.Id only exists after the save above, so ConsignmentSale (which points
            // at it) has to be created in a second pass.
            foreach (var candidate in consignmentCandidates)
            {
                _context.ConsignmentSales.Add(new ConsignmentSale
                {
                    SaleItemId = candidate.Item.Id,
                    ProductId = candidate.Product.Id,
                    SupplierId = candidate.Product.SupplierId!.Value,
                    CompanyId = request.CompanyId,
                    Quantity = candidate.Item.Quantity,
                    SaleAmount = candidate.Item.Subtotal,
                    ConsignorAmount = candidate.ConsignorAmount,
                    StoreAmount = candidate.StoreAmount,
                    CreatedAt = now,
                    CreatedBy = request.CurrentUser
                });
            }

            if (consignmentCandidates.Count > 0)
                await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return (sale.Id, null);
        }
    }
}
