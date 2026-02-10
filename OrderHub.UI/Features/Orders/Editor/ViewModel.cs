using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using OrderHub.UI.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class ViewModel : EditorViewModelBase
{
    private readonly IMediator _mediator;
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
        Clients = await _mediator.Send(new Application.Queries.ClientQueries.GetAllClientsQuery());
    }

    protected override Task Save()
    {
        throw new System.NotImplementedException();
    }

    async partial void OnSelectedCategoryChanged(CategoryInfoDto oldValue, CategoryInfoDto newValue)
    {
        if (newValue is null)
        {
            SubCategories.Clear();
            return;
        }

        if (newValue.ParentId is null)
        {
            SubCategories.Clear();
        }
        else
        {
            RemoveSubCategoriesAfterParent((int) newValue.ParentId);
        }

        IEnumerable<CategoryInfoDto> subCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetSubCategoriesQuery(newValue.Id));

        bool exists = _subCategories.Any(s => s.Key.Id == newValue.Id);

        if (subCategories.Any() && !exists)
        {
            SubCategories.Add(new KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>(newValue, subCategories));
        }
    }

    private void RemoveSubCategoriesAfterParent(int parentId)
    {
        List<int> ids = SubCategories.Select(c => c.Key.Id).ToList();

        int parentIndex = ids.IndexOf(parentId);

        if (parentIndex + 1 < ids.Count)
        {
            for (int i = parentIndex + 1; i < ids.Count; i++)
            {
                SubCategories.RemoveAt(i);
            }
        }
    }

    [ObservableProperty]
    private IEnumerable<CategoryInfoDto> _rootCategories;

    [ObservableProperty]
    private CategoryInfoDto _selectedCategory;

    [ObservableProperty]
    private IEnumerable<ClientListDto> _clients;

    [ObservableProperty]
    private ClientListDto _selectedClient;

    [ObservableProperty]
    private IEnumerable<ProductInfoDto> _products;

    public override string Title { get; }
}
