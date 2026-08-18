using MedicalSupply.Application.Abstractions.Persistence;
using MedicalSupply.Application.Abstractions.Security;
using MedicalSupply.Application.Abstractions.Services;
using MedicalSupply.Infrastructure.Identity;
using MedicalSupply.Infrastructure.Persistence;
using MedicalSupply.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalSupply.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core against SQL Server or SQLite depending on configuration.
    /// Set "Database:Provider" to "SqlServer" or "Sqlite" in appsettings.json; the
    /// matching connection string ("DefaultConnection") is used either way. SQLite
    /// is the friction-free default for local review — see README "Running locally".
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<MedicalSupplyDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            else
                options.UseSqlite(connectionString);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IRequestNumberGenerator, RequestNumberGenerator>();
        services.AddSingleton<IUserDirectory, DemoUserDirectory>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
