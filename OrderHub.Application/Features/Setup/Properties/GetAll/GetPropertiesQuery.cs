using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.GetAll;

[RequirePermission(PermissionFeature.Properties, PermissionLevel.Read)]
public sealed record GetPropertiesQuery : IRequest<IEnumerable<PropertyListItem>>;
