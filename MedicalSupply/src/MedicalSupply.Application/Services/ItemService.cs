using MedicalSupply.Application.Abstractions.Persistence;
using MedicalSupply.Application.Common;
using MedicalSupply.Application.DTOs;
using MedicalSupply.Application.Exceptions;
using MedicalSupply.Domain.Entities;

namespace MedicalSupply.Application.Services;

public class ItemService
{
    private readonly IUnitOfWork _uow;

    public ItemService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ItemDto> CreateAsync(CreateItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationAppException("Item code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationAppException("Item name is required.");
        if (request.UnitPrice < 0)
            throw new ValidationAppException("Unit price cannot be negative.");
        if (request.AvailableQuantity < 0)
            throw new ValidationAppException("Available quantity cannot be negative.");

        var item = new Item(
            request.Code,
            request.Name,
            request.Category,
            request.UnitPrice,
            request.AvailableQuantity,
            request.RequiresPharmacyApproval,
            request.IsControlledMedication,
            request.IsActive);

        _uow.Items.Add(item);
        await _uow.SaveChangesAsync(ct);

        return ToDto(item);
    }

    public async Task<ItemDto> UpdateAsync(int id, UpdateItemRequest request, CancellationToken ct = default)
    {
        var item = await _uow.Items.GetByIdAsync(id, ct)
            ?? throw new NotFoundAppException(nameof(Item), id);

        item.UpdateDetails(
            request.Name,
            request.Category,
            request.UnitPrice,
            request.RequiresPharmacyApproval,
            request.IsControlledMedication,
            request.IsActive);

        await _uow.SaveChangesAsync(ct);
        return ToDto(item);
    }

    public async Task<ItemDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _uow.Items.GetByIdAsync(id, ct)
            ?? throw new NotFoundAppException(nameof(Item), id);
        return ToDto(item);
    }

    public async Task<PagedResult<ItemDto>> SearchAsync(ItemSearchRequest request, CancellationToken ct = default)
    {
        var page = new PageRequest { Page = request.Page, PageSize = request.PageSize };
        var (items, totalCount) = await _uow.Items.SearchAsync(
            request.Search, request.Category, page.Page, page.PageSize, ct);

        return new PagedResult<ItemDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = totalCount
        };
    }

    private static ItemDto ToDto(Item i) => new(
        i.Id, i.Code, i.Name, i.Category, i.UnitPrice,
        i.AvailableQuantity, i.ReservedQuantity, i.UnreservedQuantity,
        i.RequiresPharmacyApproval, i.IsControlledMedication, i.IsActive);
}
