# Inventory — PostgreSQL Lock Proof (D13)

**Date:** 2026-11-13 · **Environment:** PostgreSQL 18.6 on `localhost:5432`, scratch database `orderhub-lock-proof` (created for the proof, dropped afterwards) · **Scope:** the D13 ordered-row-lock protocol on `stock_balances` and the concurrent final-save reservation path.

The suite itself runs on SQLite (`FOR UPDATE` is a no-op there), so this proof is the manual PostgreSQL evidence required by the release checklist. It has two parts: a deterministic raw-session lock demonstration, and the real application path (`CreateOrderCommandHandler`) under concurrency.

## 1. Raw row-lock proof

Two psql sessions against a table with one `(warehouse_id, product_id)` balance row:

```sql
-- session A
BEGIN;
SELECT id, stock_on_hand, reserved_on_hand, version
FROM stock_balances WHERE warehouse_id = 1 AND product_id = 1 FOR UPDATE;
-- keep the transaction open

-- session B (run while A is still open)
BEGIN;
SELECT id, stock_on_hand, reserved_on_hand, version
FROM stock_balances WHERE warehouse_id = 1 AND product_id = 1 FOR UPDATE;
-- blocks here
```

```sql
-- session A
COMMIT;   -- B unblocks and returns the row
-- session B
COMMIT;
```

**Observed (2026-11-13, reproduced by the harness below):**

```
raw session A holds FOR UPDATE on the balance row
raw session B blocked while A held the lock: True
raw session B acquired the lock after A committed
```

This is the exact statement `InventoryRepository.LockBalancesAsync` issues (`SELECT * FROM stock_balances WHERE warehouse_id = {w} AND product_id = {p} FOR UPDATE`, ordered ascending by `(product_id, warehouse_id)`). Every write path that touches `StockBalance` calls it inside its transaction before reserving/releasing/issuing/adjusting: create order, update/approval apply, lifecycle release, issue batches, deliveries, returns, pending-return settlement, count approval, and rebuild.

## 2. Application-level concurrent final save

Two `CreateOrderCommand`s for 4 units each against a balance of 5, each on its own `AppDbContext` and connection, released simultaneously by a `Barrier`. The real handler runs: load warehouses → `BeginTransaction` → `LockBalancesAsync` (D13 ordered locks) → guarded `TryReserveAsync` (`StockOnHand − ReservedOnHand >= qty`) → persist.

Method: a temporary xunit harness in `OrderHub.Tests` (`PgLockProofTests`), executed with `ORDERHUB_PG_LOCK_PROOF=1`; the harness migrated the scratch database, seeded city/user/client/category/product/warehouse/balance (`StockOnHand = 5`), ran the raw-lock check, then the two concurrent commands. It was removed after the run; its full source is in §4.

**Observed output (file `%TEMP%\orderhub-pg-lock-proof.txt`):**

```
run at 2026-11-13T14:56:11.4773185Z
seeded client=1 product=1 warehouse=1 stock=5
raw session A holds FOR UPDATE on the balance row
raw session B blocked while A held the lock: True
raw session B acquired the lock after A committed
request A: success=True code=- message=-
request B: success=False code=Conflict message=الكمية لم تعد متاحة في مخزن القفل؛ أعد التوزيع.
pg_locks showed an ungranted stock_balances lock: False
stored balance after both requests: stock=5 reserved=4 version=2
orders persisted: 1
```

**Result:** exactly one winner and one `"Conflict"` with the Arabic re-allocation message; the stored balance ends with `ReservedOnHand = 4` (single reservation), `Version = 2`, and only one order row. The `Assert` set in the harness (`Single(success)`, `Conflict`, `StockOnHand = 5`, `ReservedOnHand = 4`, one order) passed.

**Note on the `pg_locks` sample:** a 1 ms poller never observed an ungranted `stock_balances` lock — the losing transaction reaches `FOR UPDATE` after the winner's very short commit window, so it conflicts on the guarded update rather than waiting. The blocking behavior itself is demonstrated deterministically by the raw-session test in §1.

## 3. Command used

```powershell
createdb -h localhost -U postgres orderhub-lock-proof
$env:ORDERHUB_PG_LOCK_PROOF = "1"
dotnet test OrderHub.Tests -c Debug --filter "FullyQualifiedName~PgLockProofTests"
# -> Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 13 s
dropdb -h localhost -U postgres orderhub-lock-proof
```

## 4. Harness source (removed after the proof)

