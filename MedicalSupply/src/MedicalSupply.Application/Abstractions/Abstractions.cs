using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.Abstractions;

// Tells services who is calling right now. Implemented in the Api project
// by reading the claims from the validated JWT.
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string Email { get; }
    UserRole Role { get; }
}

public interface ITokenService
{
    string CreateToken(string email, UserRole role, out DateTime expiresAt);
}

public record DemoUser(string Email, string PasswordHash, UserRole Role);

public interface IUserDirectory
{
    DemoUser? FindByEmail(string email);
}
