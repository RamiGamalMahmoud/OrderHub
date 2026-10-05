using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Get;

public sealed record GetPropertyQuery(int Id) : IRequest<PropertyDetails>;
