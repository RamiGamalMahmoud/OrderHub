using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
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
        CategorySelection = new CategorySelection(_mediator);
    }

    public virtual async Task LoadDataAsync()
    {
        await CategorySelection.LoadRootCategoriesAsync();
    }

    public CategorySelection CategorySelection { get; }

    [ObservableProperty]
    [Required(ErrorMessage = "اسم القسم مطلوب")]
    [MinLength(2, ErrorMessage = "الاسم يجب أن يكون أكثر من 2 حرف")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    private CategoryInfoDto _selectedParent;

    partial void OnSelectedParentChanged(CategoryInfoDto oldValue, CategoryInfoDto newValue)
    {
        CategorySelection.SelectedCategory = newValue;
    }
}
