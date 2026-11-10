using System;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Application.Features.Trips.Contracts;

public static class TripPartySnapshotFactory
{
    public static async Task<Result<TripParty>> ResolveAsync(
        TripPartyInput input,
        IClientRepository clients,
        ISupplierRepository suppliers,
        IShippingCarrierRepository carriers,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (input.PartyType)
            {
                case TripPartyType.Client:
                {
                    if (input.PartyId is not > 0)
                    {
                        return Result<TripParty>.Failure("يجب اختيار عميل محفوظ.");
                    }

                    var client = await clients.GetForUpdateAsync(input.PartyId.Value, cancellationToken);
                    if (client is null)
                    {
                        return Result<TripParty>.Failure("العميل غير موجود.");
                    }

                    return TripParty.FromClient(client);
                }

                case TripPartyType.Supplier:
                {
                    if (input.PartyId is not > 0)
                    {
                        return Result<TripParty>.Failure("يجب اختيار مورد محفوظ.");
                    }

                    var supplier = await suppliers.GetForUpdateAsync(input.PartyId.Value, cancellationToken);
                    if (supplier is null)
                    {
                        return Result<TripParty>.Failure("المورد غير موجود.");
                    }

                    return TripParty.FromSupplier(supplier);
                }

                case TripPartyType.ShippingCarrier:
                {
                    if (input.PartyId is not > 0)
                    {
                        return Result<TripParty>.Failure("يجب اختيار شركة شحن محفوظة.");
                    }

                    var carrier = await carriers.GetForUpdateAsync(input.PartyId.Value, cancellationToken);
                    if (carrier is null)
                    {
                        return Result<TripParty>.Failure("شركة الشحن غير موجودة.");
                    }

                    return TripParty.FromShippingCarrier(carrier);
                }

                case TripPartyType.External:
                    return TripParty.External(input.Name, input.Phone, input.Address, input.Location);

                default:
                    return Result<TripParty>.Failure("نوع الطرف غير صالح.");
            }
        }
        catch (InvalidOperationException exception)
        {
            return Result<TripParty>.Failure(exception.Message);
        }
    }
}
