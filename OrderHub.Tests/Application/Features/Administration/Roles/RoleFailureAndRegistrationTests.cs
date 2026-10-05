using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderHub.Application;
using OrderHub.Application.Features.Administration.Roles.Create;
using OrderHub.Application.Features.Administration.Roles.Update;
using OrderHub.Application.Features.Administration.Roles.Delete;
using OrderHub.Application.Features.Administration.Roles.GetRole;
using OrderHub.Application.Features.Administration.Roles.GetRoles;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Application.Features.Administration.Roles;

public class RoleFailureAndRegistrationTests
{
    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Delete")]
    public async Task Commit_failures_propagate_instead_of_becoming_business_failures(string operation)
    {
        var repository = new Mock<IRoleRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(x => x.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Role("Editors"));
        var failure = new InvalidOperationException("Storage failed");
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var command = operation switch
        {
            "Create" => new CreateRoleCommandHandler(repository.Object, unitOfWork.Object).Handle(new(new("Editors", [])), default),
            "Update" => new UpdateRoleCommandHandler(repository.Object, unitOfWork.Object).Handle(new(42, new("Editors", [])), default),
            _ => new DeleteRoleCommandHandler(repository.Object, unitOfWork.Object).Handle(new(42), default)
        };
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => command));
    }

    [Fact]
    public async Task Existing_Application_scan_registers_all_five_handlers_without_new_DI_pattern()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(new Mock<IRoleRepository>().Object);
        services.AddSingleton(new Mock<IUnitOfWork>().Object);
        services.AddSingleton(new Mock<IRoleDetailsService>().Object);
        var listing = new Mock<IRoleListingService>();
        listing.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<RoleListItem>());
        services.AddSingleton(listing.Object);
        using var provider = services.BuildServiceProvider();
        Assert.IsType<CreateRoleCommandHandler>(provider.GetRequiredService<IRequestHandler<CreateRoleCommand, OrderHub.Domain.Common.Result>>());
        Assert.IsType<UpdateRoleCommandHandler>(provider.GetRequiredService<IRequestHandler<UpdateRoleCommand, OrderHub.Domain.Common.Result>>());
        Assert.IsType<DeleteRoleCommandHandler>(provider.GetRequiredService<IRequestHandler<DeleteRoleCommand, OrderHub.Domain.Common.Result>>());
        Assert.IsType<GetRoleQueryHandler>(provider.GetRequiredService<IRequestHandler<GetRoleQuery, GetRoleResponse>>());
        Assert.IsType<GetRolesQueryHandler>(provider.GetRequiredService<IRequestHandler<GetRolesQuery, System.Collections.Generic.IReadOnlyList<RoleListItem>>>());
        Assert.Empty(await provider.GetRequiredService<IMediator>().Send(new GetRolesQuery()));
        Assert.DoesNotContain(typeof(CreateRoleCommand).Assembly.GetReferencedAssemblies(), reference =>
            new[] { "Microsoft.EntityFrameworkCore", "Npgsql", "OrderHub.Infrastructure", "OrderHub.Web", "Microsoft.AspNetCore" }.Any(prefix => reference.Name.StartsWith(prefix)));
    }
}
