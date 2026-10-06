using System;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;

namespace OrderHub.Tests.Domain;

public class ProductFeatureDomainTests
{
    [Fact]
    public void Basic_data_normalizes_and_zero_price_is_valid()
    {
        var product = new Product(" Product ", " abc ", new Category("Category"), Money.Zero);
        Assert.Equal("Product", product.Name.Value);
        Assert.Equal("ABC", product.Code);
        product.UpdateDetails(" Renamed ", " new ", new Category("Other"), new Money(12));
        Assert.Equal("Renamed", product.Name.Value);
        Assert.Equal("NEW", product.Code);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("code")]
    [InlineData("category")]
    [InlineData("price")]
    public void Invalid_update_preserves_all_basic_data(string field)
    {
        var category = new Category("Category");
        var product = new Product("Product", "ABC", category, new Money(12));
        Assert.ThrowsAny<ArgumentException>(() => product.UpdateDetails(
            field == "name" ? " x " : "Renamed",
            field == "code" ? " " : "NEW",
            field == "category" ? null : new Category("Other"),
            field == "price" ? null : new Money(20)));
        Assert.Equal("Product", product.Name.Value);
        Assert.Equal("ABC", product.Code);
        Assert.Equal(12, product.Price.Value);
        Assert.Same(category, product.Category);
    }

    [Fact]
    public void Property_assignment_is_unique_required_and_retained_until_removed()
    {
        var product = new Product("Product", "ABC", new Category("Category"), Money.Zero);
        product.AddProperty(1, false);
        var assignment = Assert.Single(product.Properties);
        product.AddProperty(1, true);
        product.ChangePropertyRquirement(1, true);
        Assert.Same(assignment, Assert.Single(product.Properties));
        Assert.True(assignment.IsRequired);
        Assert.Throws<ArgumentOutOfRangeException>(() => product.AddProperty(0, true));
        product.RemoveProperty(1);
        Assert.Empty(product.Properties);
        Assert.Throws<ArgumentException>(() => new Money(-1));
    }
}
