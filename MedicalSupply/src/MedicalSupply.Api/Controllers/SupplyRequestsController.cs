using MedicalSupply.Application.Abstractions;
using MedicalSupply.Application.DTOs;
using MedicalSupply.Application.Services;
using MedicalSupply.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSupply.Api.Controllers;

[ApiController]
[Route("api/supply-requests")]
[Authorize]
public class SupplyRequestsController : ControllerBase
{
    private readonly SupplyRequestService _service;
    private readonly ICurrentUser _currentUser;

    public SupplyRequestsController(SupplyRequestService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "Requester,Administrator")]
    public async Task<ActionResult<SupplyRequestDto>> Create(CreateSupplyRequestRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplyRequestDto>> GetById(int id, CancellationToken ct)
    {
        return Ok(await _service.GetByIdAsync(id, ct));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<SupplyRequestDto>>> Search(
        [FromQuery] int? departmentId,
        [FromQuery] SupplyRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await _service.SearchAsync(departmentId, status, page, pageSize, ct));
    }

    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = "Requester,Administrator")]
    public async Task<ActionResult<SupplyRequestDto>> Submit(int id, CancellationToken ct)
    {
        return Ok(await _service.SubmitAsync(id, ct));
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "DepartmentManager,Administrator")]
    public async Task<ActionResult<SupplyRequestDto>> Approve(int id, CancellationToken ct)
    {
        return Ok(await _service.ApproveAsync(id, _currentUser.Email, ct));
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "DepartmentManager,Administrator")]
    public async Task<ActionResult<SupplyRequestDto>> Reject(int id, RejectRequest request, CancellationToken ct)
    {
        return Ok(await _service.RejectAsync(id, _currentUser.Email, request.Reason, ct));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Requester,DepartmentManager,Administrator")]
    public async Task<ActionResult<SupplyRequestDto>> Cancel(int id, CancellationToken ct)
    {
        return Ok(await _service.CancelAsync(id, ct));
    }

    [HttpPost("{id:int}/fulfill")]
    [Authorize(Roles = "StoreKeeper,Administrator")]
    public async Task<ActionResult<SupplyRequestDto>> Fulfill(int id, CancellationToken ct)
    {
        return Ok(await _service.FulfillAsync(id, ct));
    }
}
