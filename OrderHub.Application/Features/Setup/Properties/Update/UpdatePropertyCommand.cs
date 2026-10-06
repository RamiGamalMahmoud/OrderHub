using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;

using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Update;

[RequirePermission(PermissionFeature.Properties, PermissionLevel.Write)]
public sealed record UpdatePropertyCommand(PropertyUpdateInput Dto) : IRequest;
