using MedicalSupply.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalSupply.Infrastructure.Persistence.Configurations;

public class SupplyRequestItemConfiguration : IEntityTypeConfiguration<SupplyRequestItem>
{
    public void Configure(EntityTypeBuilder<SupplyRequestItem> builder)
    {
        builder.ToTable("SupplyRequestItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(i => i.TotalPrice).HasColumnType("decimal(18,2)");

        builder.HasOne(i => i.Item)
            .WithMany()
            .HasForeignKey(i => i.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.SupplyRequestId, i.ItemId }).IsUnique();
    }
}
