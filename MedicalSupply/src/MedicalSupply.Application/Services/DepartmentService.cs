using MedicalSupply.Application.Abstractions.Persistence;
using MedicalSupply.Application.DTOs;
using MedicalSupply.Application.Exceptions;
using MedicalSupply.Domain.Entities;

namespace MedicalSupply.Application.Services;

public class DepartmentService
{
    private readonly IUnitOfWork _uow;

    public DepartmentService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationAppException("Department code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationAppException("Department name is required.");
        if (request.MonthlyBudget < 0)
            throw new ValidationAppException("Monthly budget cannot be negative.");

        var department = new Department(request.Code, request.Name, request.MonthlyBudget, request.IsActive);
        _uow.Departments.Add(department);
        await _uow.SaveChangesAsync(ct);

        return ToDto(department);
    }

    public async Task<DepartmentDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var department = await _uow.Departments.GetByIdAsync(id, ct)
            ?? throw new NotFoundAppException(nameof(Department), id);
        return ToDto(department);
    }

    public async Task<List<DepartmentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var departments = await _uow.Departments.GetAllAsync(ct);
        return departments.Select(ToDto).ToList();
    }

    private static DepartmentDto ToDto(Department d) =>
        new(d.Id, d.Code, d.Name, d.IsActive, d.MonthlyBudget);
}
