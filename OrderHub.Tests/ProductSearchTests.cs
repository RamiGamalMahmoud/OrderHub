using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Products;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class ProductSearchTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly ProductReader _reader;

    public ProductSearchTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _reader = new ProductReader(new AppDbContextFactory(options));

        Seed();
    }

    private void Seed()
    {
        var city = new City("الرياض");
        _context.Cities.Add(city);

        var category = new Category("تصنيف");
        _context.Categories.Add(category);

        var supplier = new Supplier(
            "مورد الجملة",
            null,
            null,
            Address.Create("مستودع الشمال", city).Value,
            Phone.Create("0503333333", "+966").Value);
        _context.Suppliers.Add(supplier);

        var alpha = new Product("Alpha", "PRD-A", category, new Money(10m));
        var beta = new Product("Beta", "PRD-B", category, new Money(20m));
        var gamma = new Product("Gamma", "PRD-C", category, new Money(30m));
        var delta = new Product("Delta", "PRD-D", category, new Money(40m));
        delta.AddSupplier(supplier);

        _context.Products.AddRange(alpha, beta, gamma, delta);
        _context.SaveChanges();
    }

    private void AddProducts(int count, string namePrefix, string codePrefix, Category category)
    {
        for (int index = 1; index <= count; index++)
        {
            _context.Products.Add(new Product(
                $"{namePrefix}{index:D2}",
                $"{codePrefix}-{index:D2}",
                category,
                new Money(5m + index)));
        }

        _context.SaveChanges();
    }

    [Fact]
    public async Task Empty_search_returns_all_products_alphabetically()
    {
        var page = await _reader.GetProductsByName(string.Empty, 1, 10, CancellationToken.None);

        Assert.Equal(4, page.TotalCount);
        Assert.Equal(new[] { "Alpha", "Beta", "Delta", "Gamma" }, page.Items.Select(item => item.Name));
        Assert.True(page.HasNextPage is false);
        Assert.True(page.HasPreviousPage is false);
    }

    [Fact]
    public async Task Search_matches_product_names_and_supplier_names()
    {
        var bySupplier = await _reader.GetProductsByName("مورد الجملة", 1, 10, CancellationToken.None);
        var supplierMatch = Assert.Single(bySupplier.Items);
        Assert.Equal("Delta", supplierMatch.Name);
        Assert.Equal(1, bySupplier.TotalCount);

        var byName = await _reader.GetProductsByName("am", 1, 10, CancellationToken.None);
        var nameMatch = Assert.Single(byName.Items);
        Assert.Equal("Gamma", nameMatch.Name);
    }

    [Fact]
    public async Task Empty_and_typed_search_are_paged_alphabetically()
    {
        var category = _context.Categories.Single();
        AddProducts(9, "Item", "PRT", category);

        var firstPage = await _reader.GetProductsByName(string.Empty, 1, 10, CancellationToken.None);

        Assert.Equal(13, firstPage.TotalCount);
        Assert.Equal(10, firstPage.Items.Count());
        Assert.True(firstPage.HasNextPage);
        Assert.Equal(
            new[] { "Alpha", "Beta", "Delta", "Gamma", "Item01", "Item02", "Item03", "Item04", "Item05", "Item06" },
            firstPage.Items.Select(item => item.Name));

        var secondPage = await _reader.GetProductsByName(string.Empty, 2, 10, CancellationToken.None);

        Assert.Equal(3, secondPage.Items.Count());
        Assert.Equal(new[] { "Item07", "Item08", "Item09" }, secondPage.Items.Select(item => item.Name));
        Assert.True(secondPage.HasNextPage is false);
        Assert.True(secondPage.HasPreviousPage);

        var typedFirstPage = await _reader.GetProductsByName("Item", 1, 4, CancellationToken.None);

        Assert.Equal(9, typedFirstPage.TotalCount);
        Assert.Equal(new[] { "Item01", "Item02", "Item03", "Item04" }, typedFirstPage.Items.Select(item => item.Name));
        Assert.True(typedFirstPage.HasNextPage);

        var typedLastPage = await _reader.GetProductsByName("Item", 3, 4, CancellationToken.None);

        Assert.Equal(new[] { "Item09" }, typedLastPage.Items.Select(item => item.Name));
        Assert.True(typedLastPage.HasNextPage is false);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
