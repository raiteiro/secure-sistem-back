using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class RoleNavigationRouteConfiguration : IEntityTypeConfiguration<RoleNavigationRoute>
    {
        public void Configure(EntityTypeBuilder<RoleNavigationRoute> builder)
        {
            builder.ToTable("RoleNavigationRoutes");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.NavigationRoute)
                .WithMany()
                .HasForeignKey(x => x.NavigationRouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // A role can only have a route assigned once
            builder.HasIndex(x => new { x.RoleId, x.NavigationRouteId }).IsUnique();
        }
    }
}
