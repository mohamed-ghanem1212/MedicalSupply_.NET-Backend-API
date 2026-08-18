using MedicalSupply.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Application.Abstractions;

// One simple interface instead of separate repositories + a unit of work.
// Services use this directly with normal LINQ, the same way you'd use a DbContext.
public interface IAppDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Item> Items { get; }
    DbSet<SupplyRequest> SupplyRequests { get; }
    DbSet<SupplyRequestItem> SupplyRequestItems { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
