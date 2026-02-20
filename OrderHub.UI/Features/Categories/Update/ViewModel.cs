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
    public ViewModel(IMediator mediator, ISelectionStore<ICategoryMarker, int> selectionStore) : base(mediator, selectionStore)
    {
    }

    public override string Title { get; }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();
        CategoryEditDto categoryEditDto = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryForEditQuery(_selectionStore.Id));
        Name = categoryEditDto.Name;
        SelectedParent = Categories.FirstOrDefault(c => c.Id == categoryEditDto.ParentId);
    }

    protected override async Task Save()
    {
        CategoryUpdateDto categoryUpdateDto = new CategoryUpdateDto(_selectionStore.Id, Name, SelectedParent?.Id);
        Result result = await _mediator.Send(new Application.Commands.CategoryCommands.UpdateCategoryCommand(categoryUpdateDto));
    }
}
