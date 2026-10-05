using MediatR;

using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Update;

public sealed record UpdatePropertyCommand(PropertyUpdateInput Dto) : IRequest;
