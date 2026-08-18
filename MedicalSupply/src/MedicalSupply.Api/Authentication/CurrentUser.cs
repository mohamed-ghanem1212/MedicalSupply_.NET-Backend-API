using System.Security.Claims;
using MedicalSupply.Application.Abstractions;
using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Api.Authentication;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public string Email => User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public UserRole Role
    {
        get
        {
            var roleValue = User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(roleValue, out var role) ? role : default;
        }
    }
}
