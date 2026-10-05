using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.List;
using OrderHub.Application.Features.Products.Update;

using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Products;

internal sealed class ProductReader(AppDbContextFactory contextFactory) : IProductReader
{
    public async Task<IEnumerable<ProductListItem>> GetListProductsAsync(CancellationToken cancellationToken = default)
    {
        using (AppDbContext dbContext = contextFactory.CreateDbContext())
        {
            return await dbContext
                .Products
                .AsNoTracking()
                .Select(p => new ProductListItem(
                    p.Id,
                    p.Name.Value,
                    p.Price.Value,
                    p.Code,
                    p.Category.Name.Value,
                    p.Suppliers.Select(s => new ProductSupplierItem(s.Id, s.Name.Value)).ToImmutableList()))
                .ToListAsync(cancellationToken: cancellationToken);
        }
    }

    public async Task<ProductDetails> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDetails(
                p.Name.Value,
                p.Code,
                p.Price.Value,
                p.Category.Id,
                p.Suppliers.Select(s => s.Id),
                p.Properties.Select(p => new ProductPropertySelection(p.PropertyId, p.IsRequired))))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProductLookupItem>> GetProductByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        string sql = """
            WITH RECURSIVE category_tree AS (
                -- Anchor member: start with root category
                SELECT id, name, parent_category_id, 0 as level
                FROM categories
                WHERE id = @p0

                UNION ALL

                -- Recursive member: get children
                SELECT c.id, c.name, c.parent_category_id, ct.level + 1
                FROM categories c
                JOIN category_tree ct ON c.parent_category_id = ct.id
            )
            SELECT * FROM category_tree
            ORDER BY level, name
            """;

        IEnumerable<int> categories = await appDbContext
            .Categories
            .FromSqlRaw(sql, categoryId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        return await appDbContext.Products
            .AsNoTracking()
            .Where(p => categories.Contains(p.Category.Id))
            .Select(p => new ProductLookupItem(
                p.Id,
                p.Name.Value,
                p.Price.Value,
                p.Code,
                p.Category.Name.Value,
                p.Suppliers.Select(s => new ProductLookupSupplier(s.Id, s.Name.Value)).ToImmutableList()))
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ProductLookupItem>> GetProductsByName(string name, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = contextFactory.CreateDbContext();

        string searchTerm = name.Trim();

        if (string.IsNullOrEmpty(searchTerm)) return Enumerable.Empty<ProductLookupItem>();

        return await dbContext
            .Products
            .AsNoTracking()
            .Where(p => p.Name.Value.Contains(searchTerm) || p.Suppliers.Any(s => s.Name.Value.Contains(searchTerm)))
            .Select(p => new ProductLookupItem(
                p.Id,
                p.Name.Value,
                p.Price.Value,
                p.Code,
                p.Category.Name.Value,
                p.Suppliers.Select(s => new ProductLookupSupplier(s.Id, s.Name.Value)).ToImmutableList()))
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ProductInfo>> GetProductsInfoAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext
            .Products.AsNoTracking()
            .Select(p => new ProductInfo(p.Id, p.Name.Value))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
