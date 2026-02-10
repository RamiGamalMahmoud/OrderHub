using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using OrderHub.UI.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Deliverymen.Editor;

public abstract partial class ViewModel : EditorViewModelBase, IDisposable
{
    protected readonly IMediator _mediator;

    protected ViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _notifyPropertiesNames = [nameof(Name), nameof(SelectedCity)];
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
    [Required(ErrorMessage = "اسم المدينة مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private CityInfoDto _selectedCity;

    private IEnumerable<CityInfoDto> _cities;
    public IEnumerable<CityInfoDto> Cities { get => _cities; set => SetProperty(ref _cities, value); }

}
