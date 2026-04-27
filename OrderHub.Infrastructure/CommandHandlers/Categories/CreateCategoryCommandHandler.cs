using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.CategoryCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Categories;

internal class CreateCategoryCommandHandler(AppDbContextFactory appDbContextFactory, ICacheService cacheService) : IRequestHandler<CreateCategoryCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;
    private readonly ICacheService _cacheService = cacheService;

    public async Task<Result> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        bool isCategoryExists = await appDbContext.Categories
            .Where(c => c.Name.Value == request.CategoryCreateDto.Name && c.ParentCategoryId == request.CategoryCreateDto.ParentId)
            .AnyAsync(cancellationToken: cancellationToken);
        if (isCategoryExists)
        {
            return Result.Failure("هذا القسم موجود بالفعل.");
        }
        Category category = new Category(request.CategoryCreateDto.Name);
        category.ParentCategoryId = request.CategoryCreateDto.ParentId;

        appDbContext.Categories.Add(category);
        try
        {
            await appDbContext.SaveChangesAsync();
            
            // Invalidate category cache
            _cacheService.Remove(CacheKeys.AllCategories);
            
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(request.CategoryCreateDto.Name);
        }
    }
}
