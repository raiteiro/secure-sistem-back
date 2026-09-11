using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.TaxId).HasMaxLength(50);
            builder.Property(x => x.Address).HasMaxLength(300);
            builder.Property(x => x.Phone).HasMaxLength(20);
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.Website).HasMaxLength(300);
            builder.Property(x => x.Instagram).HasMaxLength(300);
            builder.Property(x => x.Facebook).HasMaxLength(300);
            builder.Property(x => x.TikTok).HasMaxLength(300);
            builder.Property(x => x.WhatsApp).HasMaxLength(20);
            builder.Property(x => x.LogoPath).HasMaxLength(500);
            builder.Property(x => x.ColorPreset).HasMaxLength(20);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        }
    }
}
