using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests.Infrastructure;

public sealed class LifecycleRehearsalFactAttribute : FactAttribute
{
    public LifecycleRehearsalFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORDERHUB_REHEARSAL_SOURCE")))
            Skip = "Opt in with ORDERHUB_REHEARSAL_SOURCE and PostgreSQL backup tools.";
    }
}

public class OrderLifecycleReleaseRehearsalTests
{
    [LifecycleRehearsalFact]
    public async Task Real_backup_restores_and_migrates_without_changing_original_business_data()
    {
        var sourceConnection = Environment.GetEnvironmentVariable("ORDERHUB_REHEARSAL_SOURCE")!;
        var tools = Environment.GetEnvironmentVariable("ORDERHUB_POSTGRES_BIN")!;
        var directory = Path.Combine(Path.GetTempPath(), "orderhub-rehearsal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await using var source = new NpgsqlConnection(sourceConnection);
            await source.OpenAsync();
            await using var snapshot = await source.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
            await Execute(source, "SET TRANSACTION READ ONLY");
            var token = (string)(await new NpgsqlCommand("SELECT pg_export_snapshot()", source).ExecuteScalarAsync())!;
            var columns = await Columns(source);
            var before = await Fingerprints(source, columns);
            var hasHistory = columns.ContainsKey("order_status_history");
            var uninitialized = hasHistory ? "NOT EXISTS (SELECT 1 FROM order_status_history h WHERE h.order_id=o.id)" : "TRUE";
            var statusCounts = await Scalar(source, $"SELECT count(*) FROM orders o WHERE order_status='Processing' OR ({uninitialized} AND order_status NOT IN ('Delivered','Cancelled'))");
            var historyCount = await Scalar(source, $"SELECT count(*) + count(*) FILTER (WHERE order_status IN ('Delivered','Cancelled')) FROM orders o WHERE {uninitialized}");
            if (hasHistory) historyCount += await Scalar(source, "SELECT count(*) FROM order_status_history");
            var backup = Path.Combine(directory, "source.dump");
            await RunTool(Path.Combine(tools, "pg_dump.exe"), sourceConnection,
                "--format=custom", "--no-owner", "--no-privileges", "--snapshot=" + token, "--file=" + backup);
            Assert.True(new FileInfo(backup).Length > 0);

            using var clone = new OrderLifecyclePostgresDatabase(createSchema: false);
            await RunTool(Path.Combine(tools, "pg_restore.exe"), clone.ConnectionString,
                "--exit-on-error", "--no-owner", "--no-privileges", backup);
            await using var restored = new NpgsqlConnection(clone.ConnectionString);
            await restored.OpenAsync();
            Assert.Equal(before, await Fingerprints(restored, columns));
            using var db = clone.CreateDbContext();
            var releaseSql = Environment.GetEnvironmentVariable("ORDERHUB_REHEARSAL_SQL");
            if (string.IsNullOrWhiteSpace(releaseSql)) await db.Database.MigrateAsync();
            else await Execute(restored, await File.ReadAllTextAsync(releaseSql));
            Assert.Equal(before, await Fingerprints(restored, columns));
            Assert.Equal(statusCounts, await Scalar(restored, "SELECT count(*) FROM orders WHERE order_status='Processing'"));
            Assert.Equal(historyCount, await Scalar(restored, "SELECT count(*) FROM order_status_history"));
            Assert.Equal(0, await Scalar(restored, "SELECT count(*) FROM orders WHERE order_status='Pending'"));
            Assert.Equal(0, await Scalar(restored, "SELECT count(*) FROM order_status_history WHERE source = 'LegacyMigration' AND changed_by_user_id IS NOT NULL"));
            Assert.Equal(0, await Scalar(restored, "SELECT count(*) FROM order_status_history h JOIN orders o ON o.id=h.order_id WHERE h.new_status='Processing' AND h.changed_at_utc <> o.created_at"));
            await db.Database.MigrateAsync();
            if (!string.IsNullOrWhiteSpace(releaseSql)) await Execute(restored, await File.ReadAllTextAsync(releaseSql));
            Assert.Equal(historyCount, await Scalar(restored, "SELECT count(*) FROM order_status_history"));

            var report = Environment.GetEnvironmentVariable("ORDERHUB_REHEARSAL_REPORT");
            if (!string.IsNullOrWhiteSpace(report))
            {
                var lines = new List<string> { "# تجربة الاستعادة والترحيل الفعلية", "", "تمت القراءة من قاعدة التطوير دون تعديلها، داخل snapshot ثابت للنسخ والمقارنة.", "تمت استعادة pg_dump بصيغة custom إلى قاعدة عشوائية معزولة، ثم تطبيق migrations عليها فقط.", "", "| الجدول | الصفوف | SHA-256 للقيم الأصلية قبل وبعد |", "|---|---:|---|" };
                lines.AddRange(before.Select(row => $"| {row.Table} | {row.Count} | `{row.Hash}` |"));
                lines.AddRange(new[] { "", $"- عدد سجلات تاريخ الطلب بعد الترحيل: {historyCount}.", $"- عدد الطلبات Processing بعد الترحيل: {statusCounts}.", "- لم يتبق Pending في حالة الطلب. كل تاريخ المصدر مرحّل وبدون مستخدم مختلق.", "- Processing يحتفظ بوقت الإنشاء الأصلي؛ إعادة تطبيق migrations لا تضيف تاريخًا مكررًا.", "- المقارنة تشمل كل أعمدة المصدر باستثناء حالة الطلب التي تتغير عمدًا، وتستبعد جدول تتبع migrations.", "- أزيلت القاعدة المعزولة وملف النسخة المؤقت بعد الاختبار. لا تحتوي هذه الأدلة على بيانات العملاء أو كلمات المرور." });
                await File.WriteAllLinesAsync(report, lines);
            }
            await ReviewWebIfRequested(db, clone.ConnectionString, directory);
        }
        finally
        {
            // This directory is created by this test and contains only its temporary backup.
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task ReviewWebIfRequested(OrderHub.Infrastructure.AppDbContext db, string connection, string directory)
    {
        var project = Environment.GetEnvironmentVariable("ORDERHUB_REHEARSAL_WEB_PROJECT");
        var signal = Environment.GetEnvironmentVariable("ORDERHUB_REHEARSAL_UI_SIGNAL");
        if (string.IsNullOrWhiteSpace(project)) return;
        var manualReview = !string.IsNullOrWhiteSpace(signal);
        signal = manualReview ? signal! : Path.Combine(directory, "review");
        var actor = User.Create("LifecycleReview", new HashShervice().Hash("Review-only-2026!"), "مراجع تجربة معزولة", isAdmin: true);
        var city = new City("مدينة التجربة");
        var client = new Client("عميل تجريبي", "عنوان تجريبي", city, "01012345678", "+20");
        var courier = new Deliveryman("مندوب تجريبي", city, "01012345678");
        var product = new Product("منتج تجريبي", "REVIEWONLY", new Category("تصنيف تجريبي"), new Money(10));
        db.AddRange(actor, client, courier, product);
        await db.SaveChangesAsync();
        var order = new Order(client.Id, "REVIEW-ONLY");
        order.UpdateHeader(client.Id, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, null);
        order.AddOrderItem(product.Id, "منتج تجريبي", 10, 1, 0, null, null, []);
        order.AddDeliveryStep(new() { StepOrder = 1, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = courier.Id });
        order.AddDeliveryStep(new() { StepOrder = 2, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = courier.Id });
        order.InitializeHistory(new(actor.Id, actor.DisplayName, order.CreatedAt));
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        if (manualReview)
        {
            var carrierId = await db.ShippingCarriers.Select(carrier => carrier.Id).FirstAsync();
            foreach (var status in Enum.GetValues<OrderStatus>())
            {
                var sample = new Order(client.Id, "REVIEW-" + (int)status);
                var method = status == OrderStatus.Shipped ? DeliveryMethod.ShippingCompany
                    : status == OrderStatus.ReadyForPickup ? DeliveryMethod.Pickup : DeliveryMethod.DeliveryMan;
                sample.UpdateHeader(client.Id, OrderSource.Online, method,
                    method == DeliveryMethod.DeliveryMan ? courier.Id : null,
                    method == DeliveryMethod.ShippingCompany ? carrierId : null, null);
                sample.AddOrderItem(product.Id, "منتج تجريبي", 10, 1, 0, null, null, []);
                Assert.True(sample.InitializeHistory(new(actor.Id, actor.DisplayName, sample.CreatedAt)).IsSuccess);
                if (status is OrderStatus.Delivered or OrderStatus.DeliveryFailed)
                    Assert.True(sample.TransitionTo(OrderStatus.OutForDelivery, new(actor.Id, actor.DisplayName, DateTime.UtcNow)).IsSuccess);
                if (status != OrderStatus.Processing)
                    Assert.True(sample.TransitionTo(status, new(actor.Id, actor.DisplayName, DateTime.UtcNow),
                        status is OrderStatus.Cancelled or OrderStatus.DeliveryFailed ? "سبب تجريبي لمراجعة ألوان الحالات" : null).IsSuccess);
                db.Orders.Add(sample);
            }
            await db.SaveChangesAsync();
        }
        var info = new ProcessStartInfo("dotnet") { WorkingDirectory = project, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        info.ArgumentList.Add(Path.Combine(project, "bin/Debug/net9.0/OrderHub.Web.dll"));
        info.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        info.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:5187";
        info.Environment["ConnectionStrings__DefaultConnection"] = connection;
        info.Environment["ApplicationData__RootPath"] = Path.Combine(directory, "app-data");
        using var app = Process.Start(info)!;
        var errors = app.StandardError.ReadToEndAsync();
        var output = app.StandardOutput.ReadToEndAsync();
        try
        {
            if (File.Exists(signal)) File.Delete(signal);
            using var http = new HttpClient(new HttpClientHandler { UseProxy = false });
            http.BaseAddress = new Uri("http://127.0.0.1:5187");
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 Version/18.1 Mobile/15E148 Safari/604.1");
            for (var attempt = 0; attempt < 80; attempt++)
            {
                try { if ((await http.GetAsync("/health")).IsSuccessStatusCode) break; }
                catch (HttpRequestException) { }
                await Task.Delay(250);
            }
            using var login = await http.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["username"] = "LifecycleReview", ["password"] = "Review-only-2026!", ["rememberMe"] = "false" }));
            login.EnsureSuccessStatusCode();
            Assert.Equal("/", login.RequestMessage!.RequestUri!.AbsolutePath);
            foreach (var route in new[] { $"/orders/{order.Id}/workflow", $"/orders/{order.Id}", "/orders/archived", "/orders" })
            {
                using var page = await http.GetAsync(route);
                page.EnsureSuccessStatusCode();
                Assert.DoesNotContain("/login", page.RequestMessage!.RequestUri!.AbsolutePath);
                var html = await page.Content.ReadAsStringAsync();
                if (route.Contains(order.Id.ToString())) Assert.Contains("REVIEW-ONLY", html);
            }
            if (manualReview)
            {
                await File.WriteAllTextAsync(signal + ".ready", order.Id.ToString());
                var timeout = DateTime.UtcNow.AddMinutes(10);
                while (!File.Exists(signal) && !app.HasExited && DateTime.UtcNow < timeout)
                    await Task.Delay(500);
                Assert.False(app.HasExited, "Review app terminated: " + (app.HasExited ? await errors : ""));
                Assert.True(File.Exists(signal), "Manual browser review timed out.");
            }
        }
        finally
        {
            if (!app.HasExited) app.Kill(entireProcessTree: true);
            await app.WaitForExitAsync();
            await Task.WhenAll(errors, output);
            await File.WriteAllTextAsync(signal + ".log", await output + "\n" + await errors);
            if (File.Exists(signal + ".ready")) File.Delete(signal + ".ready");
        }
    }

