using MedicalSupply.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalSupply.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<DepartmentService>();
        services.AddScoped<ItemService>();
        services.AddScoped<SupplyRequestService>();
        services.AddScoped<AuthService>();
        return services;
    }
}
