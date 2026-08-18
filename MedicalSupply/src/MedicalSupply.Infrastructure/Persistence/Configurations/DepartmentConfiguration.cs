using MedicalSupply.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalSupply.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code).IsRequired().HasMaxLength(20);
        builder.HasIndex(d => d.Code).IsUnique();

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.MonthlyBudget).HasColumnType("decimal(18,2)");
        builder.Property(d => d.IsActive).IsRequired();
    }
}
