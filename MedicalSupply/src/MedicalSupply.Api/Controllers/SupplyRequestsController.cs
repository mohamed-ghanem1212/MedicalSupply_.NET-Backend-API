using MedicalSupply.Application.Common;
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

    public SupplyRequestsController(SupplyRequestService service) => _service = service;

    /// <summary>Creates a Draft supply request. Restricted to Requesters.</summary>
    [HttpPost]
    [Authorize(Roles = "Requester,Administrator")]
    [ProducesResponseType(typeof(SupplyRequestDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SupplyRequestDetailsDto>> Create(
        [FromBody] CreateSupplyRequestRequest request, CancellationToken ct)
    {
        var result = await _service.CreateDraftAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SupplyRequestDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplyRequestDetailsDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Searches supply requests by department, status, and date range, with pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SupplyRequestSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupplyRequestSummaryDto>>> Search(
        [FromQuery] int? departmentId,
        [FromQuery] SupplyRequestStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.SearchAsync(
            new SupplyRequestSearchRequest(departmentId, status, fromDate, toDate, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Submits a Draft request, determining the required approval flow.</summary>
    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = "Requester,Administrator")]
    [ProducesResponseType(typeof(SupplyRequestDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplyRequestDetailsDto>> Submit(int id, CancellationToken ct)
    {
        var result = await _service.SubmitAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Approves the currently-pending approval step. The caller's role must match
    /// the approval type in the body (DepartmentManager/Pharmacist/FinanceOfficer);
    /// the decision is attributed to the authenticated caller, not the request body.
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "DepartmentManager,Pharmacist,FinanceOfficer,Administrator")]
    [ProducesResponseType(typeof(SupplyRequestDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplyRequestDetailsDto>> Approve(
        int id, [FromBody] ApprovalActionRequest request, CancellationToken ct)
    {
        var result = await _service.ApproveAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Rejects the currently-pending approval step.</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "DepartmentManager,Pharmacist,FinanceOfficer,Administrator")]
    [ProducesResponseType(typeof(SupplyRequestDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplyRequestDetailsDto>> Reject(
        int id, [FromBody] RejectionActionRequest request, CancellationToken ct)
    {
        var result = await _service.RejectAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Cancels a request (Draft/Submitted/Pending*/Approved, not Fulfilled or Rejected).</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Requester,DepartmentManager,Administrator")]
    [ProducesResponseType(typeof(SupplyRequestDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplyRequestDetailsDto>> Cancel(int id, CancellationToken ct)
    {
        var result = await _service.CancelAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Fulfills an Approved request, reducing reserved and available stock. StoreKeeper only.</summary>
    [HttpPost("{id:int}/fulfill")]
    [Authorize(Roles = "StoreKeeper,Administrator")]
    [ProducesResponseType(typeof(SupplyRequestDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplyRequestDetailsDto>> Fulfill(int id, CancellationToken ct)
    {
        var result = await _service.FulfillAsync(id, ct);
        return Ok(result);
    }
}
