using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.SupplierCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Suppliers;

internal class UpdateSupplierCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<UpdateSupplierCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        City city = await appDbContext.Cities.FindAsync(request.Dto.CityId);

        Supplier supplier = await appDbContext
            .Suppliers
            .Include(s => s.Address).ThenInclude(a => a.City)
            .Include(s => s.Phone)
            .Where(s => s.Id == request.Dto.Id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (supplier == null)
        {
            return Result.Failure();
        }

        supplier.UpdateName(request.Dto.Name);
        supplier.Phone.ChangeNumber(request.Dto.PhoneNumber, request.Dto.CountryCode);
        supplier.UpdateBuisnessHours(request.Dto.OpenAt, request.Dto.CloseAt);
        supplier.Address.ChangeCity(city);
        supplier.Address.ChangeStreet(request.Dto.Street);

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch(DbUpdateException)
        {
            return Result.Failure();
        }
    }
}
