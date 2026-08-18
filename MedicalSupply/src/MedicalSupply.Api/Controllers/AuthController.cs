using MedicalSupply.Application.DTOs;
using MedicalSupply.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSupply.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    /// <summary>
    /// Demo login. Use any of the seeded users' emails with password "Passw0rd!"
    /// (see README "Sample users") to obtain a bearer token for the matching role.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return Ok(result);
    }
}
