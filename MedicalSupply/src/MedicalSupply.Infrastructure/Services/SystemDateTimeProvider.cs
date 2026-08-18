using MedicalSupply.Application.Abstractions.Services;

namespace MedicalSupply.Infrastructure.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
