using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.UI.Features.Categories.Update;

public class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator, ISelectionStore<ICategoryMarker, int> selectionStore, IMessenger messenger) : base(mediator, selectionStore, messenger)
    {
    }

    public override string Title { get; }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();
        CategoryEditDto categoryEditDto = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryForEditQuery(_selectionStore.Id));
        Name = categoryEditDto.Name;
        SelectedParent = (await _mediator.Send(new Application.Queries.CommonQueries.GetCategoriesInfoQuery())).FirstOrDefault(c => c.Id == categoryEditDto.ParentId);
    }

    protected override async Task Save()
    {
        CategoryUpdateDto categoryUpdateDto = new CategoryUpdateDto(_selectionStore.Id, Name, SelectedParent?.Id);
        Result result = await _mediator.Send(new Application.Commands.CategoryCommands.UpdateCategoryCommand(categoryUpdateDto));
        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تمت تعديل القسم "));
            OnRequestClose();
            _messenger.Send(new Application.Messages.Categories.CategoryUpdatedMessage());
        }

        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
        }
    }
}
