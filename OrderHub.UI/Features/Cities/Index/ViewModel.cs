using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.UI.Features.Cities.Index;

public partial class ViewModel : IndexViewModelBase<CityListDto>
{
    private readonly IDialogService _dialogService;
    private readonly ISelectionStore<ICityMarker, int> _selectionStore;
    private List<CityListDto> _allCities = [];
    private IEnumerable<CityListDto> _cities;

    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService, ISelectionStore<ICityMarker, int> selectionStore)
        : base(mediator, messenger)
    {
        _dialogService = dialogService;
        _selectionStore = selectionStore;

        _messenger.Register<Application.Messages.Cities.CityCreatedMessage>(this, async (_, _) => await ReloadAsync());
        _messenger.Register<Application.Messages.Cities.CityUpdatedMessage>(this, async (_, _) => await ReloadAsync());
        _messenger.Register<Application.Messages.Cities.CityDeletedMessage>(this, async (_, _) => await ReloadAsync());
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

    protected override Task ShowCreateAsync()
    {
        _dialogService.ShowDialog<Create.CreateCityView>();
        return Task.CompletedTask;
    }

    protected override Task ShowEditAsync(CityListDto model)
    {
        _selectionStore.Id = model.Id;
        _dialogService.ShowDialog<Edit.EditCityView>();
        return Task.CompletedTask;
    }

    protected override async Task DeleteAsync(CityListDto model)
    {
        if (!_dialogService.Confirm($"هل تريد حذف المدينة ({model.Name})؟"))
        {
            return;
        }

        Result result = await _mediator.Send(new Application.Commands.CityCommands.DeleteCityCommand(model.Id));
        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification($"تم حذف المدينة ({model.Name}) بنجاح."));
            _messenger.Send(new Application.Messages.Cities.CityDeletedMessage());
            return;
        }

        await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
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
