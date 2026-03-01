using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.CategoryCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Categories;

internal class UpdateCategoryCommandHandler(AppDbContextFactory appDbContextFactory, ICacheService cacheService) : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;
    private readonly ICacheService _cacheService = cacheService;

    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Category category = await appDbContext.Categories.FindAsync(request.CategoryUpdateDto.Id);
        category.UpdateName(request.CategoryUpdateDto.Name);
        category.ParentCategoryId = request.CategoryUpdateDto.ParentId;

        try
        {
            await appDbContext.SaveChangesAsync();
            
            // Invalidate category cache
            _cacheService.Remove(CacheKeys.AllCategories);
            
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(request.CategoryUpdateDto.Name);
        }
    }
}
