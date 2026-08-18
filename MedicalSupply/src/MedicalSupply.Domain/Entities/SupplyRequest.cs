using MedicalSupply.Domain.Enums;
using MedicalSupply.Domain.Exceptions;

namespace MedicalSupply.Domain.Entities;

public class SupplyRequest
{
    private const decimal FinanceApprovalThreshold = 10_000m;

    private readonly List<SupplyRequestItem> _items = new();
    private readonly List<ApprovalRecord> _approvalRecords = new();

    public int Id { get; private set; }
    public string RequestNumber { get; private set; } = null!;
    public int DepartmentId { get; private set; }
    public string RequestedBy { get; private set; } = null!;
    public DateTime RequestDate { get; private set; }
    public SupplyRequestStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Determined at submission time (spec 5.2) and fixed thereafter.</summary>
    public bool RequiresPharmacyApproval { get; private set; }
    public bool RequiresFinanceApproval { get; private set; }

    public IReadOnlyCollection<SupplyRequestItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<ApprovalRecord> ApprovalRecords => _approvalRecords.AsReadOnly();

    public Department Department { get; private set; } = null!;

    private SupplyRequest() { } // EF Core

    public SupplyRequest(string requestNumber, int departmentId, string requestedBy, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(requestNumber))
            throw new ArgumentException("Request number is required.", nameof(requestNumber));
        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new ArgumentException("RequestedBy is required.", nameof(requestedBy));

        RequestNumber = requestNumber;
        DepartmentId = departmentId;
        RequestedBy = requestedBy;
        RequestDate = nowUtc;
        Status = SupplyRequestStatus.Draft;
        TotalAmount = 0;
        CreatedAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    /// <summary>
    /// Adds a line item at creation time. UnitPrice must be the Item's *current*
    /// price, resolved by the caller (Application layer) before invoking this —
    /// the Domain layer does not reach out to load Item state itself.
    /// </summary>
    public void AddItem(int itemId, int requestedQuantity, decimal currentUnitPrice)
    {
        if (Status != SupplyRequestStatus.Draft)
            throw new InvalidRequestStateException("Items can only be added while the request is in Draft status.");

        if (_items.Any(i => i.ItemId == itemId))
            throw new DuplicateItemInRequestException(itemId);

        var line = new SupplyRequestItem(itemId, requestedQuantity, currentUnitPrice);
        _items.Add(line);
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(i => i.TotalPrice);
    }

    /// <summary>
    /// Spec 5.2 — Submission. The caller supplies whether any line item requires
    /// pharmacy approval (Application layer resolves this from loaded Item entities)
    /// and the department's remaining budget (Application layer computes this from
    /// other non-rejected/cancelled requests). Throws BudgetExceededException if the
    /// total exceeds the remaining budget.
    /// </summary>
    public void Submit(bool anyItemRequiresPharmacyApproval, decimal departmentRemainingBudget, DateTime nowUtc)
    {
        if (Status != SupplyRequestStatus.Draft)
            throw new InvalidRequestStateException("Only a Draft request can be submitted.");
        if (_items.Count == 0)
            throw new InvalidRequestStateException("A request must contain at least one item before submission.");

        if (TotalAmount > departmentRemainingBudget)
            throw new BudgetExceededException(DepartmentId, departmentRemainingBudget, TotalAmount);

        RequiresPharmacyApproval = anyItemRequiresPharmacyApproval;
        RequiresFinanceApproval = TotalAmount > FinanceApprovalThreshold;

        Status = SupplyRequestStatus.Submitted;
        UpdatedAt = nowUtc;

        // Move immediately into the first required approval stage.
        Status = SupplyRequestStatus.PendingManagerApproval;
    }

    /// <summary>The approval type this request is currently waiting on, or null if none.</summary>
    public ApprovalType? CurrentPendingApprovalType => Status switch
    {
        SupplyRequestStatus.PendingManagerApproval => ApprovalType.DepartmentManager,
        SupplyRequestStatus.PendingPharmacyApproval => ApprovalType.Pharmacy,
        SupplyRequestStatus.PendingFinanceApproval => ApprovalType.Finance,
        _ => null
    };

