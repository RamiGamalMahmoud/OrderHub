using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class UpdateSupplierCommandHandler(ISupplierRepository repository, IUnitOfWork unitOfWork, ICityRepository cities) : IRequestHandler<UpdateSupplierCommand, Result>
{
    public async Task<Result> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {

        City city = await cities.FindAsync(request.Dto.CityId, cancellationToken);

        Supplier supplier = await repository.GetForUpdateAsync(request.Dto.Id, cancellationToken);

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
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure();
        }
    }
}
