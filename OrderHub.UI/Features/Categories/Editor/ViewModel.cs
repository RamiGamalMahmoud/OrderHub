using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Categories.Editor;

public abstract partial class ViewModel : Common.EditorViewModelBase
{
    protected readonly IMediator _mediator;
    protected readonly ISelectionStore<ICategoryMarker, int> _selectionStore;

    protected ViewModel(IMediator mediator, ISelectionStore<ICategoryMarker, int> selectionStore)
    {
        _mediator = mediator;
        _selectionStore = selectionStore;
        ValidateAllProperties();
        _notifyPropertiesNames = [nameof(Name), nameof(SelectedParent)];
    }

    public virtual async Task LoadDataAsync()
    {
        Categories = await _mediator.Send(new Application.Queries.CommonQueries.GetCategoriesInfoQuery());
    }

    private IEnumerable<CategoryInfoDto> _categories;
    public IEnumerable<CategoryInfoDto> Categories
    {
        get => _categories;
        private set => SetProperty(ref _categories, value);
    }

    [ObservableProperty]
    [Required(ErrorMessage = "اسم القسم مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    private CategoryInfoDto _selectedParent;
}
