using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.Abstractions.Security;

public interface ITokenService
{
    string GenerateToken(string userId, string email, UserRole role, out DateTime expiresAtUtc);
}
