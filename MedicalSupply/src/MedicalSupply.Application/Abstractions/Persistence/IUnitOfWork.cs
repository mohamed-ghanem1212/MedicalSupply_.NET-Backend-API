namespace MedicalSupply.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    IDepartmentRepository Departments { get; }
    IItemRepository Items { get; }
    ISupplyRequestRepository SupplyRequests { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a single database transaction.
    /// On any exception the transaction is rolled back and the exception rethrown,
    /// guaranteeing no partial updates (spec 5.5 / 5.6 / 5.7 "all or nothing").
    /// The Infrastructure implementation also retries once on an EF Core
    /// DbUpdateConcurrencyException up to the caller's discretion — see README.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
}
