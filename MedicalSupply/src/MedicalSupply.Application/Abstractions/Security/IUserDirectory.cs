using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.Abstractions.Security;

public record DemoUser(string UserId, string Email, string PasswordHash, UserRole Role);

/// <summary>
/// Looks up the hardcoded demo users (spec Section 7 permits hardcoded users for
/// demonstration purposes). Implemented in Infrastructure so Application never
/// hand-rolls password hashing/storage details.
/// </summary>
public interface IUserDirectory
{
    DemoUser? FindByEmail(string email);
}
