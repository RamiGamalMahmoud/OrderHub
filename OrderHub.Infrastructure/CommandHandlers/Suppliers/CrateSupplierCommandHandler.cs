using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.SupplierCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Suppliers;

internal class CrateSupplierCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<CreateSupplierCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        City city = await appDbContext.Cities.FindAsync(request.Dto.CityId);

        if (city is null)
            return Result.Failure("City not found.");

        Result<Phone> phoneResult = Phone.Create(request.Dto.PhoneNumber, request.Dto.CountryCode);
        if (!phoneResult.IsSuccess)
            return Result.Failure(phoneResult.ErrorMessage);

        Result<Address> addressResult = Address.Create(request.Dto.Street, city);
        if (!addressResult.IsSuccess)
            return Result.Failure(addressResult.ErrorMessage);

        Supplier supplier = new Supplier(
            request.Dto.Name, 
            request.Dto.OpenAt, 
            request.Dto.CloseAt,
            addressResult.Value,
            phoneResult.Value
            );

        appDbContext.Suppliers.Add(supplier);

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure();
        }

    }
}
