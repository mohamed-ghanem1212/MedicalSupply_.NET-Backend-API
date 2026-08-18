using MedicalSupply.Application.Abstractions.Security;
using MedicalSupply.Domain.Enums;
using System.Security.Claims;

namespace MedicalSupply.Api.Authentication;

/// <summary>
/// Reads the caller's identity strictly from the validated JWT principal that
/// ASP.NET Core's authentication middleware has already attached to HttpContext.User.
/// This is deliberately the *only* source of truth for "who is calling and what is
/// their role" — services must never fall back to a request-body field like
/// DecisionBy for authorization (spec Section 7).
/// </summary>
public class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub")
        ?? string.Empty;

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email")
        ?? string.Empty;

    public UserRole Role
    {
        get
        {
            var roleClaim = Principal?.FindFirstValue(ClaimTypes.Role);
            return roleClaim is not null && Enum.TryParse<UserRole>(roleClaim, out var role)
                ? role
                : throw new InvalidOperationException("Authenticated principal has no valid role claim.");
        }
    }
}
