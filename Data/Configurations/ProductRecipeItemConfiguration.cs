using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class ProductRecipeItemConfiguration : IEntityTypeConfiguration<ProductRecipeItem>
    {
        public void Configure(EntityTypeBuilder<ProductRecipeItem> builder)
        {
            builder.ToTable("ProductRecipeItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SupplyProduct)
                .WithMany()
                .HasForeignKey(x => x.SupplyProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // A given supply only appears once per recipe (adjust its Quantity instead of duplicating).
            builder.HasIndex(x => new { x.ProductId, x.SupplyProductId }).IsUnique();
        }
    }
}
