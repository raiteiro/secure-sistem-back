using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
    {
        public void Configure(EntityTypeBuilder<CashSession> builder)
        {
            builder.ToTable("CashSessions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OpeningAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ClosingAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ExpectedAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Difference).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Notes).HasMaxLength(500);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ModifiedBy).HasMaxLength(100);

            builder.HasOne(x => x.CashRegister)
                .WithMany()
                .HasForeignKey(x => x.CashRegisterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.CashRegisterId, x.ClosedAt });
            builder.HasIndex(x => new { x.UserId, x.ClosedAt });
        }
    }
}
