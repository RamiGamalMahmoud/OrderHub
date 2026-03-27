using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Commands;

public static class WhatsappGroupCommands
{
    public record CreateWhatsappGroupCommand(string Name, WhatsappGroupType WhatsappGroupType) : IRequest<Result>;
    public record UpdateWhatsappGroupCommand(int Id, string Name, WhatsappGroupType WhatsappGroupType) : IRequest<Result>;
    public record DeleteWhatsappGroupCommand(int Id) : IRequest<Result>;
}
