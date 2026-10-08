using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OrderHub.Application.Features.Administration.Roles.GetRole;
using OrderHub.Application.Features.Administration.Roles.GetRoles;
using OrderHub.Domain.Enums;

namespace OrderHub.Tests.Application.Features.Administration.Roles;

public class RoleReadQueryHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Details_delegates_to_its_read_contract_and_preserves_null_not_found(bool found)
    {
        var reader = new Mock<IRoleDetailsService>();
        using var cancellation = new CancellationTokenSource();
        var response = found ? new GetRoleResponse(42, "Editors", [new(PermissionFeature.Orders, PermissionLevel.Write)]) : null;
        reader.Setup(x => x.GetRoleDetailsAsync(42, cancellation.Token)).ReturnsAsync(response);
        var result = await new GetRoleQueryHandler(reader.Object).Handle(new(42), cancellation.Token);
        Assert.Same(response, result);
        reader.Verify(x => x.GetRoleDetailsAsync(42, cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Listing_delegates_to_its_read_contract_and_preserves_results(bool populated)
    {
        var reader = new Mock<IRoleListingService>();
        using var cancellation = new CancellationTokenSource();
        IReadOnlyList<RoleListItem> rows = populated ? [new(42, "Editors", 2)] : [];
        reader.Setup(x => x.GetAsync(cancellation.Token)).ReturnsAsync(rows);
        var result = await new GetRolesQueryHandler(reader.Object).Handle(new(), cancellation.Token);
        Assert.Same(rows, result);
        reader.Verify(x => x.GetAsync(cancellation.Token), Times.Once);
    }
}
