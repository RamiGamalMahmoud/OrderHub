using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Services;
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
        WeakReferenceMessenger.Default.Register<Application.Messages.Cities.CityCreatedMessage>(this, async (_, _) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Cities.CityUpdatedMessage>(this, async (_, _) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Cities.CityDeletedMessage>(this, async (_, _) => await ReloadAsync());
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

    protected override async Task ShowCreateAsync()
    {
        await DialogService.Instance.ShowDialog<Create.CreateCityView>("إضافة مدينة جديدة");
    }

    protected override async Task ShowEditAsync(CityListDto model)
    {
        await DialogService.Instance.ShowDialog<Edit.EditCityView>("إضافة مدينة جديدة", model.Id);
    }

    protected override async Task DeleteAsync(CityListDto model)
    {
        if (!DialogService.Instance.Confirm($"هل تريد حذف المدينة ({model.Name})؟"))
        {
            return;
        }

        Result result = await _mediator.Send(new Application.Commands.CityCommands.DeleteCityCommand(model.Id));
        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess($"تم حذف المدينة ({model.Name}) بنجاح.");
            WeakReferenceMessenger.Default.Send(new Application.Messages.Cities.CityDeletedMessage());
            return;
        }

        NotificationService.Instance.ShowError(result.ErrorMessage);
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
