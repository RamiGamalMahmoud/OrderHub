using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Moq;
using OrderHub.Application.Features.Administration.Users.Create;
using OrderHub.Application.Features.Administration.Users.GetUser;
using OrderHub.Application.Features.Administration.Users.GetUsers;
using OrderHub.Application.Features.Administration.Users.Update;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.UI.Features.Administration.Users.Create;
using OrderHub.UI.Features.Administration.Users.Details;
using OrderHub.UI.Features.Administration.Users.List;
using OrderHub.UI.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests.UI;

public class UserMessagingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreatePublishesOnlyOnSuccess(bool succeeds)
    {
        var executor = new Mock<IRequestExecutor>();
        var completion = new TaskCompletionSource<Result>();
        executor.Setup(x => x.ExecuteAsync(
                It.Is<IRequest<Result>>(r => r is CreateUserCommand), It.IsAny<CancellationToken>()))
            .Returns(completion.Task);
        var editor = new CreateUserViewModel(executor.Object)
        {
            UserName = "user", DisplayName = "User", Password = "secret", ConfirmPassword = "secret"
        };
        var recipient = new object();
        int received = 0;
        WeakReferenceMessenger.Default.Register<UsersMessages.UserCreatedMessage>(recipient, (_, _) => received++);
        try
        {
            Task save = editor.SaveAsync();
            Assert.True(editor.IsSaving);
            Assert.Equal(0, received);
            await editor.SaveAsync(); // A second click must not submit another create.
            completion.SetResult(succeeds ? Result.Success() : Result.Failure("Rejected"));
            await save;
            Assert.Equal(succeeds ? 1 : 0, received);
            Assert.False(editor.IsSaving);
            Assert.Equal(!succeeds, editor.ShowSaveButton);
            executor.Verify(x => x.ExecuteAsync(It.IsAny<IRequest<Result>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DetailsPublishesUpdatedUserIdOnlyOnSuccess(bool succeeds)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<IRequest<GetUserResponse>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserResponse(42, "user", "User", true, false, [], DateTime.UtcNow));
        executor.Setup(x => x.ExecuteAsync(
                It.Is<IRequest<Result>>(r => r is UpdateUserCommand && ((UpdateUserCommand)r).UserId == 42),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(succeeds ? Result.Success() : Result.Failure("Rejected"));
        var editor = new UserDetailsViewModel(executor.Object);
        await editor.Initialize(42);
        editor.EditCommand.Execute(null);
        var recipient = new object();
        UsersMessages.UserUpdatedMessage received = null;
        WeakReferenceMessenger.Default.Register<UsersMessages.UserUpdatedMessage>(recipient, (_, message) => received = message);
        try
        {
            await editor.SaveAsync();
            Assert.Equal(succeeds ? new UsersMessages.UserUpdatedMessage(42) : null, received);
            Assert.False(editor.IsSaving);
            Assert.Equal(!succeeds, editor.ShowSaveButton);
            executor.VerifyAll();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Fact]
    public async Task ListReloadsWhenAnEventArrivesDuringLoading()
    {
        var executor = new Mock<IRequestExecutor>();
        var initial = new TaskCompletionSource<IReadOnlyList<UserListItem>>();
        IReadOnlyList<UserListItem> current = [new(42, "user", "Updated", true, false, DateTime.UtcNow)];
        executor.SetupSequence(x => x.ExecuteAsync(It.IsAny<IRequest<IReadOnlyList<UserListItem>>>(), It.IsAny<CancellationToken>()))
            .Returns(initial.Task)
            .ReturnsAsync(current)
            .ReturnsAsync(current);
        var list = new UsersViewModel(executor.Object);
        try
        {
            Task load = list.LoadAsync();
            WeakReferenceMessenger.Default.Send(new UsersMessages.UserCreatedMessage());
            initial.SetResult([]);
            await load;
            Assert.Same(current, list.Users);
            WeakReferenceMessenger.Default.Send(new UsersMessages.UserUpdatedMessage(42));
            executor.Verify(x => x.ExecuteAsync(It.IsAny<IRequest<IReadOnlyList<UserListItem>>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(list);
        }
    }
}
