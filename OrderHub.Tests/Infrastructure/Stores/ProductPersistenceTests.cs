using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Application.Features.Setup.Properties.Create;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Infrastructure.Stores;

public class ProductPersistenceTests
{
    [Fact]
    public async Task Should_Add_Properties()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var properties = await CreateProperties(mediator, "Prop");
        using var db = factory.CreateDbContext();
        var category = db.Categories.Add(new Category("Category", null));
        await db.SaveChangesAsync();
        var result = await mediator.Send(new CreateProductCommand(new(
            "Product", "000", 2, category.Entity.Id, [], properties.Select(id => new ProductPropertyInput(id, true)))));
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var product = await db.Products.Include(p => p.Properties).SingleAsync(p => p.Id == result.Value);
        Assert.Equal(3, product.Properties.Count);
        Assert.All(product.Properties, property => Assert.True(property.IsRequired));
    }

    [Fact]
    public async Task Should_Update_Properties()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var properties = await CreateProperties(mediator, "Original");
        var added = await CreateProperties(mediator, "New");
        using var db = factory.CreateDbContext();
        var category = db.Categories.Add(new Category("Category", null));
        await db.SaveChangesAsync();
        var result = await mediator.Send(new CreateProductCommand(new(
            "Product", "000", 2, category.Entity.Id, [], properties.Select(id => new ProductPropertyInput(id, true)))));
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var original = await mediator.Send(new GetProductQuery(result.Value));
        Assert.Equal(3, original.ProductProperties.Count());
        var update = await mediator.Send(new UpdateProductCommand(result.Value, new(
            original.Name, original.Code, 200, original.CategoryId, [],
            [new(properties[0], true), new(properties[2], true), new(added[0], true), new(added[1], true)])));
        Assert.True(update.IsSuccess, update.ErrorMessage);
        var updated = await mediator.Send(new GetProductQuery(result.Value));
        Assert.Equal(4, updated.ProductProperties.Count());
        Assert.Equal(200, updated.Price);
        Assert.DoesNotContain(updated.ProductProperties, p => p.PropertyId == properties[1]);
        Assert.Contains(updated.ProductProperties, p => p.PropertyId == added[0]);
    }

    private static async Task<int[]> CreateProperties(IMediator mediator, string prefix)
    {
        var ids = new int[3];
        for (var index = 0; index < ids.Length; index++)
            ids[index] = await mediator.Send(new CreatePropertyCommand($"{prefix} {index}", PropertyType.Text, "", []));
        return ids;
    }
}
