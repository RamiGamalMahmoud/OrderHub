using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OrderHub.Application.Features.Administration.Roles.Create;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Application.Features.Administration.Roles;

public class CreateRoleCommandHandlerTests
{
    [Fact]
    public async Task Create_applies_domain_permissions_and_commits_once()
    {
        var repository = new Mock<IRoleRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        Role added = null;
        repository.Setup(x => x.Add(It.IsAny<Role>())).Callback<Role>(role => added = role);
        using var cancellation = new CancellationTokenSource();
        var handler = new CreateRoleCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(new(new(" Editors ",
            [new(PermissionFeature.Orders, PermissionLevel.Read),
             new(PermissionFeature.Products, PermissionLevel.Write),
             new(PermissionFeature.Clients, PermissionLevel.None)])), cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal("Editors", added.Name);
        Assert.Equal(2, added.Permissions.Count);
        Assert.Contains(added.Permissions, permission => permission.Feature == PermissionFeature.Orders && permission.Level == PermissionLevel.Read);
        Assert.Contains(added.Permissions, permission => permission.Feature == PermissionFeature.Products && permission.Level == PermissionLevel.Write);
        Assert.DoesNotContain(added.Permissions, permission => permission.Level == PermissionLevel.None);
        Assert.All(added.Permissions, permission => Assert.Same(added, permission.Role));
        repository.Verify(x => x.NameExistsAsync("Editors", null, cancellation.Token), Times.Once);
        repository.Verify(x => x.Add(added), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Duplicate_name_does_not_add_or_commit()
    {
        var repository = new Mock<IRoleRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(x => x.NameExistsAsync("Editors", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await new CreateRoleCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new(new(" Editors ", [])), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("اسم الدور مستخدم بالفعل.", result.ErrorMessage);
        repository.Verify(x => x.Add(It.IsAny<Role>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Repeated_feature_inputs_follow_domain_setting_behavior_without_duplicate_rows()
    {
        var repository = new Mock<IRoleRepository>();
        Role added = null;
        repository.Setup(x => x.Add(It.IsAny<Role>())).Callback<Role>(role => added = role);
        var result = await new CreateRoleCommandHandler(repository.Object, new Mock<IUnitOfWork>().Object).Handle(new(new("Editors",
            [new(PermissionFeature.Orders, PermissionLevel.Write), new(PermissionFeature.Orders, PermissionLevel.None),
             new(PermissionFeature.Orders, PermissionLevel.Read)])), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(PermissionLevel.Read, Assert.Single(added.Permissions).Level);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invalid_name_is_rejected_by_domain_without_committing(string name)
    {
        var repository = new Mock<IRoleRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        await Assert.ThrowsAsync<ArgumentException>(() => new CreateRoleCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new(new(name, [])), default));
        repository.Verify(x => x.Add(It.IsAny<Role>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
