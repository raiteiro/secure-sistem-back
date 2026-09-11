using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class NavigationRouteConfiguration : IEntityTypeConfiguration<NavigationRoute>
    {
        public void Configure(EntityTypeBuilder<NavigationRoute> builder)
        {
            builder.ToTable("NavigationRoutes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.WindowName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.RoutePath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Icon)
                .HasMaxLength(100);

            builder.Property(x => x.WindowId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.ModifiedBy)
                .HasMaxLength(100);

            // Self-referencing relationship for parent-child hierarchy
            builder.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Company)
                .WithMany(x => x.NavigationRoutes)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.CompanyId);
            builder.HasIndex(x => new { x.CompanyId, x.ParentId });
        }
    }
}
