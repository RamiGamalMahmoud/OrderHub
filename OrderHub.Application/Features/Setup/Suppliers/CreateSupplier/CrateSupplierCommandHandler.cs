using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class CrateSupplierCommandHandler(ISupplierRepository repository, IUnitOfWork unitOfWork, ICityRepository cities) : IRequestHandler<CreateSupplierCommand, Result>
{
    public async Task<Result> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {

        City city = await cities.FindAsync(request.Dto.CityId, cancellationToken);

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

        repository.Add(supplier);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure();
        }

    }
}
