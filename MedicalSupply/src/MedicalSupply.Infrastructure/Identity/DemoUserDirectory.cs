using System.Security.Cryptography;
using System.Text;
using MedicalSupply.Application.Abstractions.Security;
using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Infrastructure.Identity;

/// <summary>
/// Hardcoded demo users, one per role (spec Section 7 explicitly allows this for
/// demonstration). Passwords are stored as SHA-256 hashes purely so the plain text
/// "Passw0rd!" never sits in source as a literal comparison target — this is a
/// convenience for the assessment, not production-grade credential storage. See
/// README "Known limitations" for what a real implementation would use instead
/// (ASP.NET Core Identity, salted+iterated hashing, a real user store).
/// </summary>
public class DemoUserDirectory : IUserDirectory
{
    private static readonly Dictionary<string, DemoUser> Users = BuildUsers();

    public DemoUser? FindByEmail(string email) =>
        Users.TryGetValue(email.Trim().ToLowerInvariant(), out var user) ? user : null;

    private static Dictionary<string, DemoUser> BuildUsers()
    {
        const string password = "Passw0rd!";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

        DemoUser Make(string id, string email, UserRole role) => new(id, email, hash, role);

        var users = new[]
        {
            Make("u-requester", "requester@company.com", UserRole.Requester),
            Make("u-manager", "manager@company.com", UserRole.DepartmentManager),
            Make("u-pharmacist", "pharmacist@company.com", UserRole.Pharmacist),
            Make("u-finance", "finance@company.com", UserRole.FinanceOfficer),
            Make("u-storekeeper", "storekeeper@company.com", UserRole.StoreKeeper),
            Make("u-admin", "admin@company.com", UserRole.Administrator),
        };

        return users.ToDictionary(u => u.Email.ToLowerInvariant());
    }
}
