using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.Application.Commands;

public static class ClienCommands
{
    public record CreateClientCommand(ClientCreateDto ClientCreateDto) : IRequest<Result>;
    public record DeleteClientCommand(int Id) : IRequest<Result>;
    public record UpdateClientCommand(ClientUpdateDto ClientUpdateDto) : IRequest<Result>;
}
