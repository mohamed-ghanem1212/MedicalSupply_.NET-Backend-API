using MedicalSupply.Application.Abstractions.Security;
using MedicalSupply.Application.Exceptions;
using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.Common;

/// <summary>
/// Centralizes the "does this authenticated caller's role permit this operation"
/// check (spec Section 7). Administrator always passes. Deliberately checks
/// ICurrentUserService (derived from the validated JWT), never request-body fields.
/// </summary>
public static class AuthorizationGuard
{
    public static void Require(ICurrentUserService currentUser, params UserRole[] allowedRoles)
    {
        if (!currentUser.IsAuthenticated)
            throw new ForbiddenAppException("Authentication is required for this operation.");

        if (currentUser.Role == UserRole.Administrator)
            return;

        if (!allowedRoles.Contains(currentUser.Role))
            throw new ForbiddenAppException(
                $"Role '{currentUser.Role}' is not permitted to perform this operation.");
    }
}
