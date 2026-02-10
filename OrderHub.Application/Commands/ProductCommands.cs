using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.Application.Commands;

public static class ProductCommands
{
    public record CreateProductCommand(ProductCreateDto ProductCreateDto) : IRequest<Result>;
    public record DeleteProductCommand(int Id) : IRequest<Result>;
    public record UpdateProductCommand(ProductUpdateDto ProductUpdateDto) : IRequest<Result>;
}
