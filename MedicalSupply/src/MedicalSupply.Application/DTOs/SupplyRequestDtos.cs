using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.DTOs;

public class CreateSupplyRequestItemDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class CreateSupplyRequestRequest
{
    public int DepartmentId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public List<CreateSupplyRequestItemDto> Items { get; set; } = new();
}

public class SupplyRequestItemDto
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class SupplyRequestDto
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public SupplyRequestStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? DecisionBy { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? RejectionReason { get; set; }
    public List<SupplyRequestItemDto> Items { get; set; } = new();
}

public class RejectRequest
{
    public string Reason { get; set; } = string.Empty;
}
