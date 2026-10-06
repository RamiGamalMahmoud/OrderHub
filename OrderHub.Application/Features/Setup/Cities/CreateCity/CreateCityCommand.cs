using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

[RequirePermission(PermissionFeature.Cities, PermissionLevel.Write)]
public record CreateCityCommand(CityCreateInput CityCreateInput) : IRequest<Result>;
