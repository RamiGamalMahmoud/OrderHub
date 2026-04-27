using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.DTOs.WhatsappGroupDtos;
using OrderHub.Domain.Enums;

namespace OrderHub.UI.Features.Deliverymen.Editor;

public abstract partial class ViewModel : EditorViewModelBase, IDisposable
{
    protected readonly IMediator _mediator;
    protected readonly IDialogService _dialogService;

    protected ViewModel(IMediator mediator, IDialogService dialogService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _notifyPropertiesNames = [nameof(Name), nameof(SelectedCity), nameof(PhoneNumber), nameof(SelectedWhatsappGroup)];
        WeakReferenceMessenger.Default.Register<Application.Messages.Cities.CityCreatedMessage>(this, async (r, m) =>
        {
            Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
        });
        WeakReferenceMessenger.Default.Register<Application.Messages.Cities.CityUpdatedMessage>(this, async (r, m) =>
        {
            Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
        });
        WeakReferenceMessenger.Default.Register<Application.Messages.Cities.CityDeletedMessage>(this, async (r, m) =>
        {
            Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
        });
        WeakReferenceMessenger.Default.Register<Application.Messages.WhatsappGroups.WhatsappGroupCreatedMessage>(this, async (r, m) =>
        {
            await LoadWhatsappGroupsAsync();
        });
        WeakReferenceMessenger.Default.Register<Application.Messages.WhatsappGroups.WhatsappGroupUpdatedMessage>(this, async (r, m) =>
        {
            await LoadWhatsappGroupsAsync();
        });
        WeakReferenceMessenger.Default.Register<Application.Messages.WhatsappGroups.WhatsappGroupDeletedMessage>(this, async (r, m) =>
        {
            await LoadWhatsappGroupsAsync();
        });
        ValidateAllProperties();
    }

    public virtual async Task InitializeAsync()
    {
        Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
        await LoadWhatsappGroupsAsync();
    }

    public virtual void Dispose()
    {
    }

    [ObservableProperty]
    [Required(ErrorMessage = "اسم المندوب مطلوب")]
    [MinLength(2, ErrorMessage = "الاسم يجب أن يكون أكثر من 2 حرف")]
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

    [ObservableProperty]
    private WhatsappGroupInfoDto _selectedWhatsappGroup;

    private IEnumerable<CityInfoDto> _cities;
    public IEnumerable<CityInfoDto> Cities { get => _cities; set => SetProperty(ref _cities, value); }

    private IEnumerable<WhatsappGroupInfoDto> _whatsappGroups;
    public IEnumerable<WhatsappGroupInfoDto> WhatsappGroups { get => _whatsappGroups; set => SetProperty(ref _whatsappGroups, value); }
    public IEnumerable<WhatsappGroupInfoDto> WhatsappGroupsOptions =>
        new[] { new WhatsappGroupInfoDto(0, "بدون مجموعة", WhatsappGroupType.Deliverymen) }
        .Concat(WhatsappGroups ?? []);

    [RelayCommand]
    private void ShowCreateCity() => _dialogService.ShowDialog<Features.Cities.Create.CreateCityView>();

    private async Task LoadWhatsappGroupsAsync()
    {
        WhatsappGroups = await _mediator.Send(
            new Application.Queries.WhatsappGroupQueries.GetAllWhatsappGroupInfosQuery(WhatsappGroupType.Deliverymen));
        OnPropertyChanged(nameof(WhatsappGroupsOptions));
    }

}
