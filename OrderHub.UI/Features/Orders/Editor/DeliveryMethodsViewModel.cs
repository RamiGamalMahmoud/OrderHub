using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static OrderHub.Application.DTOs.DeliverymanDtos;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.Orders.Editor;

public partial class DeliveryMethodsViewModel : ObservableValidator
{
    private readonly DeliveryPartyLookupViewModel _lookup;
    private readonly DeliveryChainEditorViewModel _chain;

    public DeliveryMethodsViewModel(IMediator mediator)
    {
        _lookup = new DeliveryPartyLookupViewModel(mediator);
        _chain = new DeliveryChainEditorViewModel();

        _lookup.PropertyChanged += Lookup_PropertyChanged;
        _chain.PropertyChanged += Chain_PropertyChanged;
        _chain.StepSelectionChanged += (_, _) => ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
        ValidateAllProperties();
    }

    public static IEnumerable<EnumItem<DeliveryMethod>> DeliveryMethods =>
        Enum.GetValues<DeliveryMethod>()
            .Cast<DeliveryMethod>()
            .Select(e => new EnumItem<DeliveryMethod>(e, e.GetDescription()))
            .OrderBy(e => e.DisplayName);

    [ObservableProperty]
    [Required(ErrorMessage = "Delivery method is required")]
    [CustomValidation(typeof(DeliveryMethodsViewModel), nameof(ValidateDeliveryMethod))]
    [NotifyDataErrorInfo]
    private EnumItem<DeliveryMethod> _selecteddDeliveryMethod;

    partial void OnSelecteddDeliveryMethodChanged(EnumItem<DeliveryMethod> oldValue, EnumItem<DeliveryMethod> newValue)
    {
        _lookup.ClearSelections();
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    public ShippingCarrierListDto SelectedShippingCarrier
    {
        get => _lookup.SelectedShippingCarrier;
        set => _lookup.SelectedShippingCarrier = value;
    }

    public DeliverymanListDto SelectedDeliveryman
    {
        get => _lookup.SelectedDeliveryman;
        set => _lookup.SelectedDeliveryman = value;
    }

    public IEnumerable<DeliverymanListDto> Deliverymen => _lookup.Deliverymen;
    public IEnumerable<ShippingCarrierListDto> ShippingCarriers => _lookup.ShippingCarriers;

    public string DeliverymanSearchTerm
    {
        get => _lookup.DeliverymanSearchTerm;
        set => _lookup.DeliverymanSearchTerm = value;
    }

    public string ShippingCarrierSearchTerm
    {
        get => _lookup.ShippingCarrierSearchTerm;
        set => _lookup.ShippingCarrierSearchTerm = value;
    }

    public DeliveryStepViewModel SelectedDeliveryStep
    {
        get => _chain.SelectedDeliveryStep;
        set => _chain.SelectedDeliveryStep = value;
    }

    public System.Collections.ObjectModel.ObservableCollection<DeliveryStepViewModel> DeliverySteps => _chain.DeliverySteps;

    public void SetDeliverymen(IEnumerable<DeliverymanListDto> deliverymen)
    {
        _lookup.SetDeliverymen(deliverymen);
        _chain.RefreshDeliverymanHandlers(_lookup.Deliverymen);
    }

    public void SetShippingCarriers(IEnumerable<ShippingCarrierListDto> shippingCarriers)
    {
        _lookup.SetShippingCarriers(shippingCarriers);
        _chain.RefreshShippingCarrierHandlers(_lookup.ShippingCarriers);
    }

    public static ValidationResult ValidateDeliveryMethod(EnumItem<DeliveryMethod> deliveryMethod, ValidationContext context)
    {
        if (context.ObjectInstance is not DeliveryMethodsViewModel viewModel || deliveryMethod is null)
            return ValidationResult.Success;

        return deliveryMethod.Value switch
        {
            DeliveryMethod.DeliveryMan when viewModel.SelectedDeliveryman is null
                => new ValidationResult("Deliveryman is required"),

            DeliveryMethod.ShippingCompany when viewModel.SelectedShippingCarrier is null
                => new ValidationResult("Shipping carrier is required"),

            DeliveryMethod.DeliveryChain when !viewModel.DeliverySteps.Any()
                => new ValidationResult("At least one delivery step is required"),

            DeliveryMethod.DeliveryChain when viewModel.DeliverySteps.Any(step => step.SelectedHandler is null)
                => new ValidationResult("Each delivery step must have a selected handler"),

            DeliveryMethod.Pickup => ValidationResult.Success,

            _ => ValidationResult.Success,
        };
    }

    [RelayCommand]
    private void AddDeliverymanStep()
    {
        _chain.AddDeliverymanStep((_lookup.Deliverymen ?? Enumerable.Empty<DeliverymanListDto>())
            .Select(d => new Handler(d.Id, d.Name)));
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    [RelayCommand]
    private void AddShippingCarrierStep()
    {
        _chain.AddShippingCarrierStep((_lookup.ShippingCarriers ?? Enumerable.Empty<ShippingCarrierListDto>())
            .Select(c => new Handler(c.Id, c.Name)));
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    [RelayCommand(CanExecute = nameof(CanRemoveDeliveryStep))]
    private void RemoveDeliveryStep(DeliveryStepViewModel step)
    {
        _chain.RemoveDeliveryStepCommand.Execute(step);
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    [RelayCommand(CanExecute = nameof(CanMoveDeliveryStepDown))]
    private void MoveDeliveryStepDown(DeliveryStepViewModel step) => _chain.MoveDeliveryStepDownCommand.Execute(step);

    [RelayCommand(CanExecute = nameof(CanMoveDeliveryStepUp))]
    private void MoveDeliveryStepUp(DeliveryStepViewModel step) => _chain.MoveDeliveryStepUpCommand.Execute(step);

    private bool CanRemoveDeliveryStep(DeliveryStepViewModel step) => step is not null;

    private bool CanMoveDeliveryStepDown(DeliveryStepViewModel step)
    {
        if (step is null) return false;

        int index = DeliverySteps.IndexOf(step);
        return index >= 0 && index < DeliverySteps.Count - 1;
    }

    private bool CanMoveDeliveryStepUp(DeliveryStepViewModel step)
    {
        if (step is null) return false;

        int index = DeliverySteps.IndexOf(step);
        return index > 0;
    }

    public void ResetSelectedDeliveryStepsOrder()
    {
        _chain.ResetSelectedDeliveryStepsOrder();
    }

    public IEnumerable<OrderDeliveryStepCreateDto> BuildDeliverySteps()
    {
        return _chain.BuildDeliverySteps();
    }

    private void Lookup_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeliveryPartyLookupViewModel.Deliverymen))
        {
            _chain.RefreshDeliverymanHandlers(_lookup.Deliverymen);
        }
        else if (e.PropertyName == nameof(DeliveryPartyLookupViewModel.ShippingCarriers))
        {
            _chain.RefreshShippingCarrierHandlers(_lookup.ShippingCarriers);
        }

        OnPropertyChanged(MapLookupPropertyName(e.PropertyName));

        if (e.PropertyName is nameof(DeliveryPartyLookupViewModel.SelectedDeliveryman)
            or nameof(DeliveryPartyLookupViewModel.SelectedShippingCarrier))
        {
            ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
        }
    }

    private void Chain_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(MapChainPropertyName(e.PropertyName));

        if (e.PropertyName == nameof(DeliveryChainEditorViewModel.SelectedDeliveryStep))
        {
            MoveDeliveryStepUpCommand.NotifyCanExecuteChanged();
            MoveDeliveryStepDownCommand.NotifyCanExecuteChanged();
            RemoveDeliveryStepCommand.NotifyCanExecuteChanged();
        }
    }

