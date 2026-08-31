using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Services;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.UI.Features.Categories.Create;

public class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator) : base(mediator)
    {
    }

    public override string Title { get; }

    protected override async Task Save()
    {
        CategoryCreateDto categoryCreateDto = new CategoryCreateDto(Name, SelectedParent?.Id);
        Result result = await _mediator.Send(new Application.Commands.CategoryCommands.CreateCategoryCommand(categoryCreateDto));

        if(result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم إضافة القسم");
            OnRequestClose();
            WeakReferenceMessenger.Default.Send(new Application.Messages.Categories.CategoryCreatedMessage());
        }

        else
        {
            NotificationService.Instance.ShowError(result.ErrorMessage);
        }
    }
}
