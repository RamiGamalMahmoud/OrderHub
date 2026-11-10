using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.GetTripEditor;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record GetTripEditorQuery(int Id) : IRequest<Result<TripEditorData>>;
