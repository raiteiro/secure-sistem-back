using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureSistem.Models;

namespace SecureSistem.Data.Configurations
{
    public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
    {
        public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
        {
            builder.ToTable("UnitsOfMeasure");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Abbreviation).IsRequired().HasMaxLength(10);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ModifiedBy).HasMaxLength(100);

            // Case-insensitive uniqueness comes from the database's default (CI) collation;
            // UnitsOfMeasureController also checks it up front for a friendly message.
            builder.HasIndex(x => x.Name).IsUnique();
            builder.HasIndex(x => x.Abbreviation).IsUnique();
        }
    }
}
