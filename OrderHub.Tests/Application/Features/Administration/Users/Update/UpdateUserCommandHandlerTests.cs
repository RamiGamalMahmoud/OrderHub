using FluentAssertions;
using Moq;
using OrderHub.Application.Features.Administration.Users.Update;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderHub.Tests.Application.Features.Administration.Users.Update;

public sealed class UpdateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();

    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateUserCommandHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_UserDoesNotExist()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null!);

        var command = CreateCommand(10);

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(!result.IsSuccess);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_UpdateBasicUserData()
    {
        // Arrange
        var user = CreateUser();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = CreateCommand(
            user.Id,
            userName: "new.username",
            displayName: "New Name");

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new.username", user.UserName);
        Assert.Equal("New Name", user.DisplayName);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_PreventDisabling_OnlyActiveAdmin()
    {
        // Arrange
        var user = CreateUser(
            isActive: true,
            isAdmin: true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.AnyAdminAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = CreateCommand(
            user.Id,
            isActive: false,
            isAdmin: true);

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(!result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.True(user.IsAdmin);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_PreventRemovingAdmin_IfUserIsOnlyActiveAdmin()
    {
        // Arrange
        var user = CreateUser(
            isActive: true,
            isAdmin: true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.AnyAdminAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = CreateCommand(
            user.Id,
            isActive: true,
            isAdmin: false);

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(!result.IsSuccess);
        Assert.True(user.IsAdmin);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_AllowDisablingAdmin_WhenAnotherActiveAdminExists()
    {
        // Arrange
        var user = CreateUser(
            isActive: true,
            isAdmin: true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.AnyOtherActiveAdminAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = CreateCommand(
            user.Id,
            isActive: false,
            isAdmin: false);

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(user.IsActive);
        Assert.False(user.IsAdmin);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_AssignsRole_ForNormalUserAndAdmin(bool admin)
    {
        var user = CreateUser();
        var role = new Role("Editors");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _roleRepositoryMock.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var result = await _handler.Handle(CreateCommand(1, isAdmin: admin, roleId: 7), default);
        Assert.True(result.IsSuccess);
        Assert.Same(role, user.Role);
        Assert.Equal(admin, user.IsAdmin);
    }

    [Fact]
    public async Task Handle_RemovesRole_WhenNoRoleSelected()
    {
        var user = CreateUser();
        user.AssignRole(new Role("Editors"));
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        Assert.True((await _handler.Handle(CreateCommand(1), default)).IsSuccess);
        Assert.Null(user.Role);
        Assert.Null(user.RoleId);
    }

    [Fact]
    public async Task Handle_RejectsMissingRole_WithoutMutationOrSave()
    {
        var user = CreateUser();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        Assert.False((await _handler.Handle(CreateCommand(1, roleId: 7), default)).IsSuccess);
        Assert.Equal("old.username", user.UserName);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    private static UpdateUserCommand CreateCommand(
        int userId,
        string userName = "rami",
        string displayName = "Rami Gamal",
        bool isActive = true,
        bool isAdmin = false,
        int? roleId = null)
    {
        return new UpdateUserCommand(
            userId,
            new UpdateUserRequest(
                userName,
                displayName,
                isActive,
                isAdmin,
                roleId));
    }

    private static User CreateUser(
        bool isActive = true,
        bool isAdmin = false)
    {
        var user = User.Create(
            "old.username",
            "Old Name",
            "password-hash");

        user.SetActive(isActive);
        user.SetAdmin(isAdmin);

        return user;
    }
}
