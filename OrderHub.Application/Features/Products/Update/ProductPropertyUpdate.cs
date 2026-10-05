using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Update;

public record ProductPropertyUpdate(
            int PropertyId,
            bool IsRequired);
