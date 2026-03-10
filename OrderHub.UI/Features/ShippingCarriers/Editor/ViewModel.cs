using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.ShippingCarriers.Editor;

public abstract partial class ViewModel : EditorViewModelBase
{
    protected IMediator _mediator;
    protected readonly IDialogService _dialogService;

    protected ViewModel(IMediator mediator, IDialogService dialogService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _notifyPropertiesNames =
        [
            nameof(Name),
            nameof(ShippingCostText),
            nameof(SelectedCity),
            nameof(Street),
            nameof(CountryCode),
            nameof(PhoneNumber)
        ];
        WeakReferenceMessenger.Default.Register<Application.Messages.Cities.CityCreatedMessage>(this, async (r, m) =>
        {
            Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
        });
        ValidateAllProperties();
    }

    [ObservableProperty]
    [Required(ErrorMessage = "اسم شركة الشحن مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    [RegularExpression(@"^\d+([.,]\d{0,2})?$", ErrorMessage = "السعر غير صالح")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _shippingCostText;

    [ObservableProperty]
    [Required(ErrorMessage = "الشارع مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _street;

    [ObservableProperty]
    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [Length(7, 14, ErrorMessage = "رقم الهاتف يجب ان يكون على الاقل 7 رقم وعلى الاكثر 14 رقم")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _phoneNumber;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "مفتاح الدولة مطلوب")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [MinLength(2, ErrorMessage = "مفتاح الدولة يجب ان يكون على الاقل 2 رقم")]
    private string _countryCode = "+966";

    [ObservableProperty]
    [Required(ErrorMessage = "المدينة مطلوبة")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private CityInfoDto _selectedCity;

    [ObservableProperty]
    private IEnumerable<CityInfoDto> _cities;

    public virtual async Task LoadAsync()
    {
        Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
    }

    [RelayCommand]
    private void ShowCreateCity() => _dialogService.ShowDialog<Features.Cities.Create.CreateCityView>();
}
