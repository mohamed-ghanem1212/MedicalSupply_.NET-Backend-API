using MedicalSupply.Application.Abstractions.Persistence;
using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Infrastructure.Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly MedicalSupplyDbContext _db;

    public DepartmentRepository(MedicalSupplyDbContext db) => _db = db;

    public Task<Department?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<List<Department>> GetAllAsync(CancellationToken ct = default) =>
        _db.Departments.OrderBy(d => d.Name).ToListAsync(ct);

    public void Add(Department department) => _db.Departments.Add(department);

    public Task<decimal> GetCommittedAmountAsync(
        int departmentId, int? excludeRequestId = null, CancellationToken ct = default)
    {
        var query = _db.SupplyRequests.Where(r =>
            r.DepartmentId == departmentId &&
            r.Status != SupplyRequestStatus.Rejected &&
            r.Status != SupplyRequestStatus.Cancelled);

        if (excludeRequestId.HasValue)
            query = query.Where(r => r.Id != excludeRequestId.Value);

        return query.SumAsync(r => r.TotalAmount, ct);
    }
}
