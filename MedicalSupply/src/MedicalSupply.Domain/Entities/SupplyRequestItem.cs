namespace MedicalSupply.Domain.Entities;

public class SupplyRequestItem
{
    public int Id { get; set; }
    public int SupplyRequestId { get; set; }
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
