using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using OrderHub.UI.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class ViewModel : EditorViewModelBase
{
    private readonly IMediator _mediator;
    [ObservableProperty]
    private IEnumerable<CategoryTreeDto> _categories;
    [ObservableProperty]
    private CategoryTreeDto _selectedCategoryTree;
    private ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> _subCategories;
    public ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> SubCategories
    {
        get => _subCategories;
        set => SetProperty(ref _subCategories, value);
    }

    public ViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _subCategories = [];
    }

    internal async Task LoadAsync()
    {
        RootCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetRootCategoriesQuery());
        Categories = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryTreeQuery());
    }

    protected override Task Save()
    {
        throw new System.NotImplementedException();
    }

    async partial void OnSelectedCategoryChanged(CategoryInfoDto oldValue, CategoryInfoDto newValue)
    {
        IEnumerable<CategoryInfoDto> subCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetSubCategoriesQuery(newValue.Id));

        KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>> existing = _subCategories.Where(s => s.Key.Id == newValue.Id).FirstOrDefault();

        if(newValue.ParentId is null)
        {
            SubCategories.Clear();
        }

        if (subCategories.Any() && existing.Key is null)
        {
            SubCategories.Add(new KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>(newValue, subCategories));
        }
    }

    [ObservableProperty]
    private IEnumerable<CategoryInfoDto> _rootCategories;

    [ObservableProperty]
    private CategoryInfoDto _selectedCategory;

    [ObservableProperty]
    private IEnumerable<ClientInfoDto> _clients;

    [ObservableProperty]
    private ClientInfoDto _selectedClient;

    [ObservableProperty]
    private IEnumerable<ProductInfoDto> _products;

    public override string Title { get; }
}
