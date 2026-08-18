using System.Security.Cryptography;
using System.Text;
using MedicalSupply.Application.Abstractions;
using MedicalSupply.Application.DTOs;
using MedicalSupply.Domain.Exceptions;

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

    public LoginResponse Login(LoginRequest request)
    {
        var user = _userDirectory.FindByEmail(request.Email);

        var passwordHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Password)));

        if (user is null || user.PasswordHash != passwordHash)
            throw new ValidationException("Invalid email or password.");

        var token = _tokenService.CreateToken(user.Email, user.Role, out var expiresAt);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Role = user.Role.ToString()
        };
    }
}
