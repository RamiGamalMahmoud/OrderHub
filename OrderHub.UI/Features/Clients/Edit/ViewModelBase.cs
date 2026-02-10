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

namespace OrderHub.UI.Features.Clients.Edit
{
    public abstract partial class ViewModelBase : EditorViewModelBase
    {
        private IEnumerable<CityInfoDto> _cities;
        protected readonly IMediator _mediator;
        private readonly IDialogService _dialogService;
        protected readonly IMessenger _messenger;

        protected ViewModelBase(IMediator mediator, IDialogService dialogService, IMessenger messenger)
        {
            _notifyPropertiesNames = [nameof(Name), nameof(Street), nameof(SelectedCity), nameof(Number), nameof(CountryCode)];
            _mediator = mediator;
            _dialogService = dialogService;
            _messenger = messenger;

            _messenger.Register<Application.Messages.Cities.CityCreatedMessage>(this, async (r, m) =>
            {
                Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
            });
            ValidateAllProperties();
        }   

        public virtual async Task LoadAsync()
        {
            Cities = await _mediator.Send(new Application.Queries.CommonQueries.GetCitiesInfoQuery());
        }

        public IEnumerable<CityInfoDto> Cities { get => _cities; private set => SetProperty(ref _cities, value); }

        [ObservableProperty]
        [Required(ErrorMessage = "اسم العميل مطلوب")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _name = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "الشارع مطلوب")]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        [NotifyDataErrorInfo]
        private string _street;

        [ObservableProperty]
        [Required(ErrorMessage = "المدينة مطلوبة")]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        [NotifyDataErrorInfo]
        private CityInfoDto _selectedCity;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Length(10, 14, ErrorMessage = "رقم الهاتف يجب ان يكون على الاقل 10 رقم وعلى الاكثر 14 رقم")]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _number;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "مفتاح الدولة مطلوب")]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        [MinLength(2, ErrorMessage = "مفتاح الدولة يجب ان يكون على الاقل 2 رقم")]
        private string _countryCode = "+966";

        [RelayCommand]
        private void ShowCreateCity() => _dialogService.ShowDialog<Features.Cities.Create.CreateCityView>();
    }
}
