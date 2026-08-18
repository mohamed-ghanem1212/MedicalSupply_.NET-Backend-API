using MedicalSupply.Application.Abstractions;
using MedicalSupply.Application.DTOs;
using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Application.Services;

public class DepartmentService
{
    private readonly IAppDbContext _db;

    public DepartmentService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationException("Code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");
        if (request.MonthlyBudget < 0)
            throw new ValidationException("Monthly budget cannot be negative.");

        var department = new Department
        {
            Code = request.Code,
            Name = request.Name,
            MonthlyBudget = request.MonthlyBudget,
            IsActive = request.IsActive
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(ct);

        return ToDto(department);
    }

    public async Task<DepartmentDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var department = await _db.Departments.FindAsync(new object?[] { id }, ct)
            ?? throw new NotFoundException($"Department {id} was not found.");

        return ToDto(department);
    }

    public async Task<List<DepartmentDto>> GetAllAsync(CancellationToken ct)
    {
        var departments = await _db.Departments.OrderBy(d => d.Name).ToListAsync(ct);
        return departments.Select(ToDto).ToList();
    }

    private static DepartmentDto ToDto(Department d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        Name = d.Name,
        MonthlyBudget = d.MonthlyBudget,
        IsActive = d.IsActive
    };
}
