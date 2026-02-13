using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.UI.Features.Categories.Create;

public class ViewModel : Editor.ViewModel
{
    private readonly IMessenger _messenger;

    public ViewModel(IMediator mediator, IMessenger messenger) : base(mediator)
    {
        _messenger = messenger;
    }

    public override string Title { get; }

    protected override async Task Save()
    {
        CategoryCreateDto categoryCreateDto = new CategoryCreateDto(Name, SelectedParent?.Id);
        Result result = await _mediator.Send(new Application.Commands.CategoryCommands.CreateCategoryCommand(categoryCreateDto));

        if(result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تمت إضافة القسم "));
            //OnRequestClose();
            _messenger.Send(new Application.Messages.Categories.CategoryCreatedMessage());
        }

        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("فشل إضافة القسم"));
        }
    }
}
