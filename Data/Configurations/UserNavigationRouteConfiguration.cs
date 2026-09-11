using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class UserNavigationRouteConfiguration : IEntityTypeConfiguration<UserNavigationRoute>
    {
        public void Configure(EntityTypeBuilder<UserNavigationRoute> builder)
        {
            builder.ToTable("UserNavigationRoutes");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.NavigationRoute)
                .WithMany()
                .HasForeignKey(x => x.NavigationRouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // A user can only have a route assigned once
            builder.HasIndex(x => new { x.UserId, x.NavigationRouteId }).IsUnique();
        }
    }
}
