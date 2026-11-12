using OrderHub.Application.Interfaces.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class WarehouseRepository(AppDbContext context) : IWarehouseRepository
{
    public Task<Warehouse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        context.Warehouses.FirstOrDefaultAsync(warehouse => warehouse.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WarehouseListItem>> GetListAsync(
        CancellationToken cancellationToken = default) =>
        await context.Warehouses
            .AsNoTracking()
            .OrderBy(warehouse => warehouse.Name.Value)
            .ThenBy(warehouse => warehouse.Id)
            .Select(warehouse => new WarehouseListItem(
                warehouse.Id,
                warehouse.Name.Value,
                warehouse.Keeper.DisplayName,
                warehouse.IsActive,
                0))
            .ToListAsync(cancellationToken);

    public Task<WarehouseEdit> GetForEditAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        context.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.Id == id)
            .Select(warehouse => new WarehouseEdit(
                warehouse.Id,
                warehouse.Name.Value,
                warehouse.KeeperUserId,
                warehouse.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        string normalized = name?.Trim().ToLowerInvariant() ?? string.Empty;

        return context.Warehouses.AnyAsync(
            warehouse => warehouse.Name.Value.ToLower() == normalized &&
                (!excludingId.HasValue || warehouse.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> IsKeeperOfAnotherWarehouseAsync(
        int keeperUserId,
        int? excludingWarehouseId = null,
        CancellationToken cancellationToken = default) =>
        context.Warehouses.AnyAsync(
            warehouse => warehouse.KeeperUserId == keeperUserId &&
                (!excludingWarehouseId.HasValue || warehouse.Id != excludingWarehouseId.Value),
            cancellationToken);

    public Task<bool> HasPositiveStockAsync(
        int warehouseId,
        CancellationToken cancellationToken = default) =>
        context.StockBalances.AnyAsync(
            balance => balance.WarehouseId == warehouseId &&
                (balance.StockOnHand > 0 || balance.DamagedQty > 0),
            cancellationToken);

    public void Add(Warehouse warehouse) => context.Warehouses.Add(warehouse);
}
