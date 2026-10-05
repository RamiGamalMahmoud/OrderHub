using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Delete;

public record DeleteProductCommand(int Id) : IRequest<Result>;
