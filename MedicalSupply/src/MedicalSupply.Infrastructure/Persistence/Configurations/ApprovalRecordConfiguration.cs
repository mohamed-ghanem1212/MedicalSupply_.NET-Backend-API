using MedicalSupply.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalSupply.Infrastructure.Persistence.Configurations;

public class ApprovalRecordConfiguration : IEntityTypeConfiguration<ApprovalRecord>
{
    public void Configure(EntityTypeBuilder<ApprovalRecord> builder)
    {
        builder.ToTable("ApprovalRecords");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ApprovalType).HasConversion<int>();
        builder.Property(a => a.Decision).HasConversion<int>();
        builder.Property(a => a.DecisionBy).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Comments).HasMaxLength(1000);

        // One decision per approval type per request — enforced at the DB level
        // as a defense-in-depth backstop to the Domain's duplicate-approval check.
        builder.HasIndex(a => new { a.SupplyRequestId, a.ApprovalType }).IsUnique();
    }
}
