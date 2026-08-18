using MedicalSupply.Application.Abstractions.Persistence;
using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Infrastructure.Persistence.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly MedicalSupplyDbContext _db;

    public ItemRepository(MedicalSupplyDbContext db) => _db = db;

    public Task<Item?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Items.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<List<Item>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default) =>
        _db.Items.Where(i => ids.Contains(i.Id)).ToListAsync(ct);

    public void Add(Item item) => _db.Items.Add(item);

    public async Task<(List<Item> Items, int TotalCount)> SearchAsync(
        string? search, ItemCategory? category, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Items.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Code.Contains(term) || i.Name.Contains(term));
        }

        if (category.HasValue)
            query = query.Where(i => i.Category == category.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
