using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.CategoryCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Categories;

internal class CreateCategoryCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<CreateCategoryCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        Category category = new Category(request.CategoryCreateDto.Name);
        category.ParentCategoryId = request.CategoryCreateDto.ParentId;

        appDbContext.Categories.Add(category);
        try
        {
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(request.CategoryCreateDto.Name);
        }
    }
}
