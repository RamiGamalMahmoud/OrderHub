using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.CategoryCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Categories;

internal class DeleteCategoryCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        Category category = await appDbContext.Categories.FindAsync(request.Id);
        appDbContext.Categories.Remove(category);
        try
        {
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure($"لم يتم حذف القسم {category.Name}");
        }
    }
}
