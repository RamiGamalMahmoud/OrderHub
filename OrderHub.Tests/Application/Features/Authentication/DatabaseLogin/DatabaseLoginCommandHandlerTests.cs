using System.Threading;
using System.Threading.Tasks;
using Moq;
using OrderHub.Application.Features.Authentication.DatabaseLogin;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Application.Features.Authentication.DatabaseLogin;

public sealed class DatabaseLoginCommandHandlerTests
{
    private const string InvalidCredentials = "بيانات تسجيل الدخول غير صحيحة.";

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Login_requires_an_active_user_and_a_correct_password(bool isActive, bool correctPassword)
    {
        var user = User.Create("testuser", "stored-hash", "Test User", isActive: isActive, isAdmin: true);
        var password = correctPassword ? "correct" : "incorrect";
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByUsernameAsync(user.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var hashing = new Mock<IHashShervice>(MockBehavior.Strict);
        if (isActive)
            hashing.Setup(x => x.Verify(password, user.PasswordHash)).Returns(correctPassword);
        var handler = new DatabaseLoginCommandHandler(repository.Object, hashing.Object);

        var result = await handler.Handle(new DatabaseLoginCommand(user.UserName, password, true), default);

        Assert.Equal(isActive && correctPassword, result.IsSuccess);
        if (result.IsSuccess)
        {
            Assert.Equal(user.Id, result.Value.Id);
            Assert.Equal(user.UserName, result.Value.UserName);
            Assert.Equal(user.DisplayName, result.Value.DisplayName);
            Assert.Equal(user.IsAdmin, result.Value.IsAdmin);
        }
        else
        {
            Assert.Equal(InvalidCredentials, result.ErrorMessage);
            Assert.Null(result.Value);
        }
        repository.Verify(x => x.GetByUsernameAsync(user.UserName, It.IsAny<CancellationToken>()), Times.Once);
        hashing.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), isActive ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task Missing_user_returns_the_same_failure_without_verifying_a_password()
    {
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByUsernameAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);
        var hashing = new Mock<IHashShervice>(MockBehavior.Strict);
        var handler = new DatabaseLoginCommandHandler(repository.Object, hashing.Object);

        var result = await handler.Handle(new DatabaseLoginCommand("missing", "password", false), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(InvalidCredentials, result.ErrorMessage);
        Assert.Null(result.Value);
        hashing.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
