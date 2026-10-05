using System.Threading;
using System.Threading.Tasks;
using Moq;
using OrderHub.Application.Features.Administration.Roles.Delete;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Application.Features.Administration.Roles;

public class DeleteRoleCommandHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Only_unassigned_roles_can_be_removed(bool assigned)
    {
        var role = new Role("Editors");
        var repository = new Mock<IRoleRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        using var cancellation = new CancellationTokenSource();
        repository.Setup(x => x.GetByIdAsync(42, cancellation.Token)).ReturnsAsync(role);
        repository.Setup(x => x.IsAssignedToUsersAsync(42, cancellation.Token)).ReturnsAsync(assigned);
        var result = await new DeleteRoleCommandHandler(repository.Object, unitOfWork.Object).Handle(new(42), cancellation.Token);
        Assert.Equal(!assigned, result.IsSuccess);
        if (assigned)
            Assert.Equal("لا يمكن حذف الدور لأنه مرتبط بمستخدمين.", result.ErrorMessage);
        repository.Verify(x => x.IsAssignedToUsersAsync(42, cancellation.Token), Times.Once);
        repository.Verify(x => x.Remove(role), assigned ? Times.Never() : Times.Once());
        unitOfWork.Verify(x => x.SaveChangesAsync(cancellation.Token), assigned ? Times.Never() : Times.Once());
    }

    [Fact]
    public async Task Missing_role_does_not_check_assignment_or_commit()
    {
        var repository = new Mock<IRoleRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var result = await new DeleteRoleCommandHandler(repository.Object, unitOfWork.Object).Handle(new(42), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("الدور غير موجود.", result.ErrorMessage);
        repository.Verify(x => x.IsAssignedToUsersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.Remove(It.IsAny<Role>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
