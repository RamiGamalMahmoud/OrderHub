using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Exceptions;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Create;

[RequirePermission(PermissionFeature.Properties, PermissionLevel.Write)]
public sealed record CreatePropertyCommand(
    string Name,
    PropertyType PropertyType,
    string desctiption,
    IReadOnlyCollection<PropertyOptionInput> Options)
    : IRequest<int>;