```csharp
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Create;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.OrderDrafts;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderHub.Tests;

public class PgLockProofTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=orderhub-lock-proof;Username=postgres;Password=12015";

    [Fact]
    public async Task Concurrent_final_saves_produce_one_winner_and_conflict_the_loser()
    {
        if (Environment.GetEnvironmentVariable("ORDERHUB_PG_LOCK_PROOF") != "1")
            return;

        var evidence = new StringBuilder();
        evidence.AppendLine($"run at {DateTime.UtcNow:O}");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        int clientId;
        int productId;
        int warehouseId;
        int userId;

        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.MigrateAsync();

            var city = new City("الرياض");
            var user = User.Create("lock-sales", "hash", "مبيعات القفل");
            setup.AddRange(city, user);
            await setup.SaveChangesAsync();

            var client = new Client("عميل القفل", "شارع التحلية", city, "0501111111", "+966");
            var category = new Category("مستلزمات");
            var product = new Product("قفاز طبي", "GLV-001", category, new Money(10m));
            var warehouse = new Warehouse("مخزن القفل", user.Id, city, "شارع الملك فهد");
            setup.AddRange(client, category, product, warehouse);
            await setup.SaveChangesAsync();

            var balance = StockBalance.Create(warehouse.Id, product.Id);
            balance.ApplyIn(5);
            setup.StockBalances.Add(balance);
            await setup.SaveChangesAsync();

            clientId = client.Id;
            productId = product.Id;
            warehouseId = warehouse.Id;
            userId = user.Id;
        }

        evidence.AppendLine($"seeded client={clientId} product={productId} warehouse={warehouseId} stock=5");

        await using (var rawA = new NpgsqlConnection(ConnectionString))
        await using (var rawB = new NpgsqlConnection(ConnectionString))
        {
            await rawA.OpenAsync();
            await rawB.OpenAsync();

            await Execute(rawA, "BEGIN");
            await Execute(rawA,
                $"SELECT * FROM stock_balances WHERE warehouse_id = {warehouseId} AND product_id = {productId} FOR UPDATE");
            evidence.AppendLine("raw session A holds FOR UPDATE on the balance row");

            await Execute(rawB, "BEGIN");
            var waiting = Execute(rawB,
                $"SELECT * FROM stock_balances WHERE warehouse_id = {warehouseId} AND product_id = {productId} FOR UPDATE");
            await Task.Delay(300);
            evidence.AppendLine($"raw session B blocked while A held the lock: {!waiting.IsCompleted}");
            await Execute(rawA, "COMMIT");
            await waiting;
            evidence.AppendLine("raw session B acquired the lock after A committed");
            await Execute(rawB, "COMMIT");
        }

        var user1 = new TestCurrentUser(userId, "lock-sales", "مبيعات القفل");

        await using var contextA = new AppDbContext(options);
        await using var contextB = new AppDbContext(options);
        var handlerA = HandlerFor(contextA, user1);
        var handlerB = HandlerFor(contextB, user1);

        CreateOrderCommand Command() => new(
            null,
            new OrderInput.Order(
                clientId,
                OrderSource.Online,
                DeliveryMethod.Pickup,
                null,
                null,
                new[]
                {
                    new OrderInput.Item(
                        productId,
                        "قفاز طبي",
                        4,
                        10m,
                        0m,
                        null,
                        null,
                        Array.Empty<OrderInput.Property>(),
                        null,
                        new[] { new OrderInput.Allocation(AllocationSourceKind.Warehouse, 4, warehouseId) })
                },
                Array.Empty<OrderInput.DeliveryStep>(),
                null),
            Guid.NewGuid());

        var barrier = new Barrier(2);
        var stopSampling = false;
        var waiterObserved = false;

        var sampler = Task.Run(async () =>
        {
            await using var monitor = new NpgsqlConnection(ConnectionString);
            await monitor.OpenAsync();
            while (!Volatile.Read(ref stopSampling))
            {
                await using var command = new NpgsqlCommand(
                    "select count(*) from pg_locks where relation = 'stock_balances'::regclass and not granted",
                    monitor);
                if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
                    waiterObserved = true;
                await Task.Delay(1);
            }
        });

        async Task<(Result<int> Result, long StartedMs, long FinishedMs)> Run(CreateOrderCommandHandler handler)
        {
            var started = Stopwatch.GetTimestamp();
            barrier.SignalAndWait();
            var result = await handler.Handle(Command(), CancellationToken.None);
            return (result, started, Stopwatch.GetTimestamp());
        }

        var taskA = Task.Run(() => Run(handlerA));
        var taskB = Task.Run(() => Run(handlerB));
        var outcomes = await Task.WhenAll(taskA, taskB);
        Volatile.Write(ref stopSampling, true);
        await sampler;

        var first = outcomes[0];
        var second = outcomes[1];
        evidence.AppendLine($"request A: success={first.Result.IsSuccess} code={first.Result.ErrorCode ?? "-"} message={first.Result.ErrorMessage ?? "-"}");
        evidence.AppendLine($"request B: success={second.Result.IsSuccess} code={second.Result.ErrorCode ?? "-"} message={second.Result.ErrorMessage ?? "-"}");
        evidence.AppendLine($"pg_locks showed an ungranted stock_balances lock: {waiterObserved}");

        Assert.Single(outcomes.Where(outcome => outcome.Result.IsSuccess));
        var loser = outcomes.Single(outcome => !outcome.Result.IsSuccess).Result;
        Assert.Equal("Conflict", loser.ErrorCode);
        Assert.Contains("لم تعد متاحة", loser.ErrorMessage);

        await using var verify = new AppDbContext(options);
        var stored = await verify.StockBalances.AsNoTracking()
            .SingleAsync(row => row.WarehouseId == warehouseId && row.ProductId == productId);
        evidence.AppendLine($"stored balance after both requests: stock={stored.StockOnHand} reserved={stored.ReservedOnHand} version={stored.Version}");
        Assert.Equal(5, stored.StockOnHand);
        Assert.Equal(4, stored.ReservedOnHand);

        var orders = await verify.Orders.AsNoTracking().CountAsync();
        evidence.AppendLine($"orders persisted: {orders}");
        Assert.Equal(1, orders);

        var target = Environment.GetEnvironmentVariable("ORDERHUB_PG_LOCK_PROOF_FILE")
            ?? Path.Combine(Path.GetTempPath(), "orderhub-pg-lock-proof.txt");
        File.WriteAllText(target, evidence.ToString());
    }

    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static CreateOrderCommandHandler HandlerFor(AppDbContext context, TestCurrentUser user) =>
        new(
            new OrderRepository(context),
            new UnitOfWork(context, new FakeNumberSequenceService()),
            new QuotationRepository(context),
            new ProformaInvoiceRepository(context),
            user,
            new DraftRepository(context),
            new FakeNumberSequenceService(),
            new InventoryRepository(context),
            new WarehouseRepository(context));
}
```
