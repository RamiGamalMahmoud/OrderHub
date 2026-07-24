using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Infrastructure;

namespace OrderHub.Tests.Infrastructure;

internal static class TestDb
{
    public static AppDbContextFactory CreateFactory()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        return new AppDbContextFactory(options);
    }
}