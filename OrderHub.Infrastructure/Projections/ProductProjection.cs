using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Models;
using System;
using System.Linq.Expressions;

namespace OrderHub.Infrastructure.Projections;

internal static class ProductProjection
{
    public static Expression<Func<Product, ProductLookup>> Lookup => p => new ProductLookup(
        p.Id,
        p.Name.Value,
        new CategoryLookup(
            p.Category.Id, 
            p.Category.Name.Value, 
            p.Category.FullPath, 
            p.Category.SubCategories.Count > 0, 
            p.Category.ParentCategoryId));
}