    private static async Task Execute(NpgsqlConnection connection, string sql) =>
        await new NpgsqlCommand(sql, connection).ExecuteNonQueryAsync();

    private static async Task<long> Scalar(NpgsqlConnection connection, string sql) =>
        Convert.ToInt64(await new NpgsqlCommand(sql, connection).ExecuteScalarAsync());

    private static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";

    private static async Task<Dictionary<string, List<string>>> Columns(NpgsqlConnection source)
    {
        var result = new Dictionary<string, List<string>>();
        await using var reader = await new NpgsqlCommand("""
            SELECT c.table_name, c.column_name FROM information_schema.columns c
            JOIN information_schema.tables t ON t.table_schema=c.table_schema AND t.table_name=c.table_name
            WHERE c.table_schema='public' AND t.table_type='BASE TABLE' AND c.table_name <> '__EFMigrationsHistory'
            ORDER BY c.table_name, c.ordinal_position
            """, source).ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var table = reader.GetString(0);
            var column = reader.GetString(1);
            if (table == "orders" && column == "order_status") continue;
            if (!result.TryGetValue(table, out var list)) result.Add(table, list = new List<string>());
            list.Add(column);
        }
        return result;
    }

    private sealed record Fingerprint(string Table, long Count, string Hash);

    private static async Task<List<Fingerprint>> Fingerprints(NpgsqlConnection connection, Dictionary<string, List<string>> columns)
    {
        var result = new List<Fingerprint>();
        foreach (var (table, names) in columns)
        {
            var projection = string.Join(",", names.Select(Quote));
            await using var reader = await new NpgsqlCommand($"SELECT row_to_json(r)::text FROM (SELECT {projection} FROM {Quote(table)}) r ORDER BY row_to_json(r)::text COLLATE \"C\"", connection).ExecuteReaderAsync();
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long count = 0;
            while (await reader.ReadAsync())
            {
                hash.AppendData(Encoding.UTF8.GetBytes(reader.GetString(0) + "\n"));
                count++;
            }
            result.Add(new Fingerprint(table, count, Convert.ToHexString(hash.GetHashAndReset())));
        }
        return result;
    }

    private static async Task RunTool(string executable, string connectionString, params string[] arguments)
    {
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        info.Environment["PGPASSWORD"] = connection.Password;
        info.Environment["PGDATABASE"] = connection.Database;
        info.Environment["PGHOST"] = connection.Host;
        info.Environment["PGPORT"] = connection.Port.ToString();
        info.Environment["PGUSER"] = connection.Username;
        if (Path.GetFileName(executable).StartsWith("pg_restore", StringComparison.OrdinalIgnoreCase))
            info.ArgumentList.Add("--dbname=" + connection.Database);
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(error, output);
        Assert.True(process.ExitCode == 0, $"{Path.GetFileName(executable)} failed: {await error}");
    }
}
