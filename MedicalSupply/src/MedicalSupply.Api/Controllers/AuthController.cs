using MedicalSupply.Application.DTOs;
using MedicalSupply.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSupply.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // Demo users (password "Passw0rd!" for all):
    // requester@company.com, manager@company.com, storekeeper@company.com, admin@company.com
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var result = _authService.Login(request);
        return Ok(result);
    }
}
