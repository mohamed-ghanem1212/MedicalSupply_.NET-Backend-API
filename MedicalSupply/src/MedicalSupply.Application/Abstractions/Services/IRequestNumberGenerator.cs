namespace MedicalSupply.Application.Abstractions.Services;

/// <summary>
/// Generates unique request numbers in the SR-{year}-{sequence} format
/// (e.g. SR-2026-000001). The Infrastructure implementation and the unique
/// database constraint on RequestNumber together guarantee uniqueness even
/// under concurrent creation — see the README's "Request-number uniqueness" section.
/// </summary>
public interface IRequestNumberGenerator
{
    Task<string> GenerateAsync(DateTime nowUtc, CancellationToken ct = default);
}
