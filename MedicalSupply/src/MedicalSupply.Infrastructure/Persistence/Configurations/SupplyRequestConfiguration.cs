using MedicalSupply.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalSupply.Infrastructure.Persistence.Configurations;

public class SupplyRequestConfiguration : IEntityTypeConfiguration<SupplyRequest>
{
    public void Configure(EntityTypeBuilder<SupplyRequest> builder)
    {
        builder.ToTable("SupplyRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestNumber).IsRequired().HasMaxLength(30);
        // Required unique constraint on request numbers (spec Section 10).
        builder.HasIndex(r => r.RequestNumber).IsUnique();

        builder.Property(r => r.RequestedBy).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);

        builder.HasIndex(r => r.DepartmentId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RequestDate);

        builder.HasOne(r => r.Department)
            .WithMany()
            .HasForeignKey(r => r.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.SupplyRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.ApprovalRecords)
            .WithOne()
            .HasForeignKey(a => a.SupplyRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(SupplyRequest.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(SupplyRequest.ApprovalRecords))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
