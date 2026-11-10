using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.GetDeliverymanTripsSummary;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Read)]
public record GetDeliverymanTripsSummaryQuery(int DeliverymanId)
    : IRequest<DeliverymanTripsSummary>;