    /// <summary>
    /// Spec 5.3 — Approval flow. Validates the request is waiting for exactly this
    /// approval type, records the decision, and advances to the next required stage
    /// or to Rejected. Moving to Approved (all steps complete) is signaled via the
    /// return value; actual stock reservation is coordinated by the Application
    /// layer (it needs Item entities this aggregate does not own) and finalized
    /// through <see cref="MarkApproved"/> inside the same transaction.
    /// </summary>
    /// <returns>True if this decision completed every required approval step.</returns>
    public bool ProcessApprovalDecision(
        ApprovalType approvalType,
        ApprovalDecision decision,
        string decisionBy,
        string? comments,
        DateTime nowUtc)
    {
        var expected = CurrentPendingApprovalType
            ?? throw new InvalidRequestStateException("This request is not currently waiting for any approval.");

        if (expected != approvalType)
            throw new WrongApprovalTypeException(
                $"This request is waiting for {expected} approval, not {approvalType}.");

        if (_approvalRecords.Any(r => r.ApprovalType == approvalType))
            throw new DuplicateApprovalException($"{approvalType} approval has already been decided for this request.");

        _approvalRecords.Add(new ApprovalRecord(Id, approvalType, decision, decisionBy, nowUtc, comments));
        UpdatedAt = nowUtc;

        if (decision == ApprovalDecision.Rejected)
        {
            Status = SupplyRequestStatus.Rejected;
            RejectionReason = comments;
            return false;
        }

        var next = DetermineNextStatus();
        Status = next;
        return next == SupplyRequestStatus.Approved;
    }

    private SupplyRequestStatus DetermineNextStatus()
    {
        var completedTypes = _approvalRecords
            .Where(r => r.Decision == ApprovalDecision.Approved)
            .Select(r => r.ApprovalType)
            .ToHashSet();

        if (!completedTypes.Contains(ApprovalType.DepartmentManager))
            return SupplyRequestStatus.PendingManagerApproval;

        if (RequiresPharmacyApproval && !completedTypes.Contains(ApprovalType.Pharmacy))
            return SupplyRequestStatus.PendingPharmacyApproval;

        if (RequiresFinanceApproval && !completedTypes.Contains(ApprovalType.Finance))
            return SupplyRequestStatus.PendingFinanceApproval;

        return SupplyRequestStatus.Approved;
    }

    /// <summary>
    /// Finalizes the transition to Approved once the Application layer has
    /// successfully reserved stock for every line item (spec 5.5). Setting the
    /// approved quantity equal to the requested quantity — partial approval
    /// quantities are not required by the spec and are treated as out of scope
    /// (documented as a known limitation in the README).
    /// </summary>
    public void MarkApproved(DateTime nowUtc)
    {
        if (Status != SupplyRequestStatus.Approved)
            throw new InvalidRequestStateException("MarkApproved can only finalize a request already past its last approval step.");

        foreach (var item in _items)
            item.SetApprovedQuantity(item.RequestedQuantity);

        UpdatedAt = nowUtc;
    }

    /// <summary>Spec 5.6 — Cancellation.</summary>
    public void Cancel(DateTime nowUtc)
    {
        var cancellable = Status is SupplyRequestStatus.Draft
            or SupplyRequestStatus.Submitted
            or SupplyRequestStatus.PendingManagerApproval
            or SupplyRequestStatus.PendingPharmacyApproval
            or SupplyRequestStatus.PendingFinanceApproval
            or SupplyRequestStatus.Approved;

        if (!cancellable)
            throw new InvalidRequestStateException($"A request in {Status} status cannot be cancelled.");

        Status = SupplyRequestStatus.Cancelled;
        UpdatedAt = nowUtc;
    }

    /// <summary>Spec 5.7 — Fulfillment. Only an Approved request may be fulfilled.</summary>
    public void MarkFulfilled(DateTime nowUtc)
    {
        if (Status != SupplyRequestStatus.Approved)
            throw new InvalidRequestStateException("Only an Approved request can be fulfilled.");

        Status = SupplyRequestStatus.Fulfilled;
        UpdatedAt = nowUtc;
    }

    public bool WasApprovedAtLeastOnce => _approvalRecords.Count > 0
        || Status is SupplyRequestStatus.Approved or SupplyRequestStatus.Fulfilled;
}
