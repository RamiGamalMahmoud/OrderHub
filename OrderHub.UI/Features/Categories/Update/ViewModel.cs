using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.UI.Features.Categories.Update;

public class ViewModel : Editor.ViewModel, IParameterizedViewModel
{
    private int _categoryId;
    public ViewModel(IMediator mediator) : base(mediator)
    {
    }

    public override string Title { get; }

    public Task Initialize(object parameter)
    {
        if(parameter is not null && parameter is int id)
        {
            _categoryId = id;
        }
        return Task.CompletedTask;
    }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();
        CategoryEditDto categoryEditDto = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryForEditQuery(_categoryId));
        Name = categoryEditDto.Name;
        SelectedParent = (await _mediator.Send(new Application.Queries.CommonQueries.GetCategoriesInfoQuery())).FirstOrDefault(c => c.Id == categoryEditDto.ParentId);
    }

    protected override async Task Save()
    {
        CategoryUpdateDto categoryUpdateDto = new CategoryUpdateDto(_categoryId, Name, SelectedParent?.Id);
        Result result = await _mediator.Send(new Application.Commands.CategoryCommands.UpdateCategoryCommand(categoryUpdateDto));
        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم التعديل بنجاح.");
            OnRequestClose();
            WeakReferenceMessenger.Default.Send(new Application.Messages.Categories.CategoryUpdatedMessage());
        }

        else
        {
            NotificationService.Instance.ShowError(result.ErrorMessage);
        }
    }
}
