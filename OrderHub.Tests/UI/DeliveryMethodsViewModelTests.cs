using FluentAssertions;
using MediatR;
using Moq;
using OrderHub.UI.Features.Orders.Editor;
using System.Linq;
using static OrderHub.Application.DTOs.DeliverymanDtos;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

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

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.FirstOrDefault(d => d.Value == Domain.Enums.DeliveryMethod.Pickup);

        deliveryMethodsViewModel.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeTrue_WithDeliveryMethodShippingCarrierAndSelectedShippingCarrierIsNull()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.FirstOrDefault(d => d.Value == Domain.Enums.DeliveryMethod.ShippingCompany);

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.GetErrors().Select(e => e.ErrorMessage).Should().Contain("Shipping carrier is required");
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeFalsee_WithDeliveryMethodShippingCarrierAndHasSelectedShippingCarrier()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.FirstOrDefault(d => d.Value == Domain.Enums.DeliveryMethod.ShippingCompany);

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.SelectedShippingCarrier = deliveryMethodsViewModel.ShippingCarriers.FirstOrDefault();

        deliveryMethodsViewModel.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeTrue_WithDeliveryMethodDeliverymanAndSelectedDeliverymanIsNull()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.FirstOrDefault(d => d.Value == Domain.Enums.DeliveryMethod.DeliveryMan);

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.GetErrors().Select(e => e.ErrorMessage).Should().Contain("Deliveryman is required");
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeFalsee_WithDeliveryMethodDeliverymanAndHasSelectedDeliveryman()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.FirstOrDefault(d => d.Value == Domain.Enums.DeliveryMethod.DeliveryMan);

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.SelectedDeliveryman = deliveryMethodsViewModel.Deliverymen.FirstOrDefault();

        deliveryMethodsViewModel.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeTrue_WithDeliveryMethodDeliveryChainAndNoSteps()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.FirstOrDefault(d => d.Value == Domain.Enums.DeliveryMethod.DeliveryChain);

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.GetErrors().Select(e => e.ErrorMessage).Should().Contain("At least one delivery step is required");
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeTrue_WithDeliveryMethodDeliveryChainAndStepWithoutHandler()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.FirstOrDefault(d => d.Value == Domain.Enums.DeliveryMethod.DeliveryChain);
        deliveryMethodsViewModel.AddDeliverymanStepCommand.Execute(null);

        deliveryMethodsViewModel.HasErrors.Should().BeTrue();
        deliveryMethodsViewModel.GetErrors().Select(e => e.ErrorMessage).Should().Contain("Each delivery step must have a selected handler");
    }

    [Fact]
    public void DeliveryMethodsViewModelHasErrors_ShouldBeFalse_WithDeliveryMethodDeliveryChainAndAllStepsHaveHandlers()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.SelecteddDeliveryMethod = DeliveryMethodsViewModel.DeliveryMethods.FirstOrDefault(d => d.Value == Domain.Enums.DeliveryMethod.DeliveryChain);
        deliveryMethodsViewModel.AddDeliverymanStepCommand.Execute(null);
        deliveryMethodsViewModel.AddShippingCarrierStepCommand.Execute(null);

        deliveryMethodsViewModel.DeliverySteps[0].SelectedHandler = deliveryMethodsViewModel.DeliverySteps[0].Handlers.First();
        deliveryMethodsViewModel.DeliverySteps[1].SelectedHandler = deliveryMethodsViewModel.DeliverySteps[1].Handlers.First();

        deliveryMethodsViewModel.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void RemoveDeliveryStep_ShouldResetStepOrder()
    {
        DeliveryMethodsViewModel deliveryMethodsViewModel = DeliveryMethodsViewModel;

        deliveryMethodsViewModel.AddDeliverymanStepCommand.Execute(null);
        deliveryMethodsViewModel.AddShippingCarrierStepCommand.Execute(null);
        deliveryMethodsViewModel.AddDeliverymanStepCommand.Execute(null);

        deliveryMethodsViewModel.RemoveDeliveryStepCommand.Execute(deliveryMethodsViewModel.DeliverySteps[1]);

        deliveryMethodsViewModel.DeliverySteps.Select(step => step.StepOrder).Should().Equal([1, 2]);
    }

    private DeliveryMethodsViewModel DeliveryMethodsViewModel => new(new Mock<IMediator>().Object)
    {
        Deliverymen =
        [
            new DeliverymanListDto(1, "Deliveryman 1", "Riyadh", "+966 500000001"),
            new DeliverymanListDto(2, "Deliveryman 2", "Jeddah", "+966 500000002"),
            new DeliverymanListDto(3, "Deliveryman 3", "Dammam", "+966 500000003")
        ],
        ShippingCarriers =
        [
            new ShippingCarrierListDto(1, "ShippingCarrier 1", 10, "+966 511111111", "Riyadh - Street 1"),
            new ShippingCarrierListDto(2, "ShippingCarrier 2", 15, "+966 522222222", "Jeddah - Street 2"),
            new ShippingCarrierListDto(3, "ShippingCarrier 3", 20, "+966 533333333", "Dammam - Street 3")
        ]
    };
}
