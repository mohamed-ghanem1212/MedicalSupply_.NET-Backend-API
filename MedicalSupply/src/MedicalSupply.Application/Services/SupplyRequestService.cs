using MedicalSupply.Application.Abstractions.Persistence;
using MedicalSupply.Application.Abstractions.Security;
using MedicalSupply.Application.Abstractions.Services;
using MedicalSupply.Application.Common;
using MedicalSupply.Application.DTOs;
using MedicalSupply.Application.Exceptions;
using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Enums;
using MedicalSupply.Domain.Exceptions;

namespace MedicalSupply.Application.Services;

public class SupplyRequestService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IRequestNumberGenerator _requestNumberGenerator;

    public SupplyRequestService(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IRequestNumberGenerator requestNumberGenerator)
    {
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
        _requestNumberGenerator = requestNumberGenerator;
    }

    // ---------------------------------------------------------------
    // 5.1 Creation
    // ---------------------------------------------------------------
    public async Task<SupplyRequestDetailsDto> CreateDraftAsync(
        CreateSupplyRequestRequest request, CancellationToken ct = default)
    {
        AuthorizationGuard.Require(_currentUser, UserRole.Requester);

        if (request.Items is null || request.Items.Count == 0)
            throw new ValidationAppException("A request must contain at least one item.");
        if (request.Items.Any(i => i.RequestedQuantity <= 0))
            throw new ValidationAppException("The requested quantity for every item must be greater than zero.");
        if (request.Items.Select(i => i.ItemId).Distinct().Count() != request.Items.Count)
            throw new ValidationAppException("The same item must not be added more than once to the same request.");

        var department = await _uow.Departments.GetByIdAsync(request.DepartmentId, ct)
            ?? throw new NotFoundAppException(nameof(Department), request.DepartmentId);
        if (!department.IsActive)
            throw new ValidationAppException("The selected department is not active.");

        var itemIds = request.Items.Select(i => i.ItemId).ToList();
        var items = await _uow.Items.GetByIdsAsync(itemIds, ct);
        var itemsById = items.ToDictionary(i => i.Id);

        foreach (var line in request.Items)
        {
            if (!itemsById.TryGetValue(line.ItemId, out var item))
                throw new NotFoundAppException(nameof(Item), line.ItemId);
            if (!item.IsActive)
                throw new ValidationAppException($"Item '{item.Code}' is not active.");
        }

        var requestNumber = await _requestNumberGenerator.GenerateAsync(_clock.UtcNow, ct);
        var supplyRequest = new SupplyRequest(requestNumber, department.Id, request.RequestedBy, _clock.UtcNow);

        foreach (var line in request.Items)
        {
            var item = itemsById[line.ItemId];
            supplyRequest.AddItem(item.Id, line.RequestedQuantity, item.UnitPrice);
        }

        _uow.SupplyRequests.Add(supplyRequest);
        await _uow.SaveChangesAsync(ct);

        return await BuildDetailsDtoAsync(supplyRequest, ct);
    }

    // ---------------------------------------------------------------
    // 5.2 Submission
    // ---------------------------------------------------------------
    public async Task<SupplyRequestDetailsDto> SubmitAsync(int id, CancellationToken ct = default)
    {
        AuthorizationGuard.Require(_currentUser, UserRole.Requester);

        var supplyRequest = await _uow.SupplyRequests.GetByIdAsync(id, ct)
            ?? throw new NotFoundAppException(nameof(SupplyRequest), id);

        var itemIds = supplyRequest.Items.Select(i => i.ItemId);
        var items = await _uow.Items.GetByIdsAsync(itemIds, ct);
        var anyRequiresPharmacy = items.Any(i => i.RequiresPharmacyApproval || i.IsControlledMedication);

        var committed = await _uow.Departments.GetCommittedAmountAsync(
            supplyRequest.DepartmentId, excludeRequestId: supplyRequest.Id, ct);
        var department = await _uow.Departments.GetByIdAsync(supplyRequest.DepartmentId, ct)
            ?? throw new NotFoundAppException(nameof(Department), supplyRequest.DepartmentId);
        var remainingBudget = department.GetRemainingBudget(committed);

        supplyRequest.Submit(anyRequiresPharmacy, remainingBudget, _clock.UtcNow);

        await _uow.SaveChangesAsync(ct);
        return await BuildDetailsDtoAsync(supplyRequest, ct);
    }

    // ---------------------------------------------------------------
    // 5.3 Approval flow (approve)
    // ---------------------------------------------------------------
    public async Task<SupplyRequestDetailsDto> ApproveAsync(
        int id, ApprovalActionRequest request, CancellationToken ct = default)
    {
        RequireRoleForApprovalType(request.ApprovalType);

        // decisionBy is taken from the authenticated caller, never trusted from the body.
        var decisionBy = _currentUser.Email;

        return await _uow.ExecuteInTransactionAsync(async innerCt =>
        {
            var supplyRequest = await _uow.SupplyRequests.GetByIdAsync(id, innerCt)
                ?? throw new NotFoundAppException(nameof(SupplyRequest), id);

            var fullyApproved = supplyRequest.ProcessApprovalDecision(
                request.ApprovalType, ApprovalDecision.Approved, decisionBy, request.Comments, _clock.UtcNow);

            if (fullyApproved)
            {
                // 5.4 / 5.5 — revalidate and reserve stock for every line item atomically.
                var itemIds = supplyRequest.Items.Select(i => i.ItemId);
                var items = await _uow.Items.GetByIdsAsync(itemIds, innerCt);
                var itemsById = items.ToDictionary(i => i.Id);

                foreach (var line in supplyRequest.Items)
                {
                    var item = itemsById[line.ItemId];
                    item.Reserve(line.RequestedQuantity); // throws InsufficientStockException -> rolls back whole op
                }

                supplyRequest.MarkApproved(_clock.UtcNow);
            }

            await _uow.SaveChangesAsync(innerCt);
            return await BuildDetailsDtoAsync(supplyRequest, innerCt);
        }, ct);
    }

    // ---------------------------------------------------------------
    // 5.3 Approval flow (reject)
    // ---------------------------------------------------------------
    public async Task<SupplyRequestDetailsDto> RejectAsync(
        int id, RejectionActionRequest request, CancellationToken ct = default)
    {
        RequireRoleForApprovalType(request.ApprovalType);
        var decisionBy = _currentUser.Email;

        var supplyRequest = await _uow.SupplyRequests.GetByIdAsync(id, ct)
            ?? throw new NotFoundAppException(nameof(SupplyRequest), id);

        supplyRequest.ProcessApprovalDecision(
            request.ApprovalType, ApprovalDecision.Rejected, decisionBy, request.Reason, _clock.UtcNow);

        await _uow.SaveChangesAsync(ct);
        return await BuildDetailsDtoAsync(supplyRequest, ct);
    }

    // ---------------------------------------------------------------
    // 5.6 Cancellation
    // ---------------------------------------------------------------
    public async Task<SupplyRequestDetailsDto> CancelAsync(int id, CancellationToken ct = default)
    {
        AuthorizationGuard.Require(_currentUser, UserRole.Requester, UserRole.DepartmentManager);

        return await _uow.ExecuteInTransactionAsync(async innerCt =>
        {
            var supplyRequest = await _uow.SupplyRequests.GetByIdAsync(id, innerCt)
                ?? throw new NotFoundAppException(nameof(SupplyRequest), id);

            var wasApproved = supplyRequest.Status == SupplyRequestStatus.Approved;

            supplyRequest.Cancel(_clock.UtcNow);

            if (wasApproved)
            {
                var itemIds = supplyRequest.Items.Select(i => i.ItemId);
                var items = await _uow.Items.GetByIdsAsync(itemIds, innerCt);
                var itemsById = items.ToDictionary(i => i.Id);

                foreach (var line in supplyRequest.Items)
                {
                    itemsById[line.ItemId].ReleaseReservation(line.RequestedQuantity);
                }
            }

            await _uow.SaveChangesAsync(innerCt);
            return await BuildDetailsDtoAsync(supplyRequest, innerCt);
        }, ct);
    }

    // ---------------------------------------------------------------
    // 5.7 Fulfillment
    // ---------------------------------------------------------------
    public async Task<SupplyRequestDetailsDto> FulfillAsync(int id, CancellationToken ct = default)
    {
        AuthorizationGuard.Require(_currentUser, UserRole.StoreKeeper);

        return await _uow.ExecuteInTransactionAsync(async innerCt =>
        {
            var supplyRequest = await _uow.SupplyRequests.GetByIdAsync(id, innerCt)
                ?? throw new NotFoundAppException(nameof(SupplyRequest), id);

            supplyRequest.MarkFulfilled(_clock.UtcNow); // throws if not Approved -> also blocks double-fulfillment

            var itemIds = supplyRequest.Items.Select(i => i.ItemId);
            var items = await _uow.Items.GetByIdsAsync(itemIds, innerCt);
            var itemsById = items.ToDictionary(i => i.Id);

            foreach (var line in supplyRequest.Items)
            {
                var quantity = line.ApprovedQuantity ?? line.RequestedQuantity;
                itemsById[line.ItemId].Fulfill(quantity);
            }

            await _uow.SaveChangesAsync(innerCt);
            return await BuildDetailsDtoAsync(supplyRequest, innerCt);
        }, ct);
    }

    // ---------------------------------------------------------------
    // Queries
    // ---------------------------------------------------------------
    public async Task<SupplyRequestDetailsDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var supplyRequest = await _uow.SupplyRequests.GetByIdAsync(id, ct)
            ?? throw new NotFoundAppException(nameof(SupplyRequest), id);
        return await BuildDetailsDtoAsync(supplyRequest, ct);
    }

    public async Task<PagedResult<SupplyRequestSummaryDto>> SearchAsync(
        SupplyRequestSearchRequest request, CancellationToken ct = default)
    {
        var page = new PageRequest { Page = request.Page, PageSize = request.PageSize };
        var (requests, totalCount) = await _uow.SupplyRequests.SearchAsync(
            request.DepartmentId, request.Status, request.FromDate, request.ToDate,
            page.Page, page.PageSize, ct);

        var departmentIds = requests.Select(r => r.DepartmentId).Distinct().ToList();
        var departments = new Dictionary<int, Department>();
        foreach (var depId in departmentIds)
        {
            var dep = await _uow.Departments.GetByIdAsync(depId, ct);
            if (dep is not null) departments[depId] = dep;
        }

        var items = requests.Select(r => new SupplyRequestSummaryDto(
            r.Id, r.RequestNumber, r.DepartmentId,
            departments.TryGetValue(r.DepartmentId, out var dep) ? dep.Name : "Unknown",
            r.RequestedBy, r.RequestDate, r.Status, r.TotalAmount)).ToList();

        return new PagedResult<SupplyRequestSummaryDto>
        {
            Items = items,
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = totalCount
        };
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    private void RequireRoleForApprovalType(ApprovalType approvalType)
    {
        var requiredRole = approvalType switch
        {
            ApprovalType.DepartmentManager => UserRole.DepartmentManager,
            ApprovalType.Pharmacy => UserRole.Pharmacist,
            ApprovalType.Finance => UserRole.FinanceOfficer,
            _ => throw new ValidationAppException("Unknown approval type.")
        };

        AuthorizationGuard.Require(_currentUser, requiredRole);
    }

    private async Task<SupplyRequestDetailsDto> BuildDetailsDtoAsync(SupplyRequest r, CancellationToken ct)
    {
        var department = await _uow.Departments.GetByIdAsync(r.DepartmentId, ct);
        var itemIds = r.Items.Select(i => i.ItemId);
        var items = await _uow.Items.GetByIdsAsync(itemIds, ct);
        var itemsById = items.ToDictionary(i => i.Id);

        var requiredApprovals = new List<ApprovalType> { ApprovalType.DepartmentManager };
        if (r.RequiresPharmacyApproval) requiredApprovals.Add(ApprovalType.Pharmacy);
        if (r.RequiresFinanceApproval) requiredApprovals.Add(ApprovalType.Finance);

        return new SupplyRequestDetailsDto(
            r.Id, r.RequestNumber, r.DepartmentId, department?.Name ?? "Unknown",
            r.RequestedBy, r.RequestDate, r.Status, r.TotalAmount, r.RejectionReason,
            r.CreatedAt, r.UpdatedAt, r.RequiresPharmacyApproval, r.RequiresFinanceApproval,
            r.Items.Select(i =>
            {
                itemsById.TryGetValue(i.ItemId, out var item);
                return new SupplyRequestItemDto(
                    i.Id, i.ItemId, item?.Code ?? "", item?.Name ?? "",
                    i.RequestedQuantity, i.ApprovedQuantity, i.UnitPrice, i.TotalPrice);
            }).ToList(),
            requiredApprovals,
            r.ApprovalRecords.Select(a => new ApprovalRecordDto(
                a.ApprovalType, a.Decision, a.DecisionBy, a.DecisionDate, a.Comments)).ToList());
    }
}
