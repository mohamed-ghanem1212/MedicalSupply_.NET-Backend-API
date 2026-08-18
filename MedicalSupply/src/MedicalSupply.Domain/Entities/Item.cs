using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Domain.Entities;

public class Item
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemCategory Category { get; set; }
    public decimal UnitPrice { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    // Simple concurrency token - EF Core checks this value hasn't changed
    // before allowing an update. Incremented manually whenever quantities change.
    public int Version { get; set; }

    public int UnreservedQuantity => AvailableQuantity - ReservedQuantity;
}
