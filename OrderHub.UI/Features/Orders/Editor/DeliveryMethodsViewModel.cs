using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
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
        .Select(e => new EnumItem<DeliveryMethod>
        {
            Value = e,
            DisplayName = e.GetDescription()
        })
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

}
