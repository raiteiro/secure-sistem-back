using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
    {
        public void Configure(EntityTypeBuilder<TaxRate> builder)
        {
            builder.ToTable("TaxRates");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Rate).HasColumnType("decimal(9,4)");
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ModifiedBy).HasMaxLength(100);

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        }
    }
}
