using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Key).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
            builder.Property(x => x.WindowId).HasMaxLength(100);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);

            builder.HasIndex(x => x.Key).IsUnique();
        }
    }
}
