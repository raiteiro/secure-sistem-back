using Microsoft.EntityFrameworkCore;
using SecureSistem.Models;

namespace SecureSistem.Data
{
    /// <summary>
    /// Main database context for the application.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<NavigationRoute> NavigationRoutes => Set<NavigationRoute>();
        public DbSet<UserNavigationRoute> UserNavigationRoutes => Set<UserNavigationRoute>();
        public DbSet<RoleNavigationRoute> RoleNavigationRoutes => Set<RoleNavigationRoute>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
