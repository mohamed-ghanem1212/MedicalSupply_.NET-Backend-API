using MedicalSupply.Application.Abstractions.Services;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Infrastructure.Services;

/// <summary>
/// Generates SR-{year}-{sequence} request numbers (e.g. SR-2026-000001).
/// Uniqueness is guaranteed by two layers working together:
///   1. This generator computes "count of requests this year + 1" as the next sequence.
///   2. The database has a UNIQUE INDEX on RequestNumber (SupplyRequestConfiguration).
/// If two requests are created at the same instant and would compute the same
/// sequence, the loser's SaveChanges throws a unique-constraint violation; the
/// caller can retry, which recomputes against the now-updated count. This keeps
/// the generator itself simple while still making duplicates impossible at the
/// data layer — see the README's "Request-number uniqueness" section.
/// </summary>
public class RequestNumberGenerator : IRequestNumberGenerator
{
    private readonly Persistence.MedicalSupplyDbContext _db;

    public RequestNumberGenerator(Persistence.MedicalSupplyDbContext db) => _db = db;

    public async Task<string> GenerateAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var year = nowUtc.Year;
        var prefix = $"SR-{year}-";

        var countThisYear = await _db.SupplyRequests
            .CountAsync(r => r.RequestNumber.StartsWith(prefix), ct);

        var candidate = $"{prefix}{(countThisYear + 1):D6}";

        // Extremely unlikely after the count-based guess, but loop defensively
        // in case of a retry after a unique-constraint collision.
        var attempt = countThisYear + 1;
        while (await _db.SupplyRequests.AnyAsync(r => r.RequestNumber == candidate, ct))
        {
            attempt++;
            candidate = $"{prefix}{attempt:D6}";
        }

        return candidate;
    }
}
