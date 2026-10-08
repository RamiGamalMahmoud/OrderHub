using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OrderHub.Application.Features.Administration.Roles.Update;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Application.Features.Administration.Roles;

public class UpdateRoleCommandHandlerTests
{
    [Fact]
    public async Task Complete_state_updates_levels_removes_omissions_and_preserves_retained_instances()
    {
        var role = new Role("Old name");
        role.SetPermission(PermissionFeature.Orders, PermissionLevel.Write);
        role.SetPermission(PermissionFeature.Products, PermissionLevel.Read);
        role.SetPermission(PermissionFeature.Clients, PermissionLevel.Read);
        role.SetPermission(PermissionFeature.Suppliers, PermissionLevel.Write);
        var retained = role.Permissions.Single(permission => permission.Feature == PermissionFeature.Orders);
        var repository = Repository(role);
        var unitOfWork = new Mock<IUnitOfWork>();
        using var cancellation = new CancellationTokenSource();

        var result = await new UpdateRoleCommandHandler(repository.Object, unitOfWork.Object).Handle(new(42, new(" Editors ",
            [new(PermissionFeature.Orders, PermissionLevel.Read), new(PermissionFeature.Products, PermissionLevel.Write),
             new(PermissionFeature.Documents, PermissionLevel.Read)])), cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal("Editors", role.Name);
        Assert.Equal(3, role.Permissions.Count);
        Assert.Same(retained, role.Permissions.Single(permission => permission.Feature == PermissionFeature.Orders));
        Assert.Equal(PermissionLevel.Read, retained.Level);
        Assert.Equal(PermissionLevel.Write, role.Permissions.Single(permission => permission.Feature == PermissionFeature.Products).Level);
        Assert.Contains(role.Permissions, permission => permission.Feature == PermissionFeature.Documents && permission.Level == PermissionLevel.Read);
        Assert.DoesNotContain(role.Permissions, permission => permission.Feature == PermissionFeature.Clients || permission.Feature == PermissionFeature.Suppliers);
        // All permissions still have generated RoleId 0; synchronization must use Feature.
        Assert.All(role.Permissions, permission => Assert.Equal(0, permission.RoleId));
        repository.Verify(x => x.GetByIdAsync(42, cancellation.Token), Times.Once);
        repository.Verify(x => x.NameExistsAsync("Editors", 42, cancellation.Token), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(cancellation.Token), Times.Once);
        repository.Verify(x => x.Add(It.IsAny<Role>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Empty_state_or_explicit_None_removes_permissions(bool explicitNone)
    {
        var role = new Role("Editors");
        role.SetPermission(PermissionFeature.Orders, PermissionLevel.Write);
        role.SetPermission(PermissionFeature.Products, PermissionLevel.Read);
        var unitOfWork = new Mock<IUnitOfWork>();
        var request = new UpdateRoleRequest("Editors", explicitNone ? [new(PermissionFeature.Orders, PermissionLevel.None)] : []);
        var result = await new UpdateRoleCommandHandler(Repository(role).Object, unitOfWork.Object).Handle(new(42, request), default);
        Assert.True(result.IsSuccess);
        Assert.Empty(role.Permissions);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Missing_role_does_not_check_name_or_commit()
    {
        var repository = new Mock<IRoleRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var result = await new UpdateRoleCommandHandler(repository.Object, unitOfWork.Object).Handle(new(42, new("Editors", [])), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("الدور غير موجود.", result.ErrorMessage);
        repository.Verify(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Duplicate_name_leaves_domain_state_unchanged_and_does_not_commit()
    {
        var role = new Role("Original");
        role.SetPermission(PermissionFeature.Orders, PermissionLevel.Write);
        var original = role.Permissions.Single();
        var repository = Repository(role);
        repository.Setup(x => x.NameExistsAsync("Editors", 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var unitOfWork = new Mock<IUnitOfWork>();
        var result = await new UpdateRoleCommandHandler(repository.Object, unitOfWork.Object).Handle(new(42, new("Editors", [])), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("اسم الدور مستخدم بالفعل.", result.ErrorMessage);
        Assert.Equal("Original", role.Name);
        Assert.Same(original, Assert.Single(role.Permissions));
        Assert.Equal(PermissionLevel.Write, original.Level);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IRoleRepository> Repository(Role role)
    {
        var repository = new Mock<IRoleRepository>();
        repository.Setup(x => x.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        return repository;
    }
}
