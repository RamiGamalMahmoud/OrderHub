using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.SupplierDtos;

namespace OrderHub.Application.Commands;

public static class SupplierCommands
{
    public record CreateSupplierCommand(SupplierCreateDto Dto) : IRequest<Result>;
    public record DeleteSupplierCommand(int Id) : IRequest<Result>;
    public record UpdateSupplierCommand(SupplierUpdateDto Dto) : IRequest<Result>;
}
