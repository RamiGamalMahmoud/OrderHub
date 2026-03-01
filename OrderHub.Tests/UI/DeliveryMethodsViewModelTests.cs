using FluentAssertions;
using OrderHub.UI.Features.Orders.Editor;
using System.Collections.Generic;
using System.Linq;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.Tests.UI;

public class DeliveryMethodsViewModelTests
{
    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeTrue_WithNoDeliveryMethodsSelected()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod.Should().BeNull();
        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeFalse_WithDeliveryMethodPickup()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.Where(d => d.Value == Domain.Enums.DeliveryMethod.Pickup).FirstOrDefault();

        deliveryMethodsViewModel.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeTrue_WithDeliveryMethodShippingCarrierAndSelectedShippingCarrierIsNull()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.Where(d => d.Value == Domain.Enums.DeliveryMethod.ShippingCompany).FirstOrDefault();

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.GetErrors().Select(e => e.ErrorMessage).Should().Contain("Shipping carrier is required");
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeFalsee_WithDeliveryMethodShippingCarrierAndHasSelectedShippingCarrier()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.Where(d => d.Value == Domain.Enums.DeliveryMethod.ShippingCompany).FirstOrDefault();

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.SelectedShippingCarrier = DeliveryMethodsViewModel.ShippingCarriers.FirstOrDefault();

        deliveryMethodsViewModel.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeTrue_WithDeliveryMethodDeliverymanAndSelectedDeliverymanIsNull()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.Where(d => d.Value == Domain.Enums.DeliveryMethod.DeliveryMan).FirstOrDefault();

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.GetErrors().Select(e => e.ErrorMessage).Should().Contain("Deliveryman is required");
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeFalsee_WithDeliveryMethodDeliverymanAndHasSelectedDeliveryman()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.Where(d => d.Value == Domain.Enums.DeliveryMethod.DeliveryMan).FirstOrDefault();

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.SelectedDeliveryman = DeliveryMethodsViewModel.Deliverymen.FirstOrDefault();

        deliveryMethodsViewModel.HasErrors.Should().BeFalse();
    }

    private DeliveryMethodsViewModel DeliveryMethodsViewModel => new DeliveryMethodsViewModel
    {
        Deliverymen =
            [
                new DeliverymanInfoDto(1, "Deliveryman 1"),
                new DeliverymanInfoDto(2, "Deliveryman 2"),
                new DeliverymanInfoDto(3, "Deliveryman 3")
            ],

        ShippingCarriers =
            [
                new ShippingCarrierInfoDto(1, "ShippingCarrier 1"),
                new ShippingCarrierInfoDto(2, "ShippingCarrier 2"),
                new ShippingCarrierInfoDto(3, "ShippingCarrier 3")
            ]
    };
}
/// Test 3 cases
/// 1. DeliveryMethod is Pickup
/// 2. DeliveryMethod is Deliveryman
/// 3. DeliveryMethod is ShippingCarrier
///