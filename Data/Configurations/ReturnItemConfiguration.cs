using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
    {
        public void Configure(EntityTypeBuilder<ReturnItem> builder)
        {
            builder.ToTable("ReturnItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TaxRateValue).HasColumnType("decimal(9,4)");
            builder.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Total).HasColumnType("decimal(18,2)");

            builder.HasOne(x => x.Return)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.SaleItem)
                .WithMany()
                .HasForeignKey(x => x.SaleItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ReturnId);
            builder.HasIndex(x => x.SaleItemId);
        }
    }
}
