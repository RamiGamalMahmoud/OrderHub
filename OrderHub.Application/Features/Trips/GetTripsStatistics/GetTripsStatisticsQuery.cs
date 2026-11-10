using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.GetTripsStatistics;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Read)]
public record GetTripsStatisticsQuery : IRequest<TripsStatistics>;
