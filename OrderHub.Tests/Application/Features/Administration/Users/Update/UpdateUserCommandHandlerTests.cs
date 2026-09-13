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

    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateUserCommandHandler(
            _userRepositoryMock.Object,
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

    [Fact]
    public async Task Handle_Should_AddNewPermission()
    {
        // Arrange
        const int userId = 1;

        var user = CreateUser();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var permissions = new List<UpdateUserRequestPermission>
        {
            new(
                PermissionFeature.Orders,
                true,
                false)
        };

        var command = CreateCommand(
            userId,
            permissions: permissions);

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        Assert.Contains(
            user.Permissions,
            x =>
                x.Feature == PermissionFeature.Orders &&
                x.CanRead &&
                !x.CanWrite);
    }

    [Fact]
    public async Task Handle_Should_UpdateExistingPermission()
    {
        // Arrange
        var user = CreateUser();

        user.SetPermission(
            PermissionFeature.Orders,
            true,
            false);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var permissions = new List<UpdateUserRequestPermission>
        {
            new(
                PermissionFeature.Orders,
                true,
                true)
        };

        var command = CreateCommand(
            user.Id,
            permissions: permissions);

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var permission = Assert.Single(
            user.Permissions,
            x => x.Feature == PermissionFeature.Orders);

        Assert.True(permission.CanRead);
        Assert.True(permission.CanWrite);
    }

    [Fact]
    public async Task Handle_Should_RemovePermission_WhenReadAndWriteAreFalse()
    {
        // Arrange
        var user = CreateUser();

        user.SetPermission(
            PermissionFeature.Products,
            true,
            true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var permissions = new List<UpdateUserRequestPermission>
        {
            new(
                PermissionFeature.Products,
                false,
                false)
        };

        var command = CreateCommand(
            user.Id,
            permissions: permissions);

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        Assert.DoesNotContain(
            user.Permissions,
            x => x.Feature == PermissionFeature.Products);
    }

    [Fact]
    public async Task Handle_Should_SyncMultiplePermissions()
    {
        // Arrange
        var user = CreateUser();

        user.SetPermission(
            PermissionFeature.Orders,
            true,
            false);

        user.SetPermission(
            PermissionFeature.Products,
            true,
            true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var permissions = new List<UpdateUserRequestPermission>
        {
            // Update
            new(
                PermissionFeature.Orders,
                true,
                true),

            // Remove
            new(
                PermissionFeature.Products,
                false,
                false),

            // Add
            new(
                PermissionFeature.Settings,
                true,
                false)
        };

        var command = CreateCommand(
            user.Id,
            permissions: permissions);

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        Assert.Contains(
            user.Permissions,
            x =>
                x.Feature == PermissionFeature.Orders &&
                x.CanRead &&
                x.CanWrite);

        Assert.DoesNotContain(
            user.Permissions,
            x => x.Feature == PermissionFeature.Products);

        Assert.Contains(
            user.Permissions,
            x =>
                x.Feature == PermissionFeature.Settings &&
                x.CanRead &&
                !x.CanWrite);
    }

    private static UpdateUserCommand CreateCommand(
        int userId,
        string userName = "rami",
        string displayName = "Rami Gamal",
        bool isActive = true,
        bool isAdmin = false,
        IReadOnlyList<UpdateUserRequestPermission> permissions = null)
    {
        return new UpdateUserCommand(
            userId,
            new UpdateUserRequest(
                userName,
                displayName,
                isActive,
                isAdmin,
                permissions ?? []));
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


