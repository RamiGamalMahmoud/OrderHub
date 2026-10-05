using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

public record DeleteCategoryCommand(int Id) : IRequest<Result>;
