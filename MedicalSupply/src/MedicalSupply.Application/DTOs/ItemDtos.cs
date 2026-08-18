using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.DTOs;

public class ItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemCategory Category { get; set; }
    public decimal UnitPrice { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int UnreservedQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class CreateItemRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemCategory Category { get; set; }
    public decimal UnitPrice { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateItemRequest
{
    public string Name { get; set; } = string.Empty;
    public ItemCategory Category { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
}
