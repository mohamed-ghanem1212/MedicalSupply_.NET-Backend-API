using MedicalSupply.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Infrastructure.Persistence;

public class MedicalSupplyDbContext : DbContext
{
    public MedicalSupplyDbContext(DbContextOptions<MedicalSupplyDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<SupplyRequest> SupplyRequests => Set<SupplyRequest>();
    public DbSet<SupplyRequestItem> SupplyRequestItems => Set<SupplyRequestItem>();
    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedicalSupplyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
