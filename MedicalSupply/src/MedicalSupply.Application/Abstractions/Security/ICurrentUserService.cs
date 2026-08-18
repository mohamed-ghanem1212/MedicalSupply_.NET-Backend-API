using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.Abstractions.Security;

/// <summary>
/// Resolves the authenticated caller's identity and role directly from the
/// validated JWT principal. Application services authorize against this —
/// never against request-body fields like "decisionBy" — per spec Section 7.
/// </summary>
public interface ICurrentUserService
{
    string UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
}
