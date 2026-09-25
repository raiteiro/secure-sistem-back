using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class ProductComboItemConfiguration : IEntityTypeConfiguration<ProductComboItem>
    {
        public void Configure(EntityTypeBuilder<ProductComboItem> builder)
        {
            builder.ToTable("ProductComboItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.ComboProduct)
                .WithMany()
                .HasForeignKey(x => x.ComboProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ComponentProduct)
                .WithMany()
                .HasForeignKey(x => x.ComponentProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // A given component only appears once per combo (adjust its Quantity instead of duplicating).
            builder.HasIndex(x => new { x.ComboProductId, x.ComponentProductId }).IsUnique();
        }
    }
}
