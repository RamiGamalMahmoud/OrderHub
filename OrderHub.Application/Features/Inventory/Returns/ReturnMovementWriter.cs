using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Returns;

/// <summary>Applies a saved return to the balances and writes one movement per physical split.</summary>
internal static class ReturnMovementWriter
{
    public static async Task<Result> ApplyAsync(
        InventoryReturn record,
        IReadOnlyCollection<InventoryReturnLine> lines,
        IInventoryRepository inventory,
        int userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var balances = new Dictionary<int, StockBalance>();

        foreach (InventoryReturnLine line in lines)
        {
            if (!balances.TryGetValue(line.ProductId, out StockBalance balance))
            {
                balance = await inventory.GetOrCreateStockItemAsync(
                    record.WarehouseId,
                    line.ProductId,
                    cancellationToken);
                balances[line.ProductId] = balance;
            }

            if (line.IntactQty > 0)
            {
                balance.ApplyIn(line.IntactQty);
                inventory.Add(InventoryMovement.Create(
                    record.WarehouseId,
                    line.ProductId,
                    line.IntactQty,
                    InventoryDirection.In,
                    InventoryMovementType.ReturnIn,
                    now,
                    userId,
                    note: record.Note,
                    sourceReturnId: record.Id,
                    nowUtc: now));
            }

            if (line.DamagedQty > 0)
            {
                balance.DamagedIn(line.DamagedQty);
                inventory.Add(InventoryMovement.Create(
                    record.WarehouseId,
                    line.ProductId,
                    line.DamagedQty,
                    InventoryDirection.In,
                    InventoryMovementType.ReturnDamagedIn,
                    now,
                    userId,
                    note: record.Note,
                    sourceReturnId: record.Id,
                    nowUtc: now));
            }
        }

        return Result.Success();
    }
}
