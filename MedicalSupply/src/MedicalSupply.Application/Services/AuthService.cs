using System.Security.Cryptography;
using System.Text;
using MedicalSupply.Application.Abstractions.Security;
using MedicalSupply.Application.DTOs;
using MedicalSupply.Application.Exceptions;

namespace MedicalSupply.Application.Services;

public class AuthService
{
    private readonly IUserDirectory _userDirectory;
    private readonly ITokenService _tokenService;

    public AuthService(IUserDirectory userDirectory, ITokenService tokenService)
    {
        _userDirectory = userDirectory;
        _tokenService = tokenService;
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = _userDirectory.FindByEmail(request.Email);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new ValidationAppException("Invalid email or password.");

        var token = _tokenService.GenerateToken(user.UserId, user.Email, user.Role, out var expiresAtUtc);
        return Task.FromResult(new LoginResponse(token, expiresAtUtc, user.Role.ToString(), user.Email));
    }

    private static bool VerifyPassword(string plainText, string storedHash)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainText)));
        return hash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
