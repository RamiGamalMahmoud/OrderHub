using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.Application.Commands;

public static class CityCommands
{
    public record CreateCityCommand(CityCreateDto CityCreateDto) : IRequest<Result>;
    public record DeleteCityCommand(int Id) : IRequest<Result>;
    public record UpdateCityCommand(CityUpdateDto CityUpdateDto) : IRequest<Result>;
}
