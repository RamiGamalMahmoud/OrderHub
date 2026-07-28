using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Stores;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests.Infrastructure.Stores;

public class ProductStoreTests
{
    [Fact]
    public async Task Should_Add_Properties()
    {
        // Arrange
        AppDbContextFactory factory = TestDb.CreateFactory();

        PropertyStore propertyStore = new PropertyStore(factory);
        ProductStore store = new ProductStore(factory);

        using AppDbContext db = factory.CreateDbContext();

        List<Property> properties =
        [
            Property.Create("Prop 1", OrderHub.Domain.Enums.PropertyType.Text),
            Property.Create("Prop 2", OrderHub.Domain.Enums.PropertyType.Text),
            Property.Create("Prop 3", OrderHub.Domain.Enums.PropertyType.Text),
        ];

        foreach (var property in properties)
        {
            await propertyStore.CreateAsync(property);
        }

        var category = db.Categories.Add(new OrderHub.Domain.Models.Category("Category", null));
        await db.SaveChangesAsync();

        List<CreateProduct.ProductPropertiesDto> productPropertiesDto =
            properties
            .Select(p => new CreateProduct.ProductPropertiesDto(p.Id, true))
            .ToList();

        CreateProduct.ProductDto productDto = new(
            "Product",
            "000",
            2,
            category.Entity.Id,
            [],
            productPropertiesDto);

        // Act
        int id = (await store.CreateAsync(productDto)).Value;

        Product product = await db.Products
            .Where(p => p.Id == id)
            .Include(p => p.Properties)
            .SingleAsync();

        // Assert
        product.Properties.Count.Should().Be(3);
    }

    [Fact]
    public async Task Should_Update_Properties()
    {
        // Arrange
        AppDbContextFactory factory = TestDb.CreateFactory();

        PropertyStore propertyStore = new PropertyStore(factory);
        ProductStore store = new ProductStore(factory);

        using AppDbContext db = factory.CreateDbContext();

        List<Property> productProperties =
        [
            Property.Create("Prop 1", OrderHub.Domain.Enums.PropertyType.Text),
            Property.Create("Prop 2", OrderHub.Domain.Enums.PropertyType.Text),
            Property.Create("Prop 3", OrderHub.Domain.Enums.PropertyType.Text),
        ];

        foreach (var property in productProperties)
        {
            await propertyStore.CreateAsync(property);
        }

        List<Property> newProperties =
        [
            Property.Create("Prop 4", OrderHub.Domain.Enums.PropertyType.Text),
            Property.Create("Prop 5", OrderHub.Domain.Enums.PropertyType.Text),
            Property.Create("Prop 6", OrderHub.Domain.Enums.PropertyType.Text),
        ];

        foreach (var property in newProperties)
        {
            await propertyStore.CreateAsync(property);
        }

        var category = db.Categories.Add(new OrderHub.Domain.Models.Category("Category", null));
        await db.SaveChangesAsync();

        List<CreateProduct.ProductPropertiesDto> productPropertiesDto =
            productProperties
            .Select(p => new CreateProduct.ProductPropertiesDto(p.Id, true))
            .ToList();

        CreateProduct.ProductDto productDto = new(
            "Product",
            "000",
            2,
            category.Entity.Id,
            [],
            productPropertiesDto);

        // Act
        int id = (await store.CreateAsync(productDto)).Value;
        var createdProduct = await store.GetProductAsync(id);

        createdProduct.ProductProperties.Should().HaveCount(3);

        var productForEdit = await store.GetProductAsync(id);

        List<UpdateProduct.ProductPropertiesDto> updateProductProperties = 
            [
                new(productProperties[0].Id, true),
                new(productProperties[2].Id, true),
                new(newProperties[0].Id, true),
                new(newProperties[1].Id, true),
            ];
        UpdateProduct.ProductDto updateProduct = new(
            productForEdit.Name,
            productForEdit.Code,
            200,
            productForEdit.CategoryId,
            [],
            updateProductProperties);

        await store.UpdateAsync(id, updateProduct);

        var updatedProduct = await store.GetProductAsync(id);

        // Assert
        updatedProduct.ProductProperties.Count().Should().Be(4);
    }
}
