using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Rebuild.RebuildBalances;

internal sealed class RebuildBalancesCommandHandler(
    IInventoryRebuildRepository rebuilds,
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<RebuildBalancesCommand, Result<RebuildBalancesResult>>
{
    private const int MaxReportedChanges = 100;

    public async Task<Result<RebuildBalancesResult>> Handle(
        RebuildBalancesCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<RebuildBalancesResult>.Failure(
                "يجب تسجيل الدخول لإعادة بناء الأرصدة.",
                "Unauthorized");

        if (!Enum.IsDefined(request.Scope))
            return Result<RebuildBalancesResult>.Failure("نطاق إعادة البناء غير صالح.");

        int? targetWarehouseId = null;
        if (request.Scope == InventoryRebuildScope.SingleWarehouse)
        {
            if (request.WarehouseId is not int warehouseId || warehouseId <= 0)
            {
                return Result<RebuildBalancesResult>.Failure(
                    "اختر المخزن المطلوب إعادة بناء أرصدته.");
            }

            Warehouse warehouse = await warehouses.GetByIdAsync(warehouseId, cancellationToken);
            if (warehouse is null)
                return Result<RebuildBalancesResult>.Failure("المخزن غير موجود.", "NotFound");

            targetWarehouseId = warehouse.Id;
        }
        else if (request.WarehouseId is not null)
        {
            return Result<RebuildBalancesResult>.Failure(
                "نطاق كل المخازن لا يقبل تحديد مخزن واحد.");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        var transactionActive = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            IReadOnlyList<StockBalance> balances = await rebuilds.LockBalancesForRebuildAsync(
                targetWarehouseId,
                cancellationToken);
            IReadOnlyList<LedgerBalanceRow> ledgerRows = await rebuilds.GetLedgerBalancesAsync(
                targetWarehouseId,
                cancellationToken);
            IReadOnlyList<ReservedBalanceRow> reservedRows = await rebuilds.GetReservedBalancesAsync(
                targetWarehouseId,
                cancellationToken);

            var ledger = ledgerRows.ToDictionary(row => (row.WarehouseId, row.ProductId));
            var reserved = reservedRows.ToDictionary(
                row => (row.WarehouseId, row.ProductId),
                row => row.ReservedQty);
            var byPair = balances.ToDictionary(balance => (balance.WarehouseId, balance.ProductId));

            var pairs = new HashSet<(int WarehouseId, int ProductId)>();
            foreach (StockBalance balance in balances)
                pairs.Add((balance.WarehouseId, balance.ProductId));
            foreach (LedgerBalanceRow row in ledgerRows)
                pairs.Add((row.WarehouseId, row.ProductId));
            foreach (ReservedBalanceRow row in reservedRows)
                pairs.Add((row.WarehouseId, row.ProductId));

            var targets = new List<RebalanceTarget>(pairs.Count);
            foreach (var pair in pairs.OrderBy(key => key.WarehouseId).ThenBy(key => key.ProductId))
            {
                (int sound, int damaged) = ledger.TryGetValue(pair, out LedgerBalanceRow derived)
                    ? (derived.SoundQty, derived.DamagedQty)
                    : (0, 0);
                int reservedQty = reserved.TryGetValue(pair, out int stored) ? stored : 0;

                if (sound < 0 || damaged < 0 || reservedQty < 0 || reservedQty > sound)
                {
                    string productName = await ResolveProductNameAsync(
                        rebuilds,
                        pair.ProductId,
                        cancellationToken);
                    return Result<RebuildBalancesResult>.Failure(
                        $"تعذر إعادة البناء: رصيد الصنف «{productName}» غير متسق " +
                        $"(الرصيد المشتق {sound.ToString(CultureInfo.InvariantCulture)} " +
                        $"والمحجوز {reservedQty.ToString(CultureInfo.InvariantCulture)}). " +
                        "راجع حركات المخزن قبل إعادة المحاولة.",
                        "Conflict");
                }

                targets.Add(new RebalanceTarget(
                    pair.WarehouseId,
                    pair.ProductId,
                    sound,
                    reservedQty,
                    damaged));
            }

            var changes = new List<RebalanceChange>();
            foreach (RebalanceTarget target in targets)
            {
                if (byPair.TryGetValue((target.WarehouseId, target.ProductId), out StockBalance balance))
                {
                    int beforeStock = balance.StockOnHand;
                    int beforeReserved = balance.ReservedOnHand;
                    int beforeDamaged = balance.DamagedQty;

                    if (balance.Rebuild(target.StockOnHand, target.ReservedOnHand, target.DamagedQty))
                    {
                        changes.Add(new RebalanceChange(
                            target,
                            beforeStock,
                            beforeReserved,
                            beforeDamaged));
                    }

                    continue;
                }

                if (target.StockOnHand == 0 && target.ReservedOnHand == 0 && target.DamagedQty == 0)
                    continue;

                StockBalance created = StockBalance.Create(target.WarehouseId, target.ProductId);
                rebuilds.AddBalance(created);
                created.Rebuild(target.StockOnHand, target.ReservedOnHand, target.DamagedQty);
                changes.Add(new RebalanceChange(target, 0, 0, 0));
            }

            List<RebalanceChange> reported = changes.Take(MaxReportedChanges).ToList();
            IReadOnlyDictionary<int, string> names = await rebuilds.GetProductNamesAsync(
                reported.Select(change => change.Target.ProductId).Distinct().ToList(),
                cancellationToken);

            Result<InventoryRebuildLog> creation = InventoryRebuildLog.Create(
                request.Scope,
                targetWarehouseId,
                userId,
                targets.Count,
                changes.Count,
                now);
            if (!creation.IsSuccess)
                return Result<RebuildBalancesResult>.Failure(creation.ErrorMessage, creation.ErrorCode);

            InventoryRebuildLog log = creation.Value;
            foreach (RebalanceChange change in changes)
            {
                Result<InventoryRebuildLogLine> added = log.AddLine(
                    change.Target.WarehouseId,
                    change.Target.ProductId,
                    change.BeforeStockOnHand,
                    change.BeforeReservedOnHand,
                    change.BeforeDamagedQty,
                    change.Target.StockOnHand,
                    change.Target.ReservedOnHand,
                    change.Target.DamagedQty,
                    now);
                if (!added.IsSuccess)
                {
                    return Result<RebuildBalancesResult>.Failure(
                        added.ErrorMessage,
                        added.ErrorCode);
                }
            }

            rebuilds.AddLog(log);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            transactionActive = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result<RebuildBalancesResult>.Success(
                new RebuildBalancesResult(
                    log.Id,
                    targets.Count,
                    changes.Count,
                    reported
                        .Select(change => new RebuildChangedPair(
                            change.Target.WarehouseId,
                            change.Target.ProductId,
                            names.TryGetValue(change.Target.ProductId, out string name)
                                ? name
                                : change.Target.ProductId.ToString(CultureInfo.InvariantCulture),
                            change.BeforeStockOnHand,
                            change.BeforeReservedOnHand,
                            change.BeforeDamagedQty,
                            change.Target.StockOnHand,
                            change.Target.ReservedOnHand,
                            change.Target.DamagedQty))
                        .ToList()));
        }
        catch (PersistenceConflictException)
        {
            return Result<RebuildBalancesResult>.Failure(
                "تغيّرت الأرصدة أثناء إعادة البناء؛ أعد المحاولة.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<RebuildBalancesResult>.Failure(
                "تعذر حفظ إعادة بناء الأرصدة. يرجى المحاولة مرة أخرى.",
                "Persistence");
        }
        catch (ArgumentException exception)
        {
            return Result<RebuildBalancesResult>.Failure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<RebuildBalancesResult>.Failure(exception.Message, "Conflict");
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }

    private static async Task<string> ResolveProductNameAsync(
        IInventoryRebuildRepository rebuilds,
        int productId,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<int, string> names = await rebuilds.GetProductNamesAsync(
            [productId],
            cancellationToken);
        return names.TryGetValue(productId, out string name)
            ? name
            : productId.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record RebalanceTarget(
        int WarehouseId,
        int ProductId,
        int StockOnHand,
        int ReservedOnHand,
        int DamagedQty);

    private sealed record RebalanceChange(
        RebalanceTarget Target,
        int BeforeStockOnHand,
        int BeforeReservedOnHand,
        int BeforeDamagedQty);
}
