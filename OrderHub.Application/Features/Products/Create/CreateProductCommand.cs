using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Create;

public record CreateProductCommand(ProductCreateInput Product) : IRequest<Result<int>>;
