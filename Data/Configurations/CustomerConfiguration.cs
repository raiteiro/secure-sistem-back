using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.Phone).HasMaxLength(20);
            builder.Property(x => x.TaxId).HasMaxLength(50);
            builder.Property(x => x.Address).HasMaxLength(300);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ModifiedBy).HasMaxLength(100);

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.CompanyId);

            // Email unique per company, but only enforced among customers that have one.
            builder.HasIndex(x => new { x.CompanyId, x.Email })
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL");
        }
    }
}
