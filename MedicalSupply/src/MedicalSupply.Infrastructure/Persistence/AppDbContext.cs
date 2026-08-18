using MedicalSupply.Application.Abstractions;
using MedicalSupply.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<SupplyRequest> SupplyRequests => Set<SupplyRequest>();
    public DbSet<SupplyRequestItem> SupplyRequestItems => Set<SupplyRequestItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(e =>
        {
            e.HasIndex(d => d.Code).IsUnique();
            e.Property(d => d.MonthlyBudget).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Item>(e =>
        {
            e.HasIndex(i => i.Code).IsUnique();
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            e.Ignore(i => i.UnreservedQuantity);
            e.Property(i => i.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<SupplyRequest>(e =>
        {
            e.HasIndex(r => r.RequestNumber).IsUnique();
            e.Property(r => r.TotalAmount).HasColumnType("decimal(18,2)");
            e.HasOne(r => r.Department).WithMany().HasForeignKey(r => r.DepartmentId);
        });

        modelBuilder.Entity<SupplyRequestItem>(e =>
        {
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(i => i.TotalPrice).HasColumnType("decimal(18,2)");
            e.HasOne(i => i.Item).WithMany().HasForeignKey(i => i.ItemId);
        });
    }
}
