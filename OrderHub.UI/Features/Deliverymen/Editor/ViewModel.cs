using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Deliverymen.Editor;

public abstract partial class ViewModel : EditorViewModelBase, IDisposable
{
    protected readonly IMediator _mediator;
    protected readonly IDialogService _dialogService;

    protected ViewModel(IMediator mediator, IDialogService dialogService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _notifyPropertiesNames = [nameof(Name), nameof(SelectedCity), nameof(PhoneNumber)];
        WeakReferenceMessenger.Default.Register<Application.Messages.Cities.CityCreatedMessage>(this, async (r, m) =>
        {
            Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
        });
        ValidateAllProperties();
    }

    public virtual async Task InitializeAsync()
    {
        Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
    }

    public virtual void Dispose()
    {
    }

    [ObservableProperty]
    [Required(ErrorMessage = "اسم المندوب مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _phoneNumber;

    [ObservableProperty]
    [Required(ErrorMessage = "اسم المدينة مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private CityInfoDto _selectedCity;

    private IEnumerable<CityInfoDto> _cities;
    public IEnumerable<CityInfoDto> Cities { get => _cities; set => SetProperty(ref _cities, value); }
    [RelayCommand]
    private void ShowCreateCity() => _dialogService.ShowDialog<Features.Cities.Create.CreateCityView>();

}
