using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.CategoryCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Categories;

internal class CreateCategoryCommandHandler(AppDbContextFactory appDbContextFactory)
    : IRequestHandler<CreateCategoryCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        bool isCategoryExists = await appDbContext.Categories
            .Where(c =>
                c.Name.Value == request.CategoryCreateDto.Name &&
                c.ParentCategoryId == request.CategoryCreateDto.ParentId)
            .AnyAsync(cancellationToken);

        if (isCategoryExists)
        {
            return Result.Failure("هذا القسم موجود بالفعل.");
        }

        Category category = new(request.CategoryCreateDto.Name)
        {
            ParentCategoryId = request.CategoryCreateDto.ParentId
        };

        appDbContext.Categories.Add(category);

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(request.CategoryCreateDto.Name);
        }
    }
}