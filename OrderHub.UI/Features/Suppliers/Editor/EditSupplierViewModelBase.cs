using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Suppliers.Editor;

public abstract partial class EditSupplierViewModelBase : EditorViewModelBase
{
    protected readonly IMediator _mediator;

    public EditSupplierViewModelBase(IMediator mediator)
    {
        _notifyPropertiesNames = [nameof(Name), nameof(OpenAt), nameof(CloseAt), nameof(Number), nameof(CountryCode), nameof(City), nameof(Street), nameof(SelectedWhatsappGroup)];
        _mediator = mediator;
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
        ValidateAllProperties();
    }

    [ObservableProperty]
    [Required(ErrorMessage = "اسم المورد مطلوب")]
    [MinLength(2, ErrorMessage = "الاسم يجب أن يكون أكثر من 2 حرف")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "وقت الافتتاح مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime? _openAt;

    [ObservableProperty]
    [Required(ErrorMessage = "وقت الاغلاق مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime? _closeAt;

    [ObservableProperty]
    [Required(ErrorMessage = "الشارع مطلوب")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyDataErrorInfo]
    private string _street;

    [ObservableProperty]
    [Required(ErrorMessage = "المدينة مطلوبة")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyDataErrorInfo]
    private CityInfoDto _city;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [Length(7, 14, ErrorMessage = "رقم الهاتف يجب ان يكون على الاقل 7 رقم وعلى الاكثر 14 رقم")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _number;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "مفتاح الدولة مطلوب")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [MinLength(2, ErrorMessage = "مفتاح الدولة يجب ان يكون على الاقل 2 رقم")]
    private string _countryCode = "+966";

    [ObservableProperty]
    private IEnumerable<CityInfoDto> _cities;

    [ObservableProperty]
    private IEnumerable<Application.DTOs.WhatsappGroupDtos.WhatsappGroupInfoDto> _whatsappGroups;

    [ObservableProperty]
    [Required(ErrorMessage = "يجب اختيار جروب الواتساب")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyDataErrorInfo]
    private Application.DTOs.WhatsappGroupDtos.WhatsappGroupInfoDto _selectedWhatsappGroup;

    [RelayCommand]
    private void ShowCreateCity() => DialogService.Instance.ShowDialog<Cities.Create.CreateCityView>();

    public virtual async Task LoadAsync()
    {
        Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
        WhatsappGroups = await _mediator.Send(new Application.Queries.WhatsappGroupQueries.GetAllWhatsappGroupInfosQuery(Domain.Enums.WhatsappGroupType.Suppliers));
    }

    [RelayCommand]
    private void CloseEdit() => OnRequestClose();

    [RelayCommand]
    private void ShowCreateWhatsappGroup() => DialogService.Instance.ShowDialog<Features.WhatsappGroups.Create.View>();


    public override string Title => "انشاء مورد جديد";
}
