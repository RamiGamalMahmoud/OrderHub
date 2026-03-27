using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.Application.Commands;

public static class ClienCommands
{
    public record CreateClientCommand(ClientFormDto Client) : IRequest<Result>;
    public record DeleteClientCommand(int Id) : IRequest<Result>;
    public record UpdateClientCommand(int Id, ClientFormDto Client) : IRequest<Result>;
}
