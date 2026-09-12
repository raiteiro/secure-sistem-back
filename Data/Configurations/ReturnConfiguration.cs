using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class ReturnConfiguration : IEntityTypeConfiguration<Return>
    {
        public void Configure(EntityTypeBuilder<Return> builder)
        {
            builder.ToTable("Returns");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RefundMethod).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Reason).HasMaxLength(500);
            builder.Property(x => x.SubtotalRefunded).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TaxRefunded).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalRefunded).HasColumnType("decimal(18,2)");
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.Sale)
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CashSession)
                .WithMany()
                .HasForeignKey(x => x.CashSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.SaleId);
            builder.HasIndex(x => x.CashSessionId);
        }
    }
}
