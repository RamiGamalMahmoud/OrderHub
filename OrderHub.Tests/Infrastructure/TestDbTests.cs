using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Infrastructure;

public class TestDbTests
{
    [Fact]
    public async Task Contexts_share_fixture_data_but_independent_fixtures_are_isolated()
    {
        using var first = TestDb.CreateFactory();
        using var second = TestDb.CreateFactory();
        using (var writer = first.CreateDbContext())
        {
            writer.Cities.Add(new City("Fixture city"));
            await writer.SaveChangesAsync();
        }
        using var reader = first.CreateDbContext();
        using var other = second.CreateDbContext();
        Assert.Equal("Fixture city", (await reader.Cities.SingleAsync()).Name.Value);
        Assert.Empty(await other.Cities.ToListAsync());
    }

    [Fact]
    public void Disposed_fixture_cannot_create_another_context()
    {
        var factory = TestDb.CreateFactory();
        factory.Dispose();
        factory.Dispose();
        Assert.Throws<ObjectDisposedException>(() => factory.CreateDbContext());
    }
}
