namespace MedicalSupply.Application.DTOs;

public record DepartmentDto(
    int Id,
    string Code,
    string Name,
    bool IsActive,
    decimal MonthlyBudget);

public record CreateDepartmentRequest(
    string Code,
    string Name,
    decimal MonthlyBudget,
    bool IsActive = true);
