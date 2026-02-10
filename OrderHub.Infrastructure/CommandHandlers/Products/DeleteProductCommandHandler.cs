using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.ProductCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Products;

internal class DeleteProductCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        Product product = await appDbContext.Products.FindAsync(request.Id);

        if (product is null)
            return Result.Failure();
        appDbContext.Products.Remove(product);

        try
        {
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch(DbUpdateException)
        {
            return Result.Failure();
        }
    }
}
