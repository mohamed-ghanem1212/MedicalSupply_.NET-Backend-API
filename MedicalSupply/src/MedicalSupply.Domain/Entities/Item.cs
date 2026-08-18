using MedicalSupply.Domain.Enums;
using MedicalSupply.Domain.Exceptions;

namespace MedicalSupply.Domain.Entities;

public class Item
{
    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public ItemCategory Category { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int AvailableQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public bool RequiresPharmacyApproval { get; private set; }
    public bool IsControlledMedication { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Manually-incremented optimistic-concurrency token. Configured as a
    /// EF Core concurrency token (ItemConfiguration) so every UPDATE statement
    /// includes "WHERE Version = @original" — a second concurrent transaction
    /// whose read is now stale gets a DbUpdateConcurrencyException instead of
    /// silently overwriting the reservation. Chosen over a SQL Server "rowversion"
    /// column because it works identically on SQL Server and SQLite (spec allows
    /// either); see the concurrency section of the README for the full rationale.
    /// </summary>
    public int Version { get; private set; }

    public int UnreservedQuantity => AvailableQuantity - ReservedQuantity;

    private Item() { } // EF Core

    public Item(
        string code,
        string name,
        ItemCategory category,
        decimal unitPrice,
        int availableQuantity,
        bool requiresPharmacyApproval,
        bool isControlledMedication,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Item code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Item name is required.", nameof(name));
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        if (availableQuantity < 0) throw new ArgumentException("Available quantity cannot be negative.", nameof(availableQuantity));

        Code = code;
        Name = name;
        Category = category;
        UnitPrice = unitPrice;
        AvailableQuantity = availableQuantity;
        ReservedQuantity = 0;
        RequiresPharmacyApproval = requiresPharmacyApproval;
        IsControlledMedication = isControlledMedication;
        IsActive = isActive;
    }

    public void UpdateDetails(
        string name,
        ItemCategory category,
        decimal unitPrice,
        bool requiresPharmacyApproval,
        bool isControlledMedication,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Item name is required.", nameof(name));
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        Name = name;
        Category = category;
        UnitPrice = unitPrice;
        RequiresPharmacyApproval = requiresPharmacyApproval;
        IsControlledMedication = isControlledMedication;
        IsActive = isActive;
    }

    public void AdjustAvailableQuantity(int delta)
    {
        var next = AvailableQuantity + delta;
        if (next < 0) throw new InvalidQuantityException("Available quantity cannot go negative.");
        AvailableQuantity = next;
        Version++;
    }

    /// <summary>
    /// Validates unreserved stock and reserves the requested quantity.
    /// Called only after all required approvals are complete (spec 5.5).
    /// </summary>
    public void Reserve(int quantity)
    {
        if (quantity <= 0) throw new InvalidQuantityException("Reservation quantity must be greater than zero.");
        if (quantity > UnreservedQuantity)
            throw new InsufficientStockException(Id, UnreservedQuantity, quantity);

        ReservedQuantity += quantity;
        Version++;
    }

    /// <summary>
    /// Releases a previously reserved quantity (request cancellation after Approved, spec 5.6).
    /// </summary>
    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0) throw new InvalidQuantityException("Release quantity must be greater than zero.");
        if (quantity > ReservedQuantity)
            throw new InvalidQuantityException("Cannot release more than is currently reserved.");

        ReservedQuantity -= quantity;
        Version++;
    }

    /// <summary>
    /// Fulfillment (spec 5.7): reduces both ReservedQuantity and AvailableQuantity
    /// by the fulfilled quantity — stock physically leaves inventory.
    /// </summary>
    public void Fulfill(int quantity)
    {
        if (quantity <= 0) throw new InvalidQuantityException("Fulfillment quantity must be greater than zero.");
        if (quantity > ReservedQuantity)
            throw new InvalidQuantityException("Cannot fulfill more than is currently reserved.");
        if (quantity > AvailableQuantity)
            throw new InvalidQuantityException("Cannot fulfill more than is currently available.");

        ReservedQuantity -= quantity;
        AvailableQuantity -= quantity;
        Version++;
    }
}
