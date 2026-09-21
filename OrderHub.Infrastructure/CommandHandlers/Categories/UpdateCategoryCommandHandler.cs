using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.CategoryCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Categories;

internal class UpdateCategoryCommandHandler(AppDbContextFactory appDbContextFactory)
    : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Category category = await appDbContext.Categories.FindAsync(
            [request.CategoryUpdateDto.Id],
            cancellationToken);

        if (category is null)
        {
            return Result.Failure("القسم غير موجود.");
        }

        category.UpdateName(request.CategoryUpdateDto.Name);
        category.ParentCategoryId = request.CategoryUpdateDto.ParentId;

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(request.CategoryUpdateDto.Name);
        }
    }
}