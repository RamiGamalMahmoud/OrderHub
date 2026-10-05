using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.GetAll;

public sealed record GetPropertiesQuery : IRequest<IEnumerable<PropertyListItem>>;
