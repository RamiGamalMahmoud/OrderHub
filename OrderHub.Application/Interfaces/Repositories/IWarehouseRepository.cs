using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IWarehouseRepository
{
    Task<Warehouse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the single warehouse the user keeps, or null when the user keeps none.</summary>
    Task<Warehouse> GetByKeeperAsync(
        int keeperUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseListItem>> GetListAsync(
        CancellationToken cancellationToken = default);

    Task<WarehouseEdit> GetForEditAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive name check, optionally excluding the warehouse being updated.</summary>
    Task<bool> NameExistsAsync(
        string name,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    /// <summary>True when the user currently keeps another warehouse (keeper uniqueness).</summary>
    Task<bool> IsKeeperOfAnotherWarehouseAsync(
        int keeperUserId,
        int? excludingWarehouseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>True when the warehouse carries positive stock or damaged quantities.</summary>
    Task<bool> HasPositiveStockAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a phone row owned exclusively by the warehouse when the contact is cleared.</summary>
    void RemovePhone(Phone phone);

    void Add(Warehouse warehouse);
}
