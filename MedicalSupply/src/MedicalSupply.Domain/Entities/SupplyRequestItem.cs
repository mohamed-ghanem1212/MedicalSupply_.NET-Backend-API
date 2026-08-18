using MedicalSupply.Domain.Exceptions;

namespace MedicalSupply.Domain.Entities;

public class SupplyRequestItem
{
    public int Id { get; private set; }
    public int SupplyRequestId { get; private set; }
    public int ItemId { get; private set; }
    public int RequestedQuantity { get; private set; }
    public int? ApprovedQuantity { get; private set; }

    /// <summary>Snapshot of Item.UnitPrice at request-creation time (spec 5.1.6).</summary>
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }

    // Navigation, read-only from outside the aggregate.
    public Item Item { get; private set; } = null!;

    private SupplyRequestItem() { } // EF Core

    internal SupplyRequestItem(int itemId, int requestedQuantity, decimal unitPriceAtCreation)
    {
        if (requestedQuantity <= 0)
            throw new InvalidQuantityException("Requested quantity must be greater than zero.");

        ItemId = itemId;
        RequestedQuantity = requestedQuantity;
        UnitPrice = unitPriceAtCreation;
        TotalPrice = requestedQuantity * unitPriceAtCreation;
    }

    internal void SetApprovedQuantity(int quantity) => ApprovedQuantity = quantity;
}
