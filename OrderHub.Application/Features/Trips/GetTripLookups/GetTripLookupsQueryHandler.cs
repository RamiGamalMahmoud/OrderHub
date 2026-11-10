#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Trips.Contracts;

namespace OrderHub.Application.Features.Trips.GetTripLookups;

internal sealed class GetTripLookupsQueryHandler(
    IDeliverymanReader deliverymanReader,
    IClientReader clientReader,
    ISupplierReader supplierReader,
    IShippingCarrierReader carrierReader)
    : IRequestHandler<GetTripLookupsQuery, IReadOnlyList<TripLookupItem>>
{
    public async Task<IReadOnlyList<TripLookupItem>> Handle(
        GetTripLookupsQuery request,
        CancellationToken cancellationToken)
    {
        int take = Math.Clamp(request.Take, 1, 20);
        string searchTerm = request.SearchTerm?.Trim() ?? "";

        switch (request.Kind)
        {
            case TripLookupKind.Deliveryman:
            {
                var items = await deliverymanReader.GetDeliverymenByNameAsync(
                    searchTerm,
                    take,
                    cancellationToken);

                return items
                    .Select(item => new TripLookupItem(
                        item.Id,
                        item.Name,
                        BuildDetail(item.CityName, item.PhoneNumber)))
                    .ToList();
            }

            case TripLookupKind.Client:
            {
                var items = await clientReader.GetClientsByNameAsync(
                    searchTerm,
                    take,
                    cancellationToken);

                return items
                    .Select(item => new TripLookupItem(
                        item.Id,
                        item.Name,
                        BuildDetail(item.Address, item.PhoneNumber)))
                    .ToList();
            }

            case TripLookupKind.Supplier:
            {
                var items = await supplierReader.GetSuppliersByNameAsync(
                    searchTerm,
                    take,
                    cancellationToken);

                return items
                    .Select(item => new TripLookupItem(
                        item.Id,
                        item.Name,
                        BuildDetail(item.Address, item.PhoneNumber)))
                    .ToList();
            }

            case TripLookupKind.ShippingCarrier:
            {
                var items = await carrierReader.GetShippingCarriersByNameAsync(
                    searchTerm,
                    take,
                    cancellationToken);

                return items
                    .Select(item => new TripLookupItem(
                        item.Id,
                        item.Name,
                        BuildDetail(item.Address, item.PhoneNumber)))
                    .ToList();
            }

            default:
                return [];
        }
    }

    private static string? BuildDetail(params string?[] parts)
    {
        var values = parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToArray();

        return values.Length == 0 ? null : string.Join(" · ", values);
    }
}
