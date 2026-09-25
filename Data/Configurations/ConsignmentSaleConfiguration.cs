using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class ConsignmentSaleConfiguration : IEntityTypeConfiguration<ConsignmentSale>
    {
        public void Configure(EntityTypeBuilder<ConsignmentSale> builder)
        {
            builder.ToTable("ConsignmentSales");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            builder.Property(x => x.SaleAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ConsignorAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.StoreAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.SaleItem)
                .WithMany()
                .HasForeignKey(x => x.SaleItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Settlement)
                .WithMany(x => x.Sales)
                .HasForeignKey(x => x.SettlementId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.SaleItemId).IsUnique();
            builder.HasIndex(x => new { x.SupplierId, x.SettlementId });
        }
    }
}
