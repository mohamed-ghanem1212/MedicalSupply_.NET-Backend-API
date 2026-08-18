using MedicalSupply.Application.DTOs;
using MedicalSupply.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSupply.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentService _departmentService;

    public DepartmentsController(DepartmentService departmentService) => _departmentService = departmentService;

    /// <summary>Creates a department. Restricted to Administrators.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DepartmentDto>> Create(
        [FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        var result = await _departmentService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _departmentService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DepartmentDto>>> GetAll(CancellationToken ct)
    {
        var result = await _departmentService.GetAllAsync(ct);
        return Ok(result);
    }
}
