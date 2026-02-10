using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.ShippingCarriersCommands;

namespace OrderHub.Infrastructure.CommandHandlers.ShippingCarriers
{
    internal class CreateShippingCarrierCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<CreateShippingCarrierCommand, Result>
    {
        private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

        public async Task<Result> Handle(CreateShippingCarrierCommand request, CancellationToken cancellationToken)
        {
            using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

            City city = await appDbContext.Cities.FindAsync(request.Dto.CityId);

            if (city is null)
                return Result.Failure("City not found.");

            Result<Phone> phoneResult = Phone.Create(request.Dto.PhoneNumber, request.Dto.CountryCode);
            if (!phoneResult.IsSuccess)
                return Result.Failure(phoneResult.ErrorMessage);

            Result<Address> addressResult = Address.Create(request.Dto.Street, city);
            if(!addressResult.IsSuccess)
                return Result.Failure(addressResult.ErrorMessage);

            ShippingCarrier shippingCarrier = new ShippingCarrier(
                request.Dto.Name,
                request.Dto.ShippingCost,
                phoneResult.Value,
                addressResult.Value
                );

            appDbContext.ShippingCarriers.Add(shippingCarrier);

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
}
