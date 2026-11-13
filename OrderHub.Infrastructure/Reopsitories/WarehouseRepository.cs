using OrderHub.Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.WarehouseStock;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class WarehouseRepository(AppDbContext context) : IWarehouseRepository
{
    public Task<Warehouse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        context.Warehouses
            .Include(warehouse => warehouse.Address)
            .ThenInclude(address => address.City)
            .Include(warehouse => warehouse.Phone)
            .FirstOrDefaultAsync(warehouse => warehouse.Id == id, cancellationToken);

    public Task<Warehouse> GetByKeeperAsync(
        int keeperUserId,
        CancellationToken cancellationToken = default) =>
        context.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(warehouse => warehouse.KeeperUserId == keeperUserId, cancellationToken);

    public async Task<IReadOnlyList<WarehouseListItem>> GetListAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await context.Warehouses
            .AsNoTracking()
            .OrderBy(warehouse => warehouse.Name.Value)
            .ThenBy(warehouse => warehouse.Id)
            .Select(warehouse => new
            {
                warehouse.Id,
                Name = warehouse.Name.Value,
                KeeperDisplayName = warehouse.Keeper.DisplayName,
                warehouse.IsActive,
                CityName = warehouse.Address == null ? null : warehouse.Address.City.Name.Value,
                Street = warehouse.Address == null ? null : warehouse.Address.Street,
                PhoneNumber = warehouse.Phone == null
                    ? null
                    : warehouse.Phone.Number.CountryCode + " " + warehouse.Phone.Number.NationalNumber
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new WarehouseListItem(
                row.Id,
                row.Name,
                row.KeeperDisplayName,
                row.IsActive,
                0,
                row.Street is null ? null : $"{row.CityName} - {row.Street}",
                row.PhoneNumber))
            .ToList();
    }

    public Task<InventoryWarehouseHeaderRow> GetInventoryHeaderAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        context.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.Id == id)
            .Select(warehouse => new InventoryWarehouseHeaderRow(
                warehouse.Id,
                warehouse.Name.Value,
                warehouse.KeeperUserId,
                warehouse.Keeper.DisplayName,
                warehouse.IsActive,
                warehouse.Address == null ? null : warehouse.Address.City.Name.Value,
                warehouse.Address == null ? null : warehouse.Address.Street,
                warehouse.Phone == null
                    ? null
                    : warehouse.Phone.Number.CountryCode + " " + warehouse.Phone.Number.NationalNumber))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryWarehouseSummaryRow>> GetInventorySummariesAsync(
        IReadOnlyCollection<int> warehouseIds,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Warehouse> query = context.Warehouses.AsNoTracking();
        if (warehouseIds is not null)
            query = query.Where(warehouse => warehouseIds.Contains(warehouse.Id));

        var rows = await query
            .OrderBy(warehouse => warehouse.Name.Value)
            .ThenBy(warehouse => warehouse.Id)
            .Select(warehouse => new
            {
                warehouse.Id,
                Name = warehouse.Name.Value,
                warehouse.Keeper.DisplayName,
                warehouse.IsActive,
                CityName = warehouse.Address == null ? null : warehouse.Address.City.Name.Value,
                Street = warehouse.Address == null ? null : warehouse.Address.Street,
                PhoneNumber = warehouse.Phone == null
                    ? null
                    : warehouse.Phone.Number.CountryCode + " " + warehouse.Phone.Number.NationalNumber,
                StockOnHand = context.StockBalances
                    .Where(balance => balance.WarehouseId == warehouse.Id)
                    .Sum(balance => (int?)balance.StockOnHand) ?? 0,
                ReservedOnHand = context.StockBalances
                    .Where(balance => balance.WarehouseId == warehouse.Id)
                    .Sum(balance => (int?)balance.ReservedOnHand) ?? 0,
                DamagedQty = context.StockBalances
                    .Where(balance => balance.WarehouseId == warehouse.Id)
                    .Sum(balance => (int?)balance.DamagedQty) ?? 0,
                ProductCount = context.StockBalances.Count(balance => balance.WarehouseId == warehouse.Id),
                WithStockCount = context.StockBalances.Count(balance =>
                    balance.WarehouseId == warehouse.Id && balance.StockOnHand > 0),
                LastMovementAtUtc = context.InventoryMovements
                    .Where(movement => movement.WarehouseId == warehouse.Id)
                    .Max(movement => (DateTime?)movement.EffectiveAtUtc)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new InventoryWarehouseSummaryRow(
                row.Id,
                row.Name,
                row.DisplayName,
                row.IsActive,
                row.CityName,
                row.Street,
                row.PhoneNumber,
                row.StockOnHand,
                row.ReservedOnHand,
                row.DamagedQty,
                row.ProductCount,
                row.WithStockCount,
                row.LastMovementAtUtc))
            .ToList();
    }

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
                warehouse.Keeper.DisplayName,
                warehouse.IsActive,
                warehouse.Address == null ? (int?)null : warehouse.Address.City.Id,
                warehouse.Address == null ? string.Empty : warehouse.Address.Street,
                warehouse.Phone == null ? string.Empty : warehouse.Phone.Number.NationalNumber,
                warehouse.Phone == null ? string.Empty : warehouse.Phone.Number.CountryCode))
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

    public void RemovePhone(Phone phone) => context.Phones.Remove(phone);

    public void Add(Warehouse warehouse) => context.Warehouses.Add(warehouse);
}
