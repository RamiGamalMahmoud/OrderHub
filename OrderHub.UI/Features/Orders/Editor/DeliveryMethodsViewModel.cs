using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Orders.Editor;

public partial class DeliveryMethodsViewModel : ObservableValidator
{
    public DeliveryMethodsViewModel()
    {
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
        SelectedDeliveryman = null;
        SelectedShippingCarrier = null;
    }

    [ObservableProperty]
    private ShippingCarrierInfoDto _selectedShippingCarrier;

    partial void OnSelectedShippingCarrierChanged(ShippingCarrierInfoDto oldValue, ShippingCarrierInfoDto newValue)
    {
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    [ObservableProperty]
    private DeliverymanInfoDto _selectedDeliveryman;

    partial void OnSelectedDeliverymanChanged(DeliverymanInfoDto oldValue, DeliverymanInfoDto newValue)
    {
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    [ObservableProperty]
    private IEnumerable<DeliverymanInfoDto> _deliverymen;

    [ObservableProperty]
    private IEnumerable<ShippingCarrierInfoDto> _shippingCarriers;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveDeliveryStepDownCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDeliveryStepUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveDeliveryStepCommand))]
    private DeliveryStepViewModel _selectedDeliveryStep;

    public ObservableCollection<DeliveryStepViewModel> DeliverySteps { get; set; } = [];

    // ================================
    // Validation
    // ================================
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

            DeliveryMethod.Pickup => ValidationResult.Success,

            _ => ValidationResult.Success,
        };
    }

    // ================================
    // Commands
    // ================================

    [RelayCommand]
    private void AddDeliverymanStep()
    {
        DeliverySteps.Add(new DeliveryStepViewModel()
        {
            Handlers = Deliverymen.Select(d => new Handler(d.Id, d.Name)),
            Type = DeliveryMethod.DeliveryMan.GetDescription(),
            StepOrder = DeliverySteps.Count + 1
        });
    }

    [RelayCommand]
    private void AddShippingCarrierStep()
    {
        DeliverySteps.Add(new DeliveryStepViewModel()
        {
            Handlers = ShippingCarriers.Select(d => new Handler(d.Id, d.Name)),
            Type = DeliveryMethod.ShippingCompany.GetDescription(),
            StepOrder = DeliverySteps.Count + 1
        });
    }

    [RelayCommand(CanExecute = nameof(CanRemoveDeliveryStep))]
    private void RemoveDeliveryStep(DeliveryStepViewModel step)
    {
        DeliverySteps.Remove(step);
        ResetSelectedDeliveryStepsOrder();
    }

    [RelayCommand(CanExecute = nameof(CanMoveDeliveryStepDown))]
    private void MoveDeliveryStepDown(DeliveryStepViewModel step) => MoveStep(step, MoveDirection.Down);

    [RelayCommand(CanExecute = nameof(CanMoveDeliveryStepUp))]
    private void MoveDeliveryStepUp(DeliveryStepViewModel step) => MoveStep(step, MoveDirection.Up);

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

    private void MoveStep(DeliveryStepViewModel step, MoveDirection direction)
    {
        int index = DeliverySteps.IndexOf(step);
        if (index == -1) return;

        int newIndex = index + (int)direction;

        if (newIndex < 0 || newIndex >= DeliverySteps.Count)
            return;

        DeliverySteps.Move(index, newIndex);
        ResetSelectedDeliveryStepsOrder();

        MoveDeliveryStepUpCommand.NotifyCanExecuteChanged();
        MoveDeliveryStepDownCommand.NotifyCanExecuteChanged();
    }

    public void ResetSelectedDeliveryStepsOrder()
    {
        for (int i = 0; i < DeliverySteps.Count; i++)
        {
            DeliverySteps[i].StepOrder = i + 1;
        }
    }
}

public record Handler(int Id, string Name);

public partial class DeliveryStepViewModel : ObservableObject
{
    public int Id { get; set; }
    public int OrderId { get; set; }

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