using MedicalSupply.Application.Common;
using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.Abstractions.Persistence;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Department>> GetAllAsync(CancellationToken ct = default);
    void Add(Department department);

    /// <summary>Sum of TotalAmount for this department's requests that still commit against budget
    /// (i.e. everything except Rejected and Cancelled), optionally excluding one request id
    /// (the request currently being submitted, since its own amount must not double-count) —
    /// used for the remaining-budget check.</summary>
    Task<decimal> GetCommittedAmountAsync(int departmentId, int? excludeRequestId = null, CancellationToken ct = default);
}

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Item>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    void Add(Item item);

    Task<(List<Item> Items, int TotalCount)> SearchAsync(
        string? search, ItemCategory? category, int page, int pageSize, CancellationToken ct = default);
}

public interface ISupplyRequestRepository
{
    Task<SupplyRequest?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SupplyRequest?> GetByRequestNumberAsync(string requestNumber, CancellationToken ct = default);
    void Add(SupplyRequest request);

    Task<(List<SupplyRequest> Requests, int TotalCount)> SearchAsync(
        int? departmentId,
        SupplyRequestStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<bool> RequestNumberExistsAsync(string requestNumber, CancellationToken ct = default);
}
