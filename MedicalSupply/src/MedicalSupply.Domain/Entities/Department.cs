namespace MedicalSupply.Domain.Entities;

public class Department
{
    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public decimal MonthlyBudget { get; private set; }

    private Department() { } // EF Core

    public Department(string code, string name, decimal monthlyBudget, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Department code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Department name is required.", nameof(name));
        if (monthlyBudget < 0) throw new ArgumentException("Monthly budget cannot be negative.", nameof(monthlyBudget));

        Code = code;
        Name = name;
        MonthlyBudget = monthlyBudget;
        IsActive = isActive;
    }

    /// <summary>
    /// Remaining budget for the current period. Spent amount is computed by the
    /// application layer from non-Rejected/Cancelled requests for this department,
    /// since "current period" bookkeeping belongs to a query, not to entity state.
    /// </summary>
    public decimal GetRemainingBudget(decimal alreadyCommittedAmount) => MonthlyBudget - alreadyCommittedAmount;
}
