using MedicalSupply.Application.Abstractions;
using MedicalSupply.Application.DTOs;
using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Enums;
using MedicalSupply.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Application.Services;

public class SupplyRequestService
{
    private readonly IAppDbContext _db;

    public SupplyRequestService(IAppDbContext db)
    {
        _db = db;
    }

    // ---------------- Create ----------------

    public async Task<SupplyRequestDto> CreateAsync(CreateSupplyRequestRequest request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            throw new ValidationException("A request must contain at least one item.");

        if (request.Items.Any(i => i.Quantity <= 0))
            throw new ValidationException("Every item quantity must be greater than zero.");

        if (request.Items.Select(i => i.ItemId).Distinct().Count() != request.Items.Count)
            throw new ValidationException("The same item cannot be added twice to one request.");

        var department = await _db.Departments.FindAsync(new object?[] { request.DepartmentId }, ct)
            ?? throw new NotFoundException($"Department {request.DepartmentId} was not found.");

        if (!department.IsActive)
            throw new ValidationException("This department is not active.");

        var supplyRequest = new SupplyRequest
        {
            RequestNumber = await GenerateRequestNumberAsync(ct),
            DepartmentId = department.Id,
            RequestedBy = request.RequestedBy,
            RequestDate = DateTime.UtcNow,
            Status = SupplyRequestStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decimal total = 0;

        foreach (var line in request.Items)
        {
            var item = await _db.Items.FindAsync(new object?[] { line.ItemId }, ct)
                ?? throw new NotFoundException($"Item {line.ItemId} was not found.");

            if (!item.IsActive)
                throw new ValidationException($"Item '{item.Code}' is not active.");

            var lineTotal = item.UnitPrice * line.Quantity;
            total += lineTotal;

            supplyRequest.Items.Add(new SupplyRequestItem
            {
                ItemId = item.Id,
                Quantity = line.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = lineTotal
            });
        }

        supplyRequest.TotalAmount = total;

        _db.SupplyRequests.Add(supplyRequest);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(supplyRequest.Id, ct);
    }

    private async Task<string> GenerateRequestNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"SR-{year}-";
        var countThisYear = await _db.SupplyRequests.CountAsync(r => r.RequestNumber.StartsWith(prefix), ct);
        return $"{prefix}{(countThisYear + 1):D6}";
    }

    // ---------------- Submit ----------------

    public async Task<SupplyRequestDto> SubmitAsync(int id, CancellationToken ct)
    {
        var request = await FindRequestAsync(id, ct);

        if (request.Status != SupplyRequestStatus.Draft)
            throw new ConflictException("Only a Draft request can be submitted.");

        request.Status = SupplyRequestStatus.Submitted;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    // ---------------- Approve ----------------

    public async Task<SupplyRequestDto> ApproveAsync(int id, string approverEmail, CancellationToken ct)
    {
        var request = await FindRequestAsync(id, ct);

        if (request.Status != SupplyRequestStatus.Submitted)
            throw new ConflictException("Only a Submitted request can be approved.");

        // Re-check and reserve stock for every item. If any item no longer has
        // enough stock, nothing is saved - EF Core's SaveChanges is all-or-nothing.
        foreach (var line in request.Items)
        {
            var item = await _db.Items.FindAsync(new object?[] { line.ItemId }, ct)
                ?? throw new NotFoundException($"Item {line.ItemId} was not found.");

            if (line.Quantity > item.UnreservedQuantity)
                throw new ConflictException(
                    $"Not enough stock for '{item.Name}'. Available: {item.UnreservedQuantity}, requested: {line.Quantity}.");

            item.ReservedQuantity += line.Quantity;
            item.Version++;
        }

        request.Status = SupplyRequestStatus.Approved;
        request.DecisionBy = approverEmail;
        request.DecisionDate = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Stock changed while approving this request. Please try again.");
        }

        return await GetByIdAsync(id, ct);
    }

    // ---------------- Reject ----------------

    public async Task<SupplyRequestDto> RejectAsync(int id, string approverEmail, string reason, CancellationToken ct)
    {
        var request = await FindRequestAsync(id, ct);

        if (request.Status != SupplyRequestStatus.Submitted)
            throw new ConflictException("Only a Submitted request can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("A rejection reason is required.");

        request.Status = SupplyRequestStatus.Rejected;
        request.DecisionBy = approverEmail;
        request.DecisionDate = DateTime.UtcNow;
        request.RejectionReason = reason;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    // ---------------- Cancel ----------------

    public async Task<SupplyRequestDto> CancelAsync(int id, CancellationToken ct)
    {
        var request = await FindRequestAsync(id, ct);

        var cancellable = request.Status is SupplyRequestStatus.Draft
            or SupplyRequestStatus.Submitted
            or SupplyRequestStatus.Approved;

        if (!cancellable)
            throw new ConflictException($"A request in {request.Status} status cannot be cancelled.");

        // If it was already approved, release the stock that was reserved for it.
        if (request.Status == SupplyRequestStatus.Approved)
        {
            foreach (var line in request.Items)
            {
                var item = await _db.Items.FindAsync(new object?[] { line.ItemId }, ct);
                if (item is not null)
                {
                    item.ReservedQuantity -= line.Quantity;
                    item.Version++;
                }
            }
        }

        request.Status = SupplyRequestStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    // ---------------- Fulfill ----------------

    public async Task<SupplyRequestDto> FulfillAsync(int id, CancellationToken ct)
    {
        var request = await FindRequestAsync(id, ct);

        if (request.Status != SupplyRequestStatus.Approved)
            throw new ConflictException("Only an Approved request can be fulfilled.");

        foreach (var line in request.Items)
        {
            var item = await _db.Items.FindAsync(new object?[] { line.ItemId }, ct)
                ?? throw new NotFoundException($"Item {line.ItemId} was not found.");

            item.ReservedQuantity -= line.Quantity;
            item.AvailableQuantity -= line.Quantity;
            item.Version++;
        }

        request.Status = SupplyRequestStatus.Fulfilled;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    // ---------------- Read ----------------

    public async Task<SupplyRequestDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var request = await _db.SupplyRequests
            .Include(r => r.Department)
            .Include(r => r.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Supply request {id} was not found.");

        return ToDto(request);
    }

    public async Task<PagedResponse<SupplyRequestDto>> SearchAsync(
        int? departmentId, SupplyRequestStatus? status, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.SupplyRequests
            .Include(r => r.Department)
            .Include(r => r.Items).ThenInclude(i => i.Item)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(r => r.DepartmentId == departmentId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var requests = await query
            .OrderByDescending(r => r.RequestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<SupplyRequestDto>
        {
            Items = requests.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    // ---------------- Helpers ----------------

    private async Task<SupplyRequest> FindRequestAsync(int id, CancellationToken ct)
    {
        return await _db.SupplyRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Supply request {id} was not found.");
    }

    private static SupplyRequestDto ToDto(SupplyRequest r) => new()
    {
        Id = r.Id,
        RequestNumber = r.RequestNumber,
        DepartmentId = r.DepartmentId,
        DepartmentName = r.Department?.Name ?? string.Empty,
        RequestedBy = r.RequestedBy,
        RequestDate = r.RequestDate,
        Status = r.Status,
        TotalAmount = r.TotalAmount,
        DecisionBy = r.DecisionBy,
        DecisionDate = r.DecisionDate,
        RejectionReason = r.RejectionReason,
        Items = r.Items.Select(i => new SupplyRequestItemDto
        {
            ItemId = i.ItemId,
            ItemName = i.Item?.Name ?? string.Empty,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice
        }).ToList()
    };
}
