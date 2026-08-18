using System.Security.Cryptography;
using System.Text;
using MedicalSupply.Application.Abstractions;
using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Infrastructure.Identity;

// Hardcoded demo users, one per role. All use the password "Passw0rd!".
public class DemoUserDirectory : IUserDirectory
{
    private readonly Dictionary<string, DemoUser> _users;

    public DemoUserDirectory()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("Passw0rd!")));

        _users = new List<DemoUser>
        {
            new("requester@company.com", hash, UserRole.Requester),
            new("manager@company.com", hash, UserRole.DepartmentManager),
            new("storekeeper@company.com", hash, UserRole.StoreKeeper),
            new("admin@company.com", hash, UserRole.Administrator)
        }.ToDictionary(u => u.Email);
    }

    public DemoUser? FindByEmail(string email) =>
        _users.TryGetValue(email.Trim().ToLowerInvariant(), out var user) ? user : null;
}
