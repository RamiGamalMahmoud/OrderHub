using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Categories.Editor;

public abstract partial class ViewModel : Common.EditorViewModelBase
{
    protected readonly IMediator _mediator;

    protected ViewModel(IMediator mediator)
    {
        _mediator = mediator;
        ValidateAllProperties();
        _notifyPropertiesNames = [nameof(Name), nameof(SelectedParent)];
    }

    public async Task LoadDataAsync()
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
