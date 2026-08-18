using MedicalSupply.Application.Abstractions;
using MedicalSupply.Application.DTOs;
using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Enums;
using MedicalSupply.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Application.Services;

public class ItemService
{
    private readonly IAppDbContext _db;

    public ItemService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ItemDto> CreateAsync(CreateItemRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationException("Code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");
        if (request.UnitPrice < 0)
            throw new ValidationException("Unit price cannot be negative.");
        if (request.AvailableQuantity < 0)
            throw new ValidationException("Available quantity cannot be negative.");

        var item = new Item
        {
            Code = request.Code,
            Name = request.Name,
            Category = request.Category,
            UnitPrice = request.UnitPrice,
            AvailableQuantity = request.AvailableQuantity,
            IsActive = request.IsActive
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync(ct);

        return ToDto(item);
    }

    public async Task<ItemDto> UpdateAsync(int id, UpdateItemRequest request, CancellationToken ct)
    {
        var item = await _db.Items.FindAsync(new object?[] { id }, ct)
            ?? throw new NotFoundException($"Item {id} was not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");
        if (request.UnitPrice < 0)
            throw new ValidationException("Unit price cannot be negative.");

        item.Name = request.Name;
        item.Category = request.Category;
        item.UnitPrice = request.UnitPrice;
        item.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return ToDto(item);
    }

    public async Task<ItemDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var item = await _db.Items.FindAsync(new object?[] { id }, ct)
            ?? throw new NotFoundException($"Item {id} was not found.");

        return ToDto(item);
    }

    public async Task<PagedResponse<ItemDto>> SearchAsync(
        string? search, ItemCategory? category, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.Items.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.Code.Contains(search) || i.Name.Contains(search));

        if (category.HasValue)
            query = query.Where(i => i.Category == category.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<ItemDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static ItemDto ToDto(Item i) => new()
    {
        Id = i.Id,
        Code = i.Code,
        Name = i.Name,
        Category = i.Category,
        UnitPrice = i.UnitPrice,
        AvailableQuantity = i.AvailableQuantity,
        ReservedQuantity = i.ReservedQuantity,
        UnreservedQuantity = i.UnreservedQuantity,
        IsActive = i.IsActive
    };
}
