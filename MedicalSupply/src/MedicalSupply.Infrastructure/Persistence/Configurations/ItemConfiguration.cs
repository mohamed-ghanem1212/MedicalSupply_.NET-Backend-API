using MedicalSupply.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalSupply.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code).IsRequired().HasMaxLength(30);
        builder.HasIndex(i => i.Code).IsUnique();

        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(i => i.Name);
        builder.HasIndex(i => i.Category);

        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Category).HasConversion<int>();

        builder.Ignore(i => i.UnreservedQuantity);

        // Manually-incremented optimistic-concurrency token — portable across
        // SQL Server and SQLite. See Item.Version doc comment and the README.
        builder.Property(i => i.Version).IsConcurrencyToken();
    }
}
