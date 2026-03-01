using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;
using static OrderHub.Application.Queries.CategoryQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Categories;

internal class GetCategoryTreeQueryHandler : IRequestHandler<GetCategoryTreeQuery, IEnumerable<CategoryTreeDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory;
    private readonly ICacheService _cacheService;

    public GetCategoryTreeQueryHandler(AppDbContextFactory appDbContextFactory, ICacheService cacheService)
    {
        _appDbContextFactory = appDbContextFactory;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<CategoryTreeDto>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        return await _cacheService.GetOrCreateAsync(
            CacheKeys.AllCategories,
            async () => await FetchCategoryTreeAsync(cancellationToken),
            TimeSpan.FromMinutes(30));
    }

    private async Task<IEnumerable<CategoryTreeDto>> FetchCategoryTreeAsync(CancellationToken cancellationToken)
    {
        await using var appDbContext = _appDbContextFactory.CreateDbContext();

        IEnumerable<CategoryFlatDto> allCategories = await appDbContext
            .Categories
            .AsNoTracking()
            .Select(c => new CategoryFlatDto(
                c.Id,
                c.Name.Value,
                c.SubCategories.Count != 0,
                c.ParentCategoryId))
            .ToListAsync(cancellationToken);

        return BuildCategoryTree(allCategories);
    }

    private static List<CategoryTreeDto> BuildCategoryTree(IEnumerable<CategoryFlatDto> flatCategories)
    {
        ILookup<int?, CategoryFlatDto> lookup = flatCategories.ToLookup(c => c.ParentCategoryId);

        List<CategoryTreeDto> BuildTree(int? parentId, int level)
        {
            return lookup[parentId]
                .Select(child => new CategoryTreeDto(
                    child.Id,
                    child.Name,
                    child.IsRootCategory,
                    BuildTree(child.Id, level + 1)))
                .ToList();
        }

        return BuildTree(null, 0);
    }

    private record CategoryFlatDto(
    int Id,
    string Name,
    bool IsRootCategory,
    int? ParentCategoryId);
}
