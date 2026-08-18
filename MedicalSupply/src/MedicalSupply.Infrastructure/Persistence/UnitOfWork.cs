using MedicalSupply.Application.Abstractions.Persistence;
using MedicalSupply.Domain.Exceptions;
using MedicalSupply.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Infrastructure.Persistence;

/// <summary>
/// Wraps the DbContext as the single transaction boundary for a use case.
/// Services that mutate more than one aggregate (e.g. approving a request also
/// reserves stock on Items) call <see cref="ExecuteInTransactionAsync{T}"/> so
/// every write commits or rolls back together (spec 5.5/5.6/5.7).
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly MedicalSupplyDbContext _db;

    public IDepartmentRepository Departments { get; }
    public IItemRepository Items { get; }
    public ISupplyRequestRepository SupplyRequests { get; }

    public UnitOfWork(MedicalSupplyDbContext db)
    {
        _db = db;
        Departments = new DepartmentRepository(db);
        Items = new ItemRepository(db);
        SupplyRequests = new SupplyRequestRepository(db);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        // SQLite and SQL Server both default to Read Committed-equivalent isolation
        // here; the optimistic Version token on Item (see ItemConfiguration) is what
        // actually prevents two concurrent approvals from over-reserving stock —
        // the transaction's job is only to make the approval-record + stock-reserve
        // + status-change a single all-or-nothing unit.
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operation(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new ConcurrencyConflictException(
                "The item's stock was modified by another request. Please retry the operation.");
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
