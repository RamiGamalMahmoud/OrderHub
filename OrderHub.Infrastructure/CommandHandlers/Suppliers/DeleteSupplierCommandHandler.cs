using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.SupplierCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Suppliers;

internal class DeleteSupplierCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<DeleteSupplierCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Supplier supplier = await appDbContext.Suppliers.FindAsync(request.Id);
        if (supplier == null)
        {
            return Result.Failure();
        }

        try
        {
            appDbContext.Suppliers.Remove(supplier);
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch(DbUpdateException)
        {
            return Result.Failure();
        }
    }
}
