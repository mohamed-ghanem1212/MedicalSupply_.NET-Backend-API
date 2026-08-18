using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Domain.Entities;

public class SupplyRequest
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public SupplyRequestStatus Status { get; set; } = SupplyRequestStatus.Draft;
    public decimal TotalAmount { get; set; }

    // Single-step approval info (no multi-stage approval flow).
    public string? DecisionBy { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<SupplyRequestItem> Items { get; set; } = new();
}
