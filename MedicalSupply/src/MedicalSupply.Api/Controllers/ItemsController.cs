using MedicalSupply.Application.DTOs;
using MedicalSupply.Application.Services;
using MedicalSupply.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSupply.Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly ItemService _itemService;

    public ItemsController(ItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ItemDto>> Create(CreateItemRequest request, CancellationToken ct)
    {
        var result = await _itemService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ItemDto>> Update(int id, UpdateItemRequest request, CancellationToken ct)
    {
        return Ok(await _itemService.UpdateAsync(id, request, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> GetById(int id, CancellationToken ct)
    {
        return Ok(await _itemService.GetByIdAsync(id, ct));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ItemDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] ItemCategory? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await _itemService.SearchAsync(search, category, page, pageSize, ct));
    }
}
