using MedicalSupply.Application.Common;
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

    public ItemsController(ItemService itemService) => _itemService = itemService;

    /// <summary>Creates an inventory item. Restricted to Administrators.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ItemDto>> Create([FromBody] CreateItemRequest request, CancellationToken ct)
    {
        var result = await _itemService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Updates an inventory item. Restricted to Administrators.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDto>> Update(int id, [FromBody] UpdateItemRequest request, CancellationToken ct)
    {
        var result = await _itemService.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _itemService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Searches items by code/name (search), category, with pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ItemDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] ItemCategory? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _itemService.SearchAsync(new ItemSearchRequest(search, category, page, pageSize), ct);
        return Ok(result);
    }
}