    private static string MapLookupPropertyName(string propertyName) => propertyName switch
    {
        nameof(DeliveryPartyLookupViewModel.SelectedDeliveryman) => nameof(SelectedDeliveryman),
        nameof(DeliveryPartyLookupViewModel.SelectedShippingCarrier) => nameof(SelectedShippingCarrier),
        nameof(DeliveryPartyLookupViewModel.Deliverymen) => nameof(Deliverymen),
        nameof(DeliveryPartyLookupViewModel.ShippingCarriers) => nameof(ShippingCarriers),
        nameof(DeliveryPartyLookupViewModel.DeliverymanSearchTerm) => nameof(DeliverymanSearchTerm),
        nameof(DeliveryPartyLookupViewModel.ShippingCarrierSearchTerm) => nameof(ShippingCarrierSearchTerm),
        _ => propertyName
    };

    private static string MapChainPropertyName(string propertyName) => propertyName switch
    {
        nameof(DeliveryChainEditorViewModel.SelectedDeliveryStep) => nameof(SelectedDeliveryStep),
        nameof(DeliveryChainEditorViewModel.DeliverySteps) => nameof(DeliverySteps),
        _ => propertyName
    };
}

public record Handler(int Id, string Name);

public partial class DeliveryStepViewModel : ObservableObject
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DeliveryMethod Method { get; set; }

    [ObservableProperty]
    private int _stepOrder;

    [ObservableProperty]
    private IEnumerable<Handler> _handlers;

    [ObservableProperty]
    private Handler _selectedHandler;

    [ObservableProperty]
    private string _type;
}

enum MoveDirection
{
    Up = -1,
    Down = 1
}
