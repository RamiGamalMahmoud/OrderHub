using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.UI.Features.Categories.Create;

public class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator, ISelectionStore<ICategoryMarker, int> selectionStore, IMessenger messenger) : base(mediator, selectionStore, messenger)
    {
    }

    public override string Title { get; }

    protected override async Task Save()
    {
        CategoryCreateDto categoryCreateDto = new CategoryCreateDto(Name, SelectedParent?.Id);
        Result result = await _mediator.Send(new Application.Commands.CategoryCommands.CreateCategoryCommand(categoryCreateDto));

        if(result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تمت إضافة القسم "));
            OnRequestClose();
            _messenger.Send(new Application.Messages.Categories.CategoryCreatedMessage());
        }

        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
        }
    }
}
