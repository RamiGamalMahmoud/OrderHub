using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using OrderHub.UI.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.UI.Features.Cities.Index;

public partial class ViewModel : IndexViewModelBase<CityListDto>
{
    private List<CityListDto> _allCities = [];
    private IEnumerable<CityListDto> _cities;

    public ViewModel(IMediator mediator)
        : base(mediator)
    {
        WeakReferenceMessenger.Default.Register<CitiesMessages.CityCreatedMessage>(this, async (_, _) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<CitiesMessages.CityUpdatedMessage>(this, async (_, _) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<CitiesMessages.CityDeletedMessage>(this, async (_, _) => await ReloadAsync());
    }

    public IEnumerable<CityListDto> Cities
    {
        get => _cities;
        private set => SetProperty(ref _cities, value);
    }

    [ObservableProperty]
    private string _searchTerm;

    partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

    protected override async Task LoadAsync()
    {
        _allCities = (await _mediator.Send(new Application.Queries.CityQueries.GetAllCitiesQuery())).ToList();
        ApplyFilter();
    }

    protected override async Task ReloadAsync()
    {
        _allCities = (await _mediator.Send(new Application.Queries.CityQueries.GetAllCitiesQuery())).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<CityListDto> filtered = _allCities;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string term = SearchTerm.Trim();
            filtered = filtered.Where(city => city.Name?.Contains(term) == true);
        }

        Cities = filtered.ToList();
    }
}
