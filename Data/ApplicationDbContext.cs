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
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<TaxRate> TaxRates => Set<TaxRate>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
        public DbSet<CashSession> CashSessions => Set<CashSession>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Return> Returns => Set<Return>();
        public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<NavigationRoute> NavigationRoutes => Set<NavigationRoute>();
        public DbSet<UserNavigationRoute> UserNavigationRoutes => Set<UserNavigationRoute>();
        public DbSet<RoleNavigationRoute> RoleNavigationRoutes => Set<RoleNavigationRoute>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
