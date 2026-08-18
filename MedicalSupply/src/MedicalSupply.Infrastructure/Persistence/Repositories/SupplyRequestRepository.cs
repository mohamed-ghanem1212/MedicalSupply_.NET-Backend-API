using MedicalSupply.Application.Abstractions.Persistence;
using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Infrastructure.Persistence.Repositories;

public class SupplyRequestRepository : ISupplyRequestRepository
{
    private readonly MedicalSupplyDbContext _db;

    public SupplyRequestRepository(MedicalSupplyDbContext db) => _db = db;

    private IQueryable<SupplyRequest> WithGraph() =>
        _db.SupplyRequests
            .Include(r => r.Items)
            .Include(r => r.ApprovalRecords)
            .Include(r => r.Department);

    public Task<SupplyRequest?> GetByIdAsync(int id, CancellationToken ct = default) =>
        WithGraph().FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<SupplyRequest?> GetByRequestNumberAsync(string requestNumber, CancellationToken ct = default) =>
        WithGraph().FirstOrDefaultAsync(r => r.RequestNumber == requestNumber, ct);

    public void Add(SupplyRequest request) => _db.SupplyRequests.Add(request);

    public Task<bool> RequestNumberExistsAsync(string requestNumber, CancellationToken ct = default) =>
        _db.SupplyRequests.AnyAsync(r => r.RequestNumber == requestNumber, ct);

    public async Task<(List<SupplyRequest> Requests, int TotalCount)> SearchAsync(
        int? departmentId,
        SupplyRequestStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.SupplyRequests.AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(r => r.DepartmentId == departmentId.Value);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        if (fromDate.HasValue)
            query = query.Where(r => r.RequestDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(r => r.RequestDate <= toDate.Value);

        var totalCount = await query.CountAsync(ct);

        var requests = await query
            .OrderByDescending(r => r.RequestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (requests, totalCount);
    }
}
