using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class ProductionOrderItemConfiguration : IEntityTypeConfiguration<ProductionOrderItem>
    {
        public void Configure(EntityTypeBuilder<ProductionOrderItem> builder)
        {
            builder.ToTable("ProductionOrderItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuantityPerUnit).HasColumnType("decimal(18,4)");
            builder.Property(x => x.TotalQuantity).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.SupplyProduct)
                .WithMany()
                .HasForeignKey(x => x.SupplyProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
