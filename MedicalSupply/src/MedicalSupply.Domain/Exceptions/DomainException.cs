namespace MedicalSupply.Domain.Exceptions;

/// <summary>
/// Base type for all domain-rule violations. Each subtype carries a stable
/// machine-readable Code so the API layer can map it to the spec's error envelope
/// ({ code, message, details, traceId }) without string-matching messages.
/// </summary>
public abstract class DomainException : Exception
{
    public abstract string Code { get; }
    public virtual IDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();

    protected DomainException(string message) : base(message) { }
}

public sealed class InvalidRequestStateException : DomainException
{
    public override string Code => "INVALID_REQUEST_STATE";
    public InvalidRequestStateException(string message) : base(message) { }
}

public sealed class DuplicateApprovalException : DomainException
{
    public override string Code => "DUPLICATE_APPROVAL";
    public DuplicateApprovalException(string message) : base(message) { }
}

public sealed class WrongApprovalTypeException : DomainException
{
    public override string Code => "WRONG_APPROVAL_TYPE";
    public WrongApprovalTypeException(string message) : base(message) { }
}

public sealed class InsufficientStockException : DomainException
{
    public override string Code => "INSUFFICIENT_STOCK";

    public InsufficientStockException(int itemId, int availableQuantity, int requestedQuantity)
        : base("The requested quantity exceeds the currently available stock.")
    {
        Details["itemId"] = itemId;
        Details["availableQuantity"] = availableQuantity;
        Details["requestedQuantity"] = requestedQuantity;
    }
}

public sealed class BudgetExceededException : DomainException
{
    public override string Code => "BUDGET_EXCEEDED";

    public BudgetExceededException(int departmentId, decimal remainingBudget, decimal requestAmount)
        : base("The request total exceeds the department's remaining monthly budget.")
    {
        Details["departmentId"] = departmentId;
        Details["remainingBudget"] = remainingBudget;
        Details["requestAmount"] = requestAmount;
    }
}

public sealed class DuplicateItemInRequestException : DomainException
{
    public override string Code => "DUPLICATE_ITEM_IN_REQUEST";
    public DuplicateItemInRequestException(int itemId)
        : base($"Item {itemId} was added more than once to the same request.")
    {
        Details["itemId"] = itemId;
    }
}

public sealed class InvalidQuantityException : DomainException
{
    public override string Code => "INVALID_QUANTITY";
    public InvalidQuantityException(string message) : base(message) { }
}

public sealed class InactiveEntityException : DomainException
{
    public override string Code => "INACTIVE_ENTITY";
    public InactiveEntityException(string message) : base(message) { }
}

public sealed class AlreadyFulfilledException : DomainException
{
    public override string Code => "ALREADY_FULFILLED";
    public AlreadyFulfilledException(string message) : base(message) { }
}

public sealed class ConcurrencyConflictException : DomainException
{
    public override string Code => "CONCURRENCY_CONFLICT";
    public ConcurrencyConflictException(string message) : base(message) { }
}
