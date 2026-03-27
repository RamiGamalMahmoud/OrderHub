using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using MediatR;
using Moq;
using OrderHub.Domain.Enums;
using OrderHub.UI.Interfaces;
using System.Linq;
using CreateOrderViewModel = OrderHub.UI.Features.Orders.Create.ViewModel;

namespace OrderHub.Tests.UI;

public class CreateOrderViewModelTests
{
    private readonly Mock<IMessenger> _messengerMock = new Mock<IMessenger>();
    private readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>();
    private readonly Mock<IDialogService> _dialogServiceMock = new Mock<IDialogService>();

    [Fact]
    public void CreateViewModelSaveCommand_ShouldBeDisabled_WhenOrderItemsIsEmpty()
    {
        CreateOrderViewModel viewModel = CreateOrderViewModel;
        viewModel.SelectedClient = new Application.DTOs.ClientDtos.ClientListDto(1, "Client 1", "Address 1", "+966 123456789");
        viewModel.OrderBuilder.ItemsCount.Should().Be(0);
        viewModel.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CreateViewModelSaveCommand_ShouldBeDisabled_WhenNoClientIsSelected()
    {
        CreateOrderViewModel viewModel = CreateOrderViewModel;
        viewModel.OrderBuilder.AddItem(new OrderHub.UI.Features.Orders.OrderItemViewModel()
        {
            CategoryName = "Category 1",
            ProductName = "Product 1",
            Price = 10,
            ProductId = 1,
            Quantity = 1
        });
        viewModel.OrderBuilder.ItemsCount.Should().Be(1);
        viewModel.SaveCommand.CanExecute(null).Should().BeFalse();
    }


    [Fact]
    public void CreateViewModelSaveCommand_ShouldBeDisabled_WhenDeliveryMethodIsShippingAndNoShippingCarrierIsSelected()
    {
        CreateOrderViewModel viewModel = CreateOrderViewModel;
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new EnumItem<DeliveryMethod>(Domain.Enums.DeliveryMethod.ShippingCompany, "");
        viewModel.DeliveryMethodsViewModel.HasErrors.Should().BeTrue();

        viewModel.SelectedClient = new Application.DTOs.ClientDtos.ClientListDto(1, "Client 1", "Address 1", "+966 123456789");
        viewModel.OrderBuilder.AddItem(new OrderHub.UI.Features.Orders.OrderItemViewModel()
        {
            CategoryName = "Category 1",
            ProductName = "Product 1",
            Price = 10,
            ProductId = 1,
            Quantity = 1
        });

        viewModel.OrderBuilder.ItemsCount.Should().Be(1);
        viewModel.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CreateViewModelSaveCommand_ShouldBeEnabled_WhenDeliveryMethodIsShippingAndShippingCarrierIsSelected()
    {
        CreateOrderViewModel viewModel = CreateOrderViewModel;
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new EnumItem<DeliveryMethod>(Domain.Enums.DeliveryMethod.ShippingCompany, "");

        viewModel.DeliveryMethodsViewModel.SelectedShippingCarrier = new Application.DTOs.ShippingCarriersDtos.ShippingCarrierListDto(1, "Shipping Company 1", 25, "+966 555555555", "City - Street");
        viewModel.DeliveryMethodsViewModel.HasErrors.Should().BeFalse();

        viewModel.SelectedClient = new Application.DTOs.ClientDtos.ClientListDto(1, "Client 1", "Address 1", "+966 123456789");
        viewModel.OrderBuilder.AddItem(new OrderHub.UI.Features.Orders.OrderItemViewModel()
        {
            CategoryName = "Category 1",
            ProductName = "Product 1",
            Price = 10,
            ProductId = 1,
            Quantity = 1
        });

        viewModel.OrderBuilder.ItemsCount.Should().Be(1);
        viewModel.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CreateViewModelSaveCommand_ShouldBeEnabled_WhenOrderIsValid()
    {
        CreateOrderViewModel viewModel = CreateOrderViewModel;
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new EnumItem<DeliveryMethod>(Domain.Enums.DeliveryMethod.Pickup, "");
        viewModel.SelectedClient = new Application.DTOs.ClientDtos.ClientListDto(1, "Client 1", "Address 1", "+966 123456789");
        viewModel.OrderBuilder.AddItem(new OrderHub.UI.Features.Orders.OrderItemViewModel()
        {
            CategoryName = "Category 1",
            ProductName = "Product 1",
            Price = 10,
            ProductId = 1,
            Quantity = 1
        });

        viewModel.OrderBuilder.ItemsCount.Should().Be(1);
        viewModel.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CreateViewModelDeliveryMethods_ShouldFireEvent_WhenDeliveryMethodIsChanged()
    {
        CreateOrderViewModel viewModel = CreateOrderViewModel;
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new EnumItem<DeliveryMethod>(Domain.Enums.DeliveryMethod.Pickup, "");
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod.Should().NotBeNull();

        bool isEventFired = false;
        viewModel.DeliveryMethodsViewModel.ErrorsChanged += (sender, e) => { isEventFired = true; };
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new EnumItem<DeliveryMethod>(Domain.Enums.DeliveryMethod.ShippingCompany, "");
        isEventFired.Should().BeTrue();
    }

    [Fact]
    public void CreateViewModelSaveCommand_ShouldBeDisabled_WhenDeliveryMethodIsDeliveryChainAndHasNoSteps()
    {
        CreateOrderViewModel viewModel = CreateOrderViewModel;

        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new EnumItem<DeliveryMethod>(Domain.Enums.DeliveryMethod.DeliveryChain, "");
        viewModel.SelectedClient = new Application.DTOs.ClientDtos.ClientListDto(1, "Client 1", "Address 1", "+966 123456789");
        viewModel.OrderBuilder.AddItem(new OrderHub.UI.Features.Orders.OrderItemViewModel()
        {
            CategoryName = "Category 1",
            ProductName = "Product 1",
            Price = 10,
            ProductId = 1,
            Quantity = 1
        });

        viewModel.DeliveryMethodsViewModel.HasErrors.Should().BeTrue();
        viewModel.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CreateViewModelSaveCommand_ShouldBeEnabled_WhenDeliveryMethodIsDeliveryChainAndAllStepsHaveHandlers()
    {
        CreateOrderViewModel viewModel = CreateOrderViewModel;

        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new EnumItem<DeliveryMethod>(Domain.Enums.DeliveryMethod.DeliveryChain, "");
        viewModel.DeliveryMethodsViewModel.AddDeliverymanStepCommand.Execute(null);
        viewModel.DeliveryMethodsViewModel.AddShippingCarrierStepCommand.Execute(null);
        viewModel.DeliveryMethodsViewModel.DeliverySteps[0].SelectedHandler = viewModel.DeliveryMethodsViewModel.DeliverySteps[0].Handlers.First();
        viewModel.DeliveryMethodsViewModel.DeliverySteps[1].SelectedHandler = viewModel.DeliveryMethodsViewModel.DeliverySteps[1].Handlers.First();

        viewModel.SelectedClient = new Application.DTOs.ClientDtos.ClientListDto(1, "Client 1", "Address 1", "+966 123456789");
        viewModel.OrderBuilder.AddItem(new OrderHub.UI.Features.Orders.OrderItemViewModel()
        {
            CategoryName = "Category 1",
            ProductName = "Product 1",
            Price = 10,
            ProductId = 1,
            Quantity = 1
        });

        viewModel.DeliveryMethodsViewModel.HasErrors.Should().BeFalse();
        viewModel.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    private CreateOrderViewModel CreateOrderViewModel => new CreateOrderViewModel(_mediatorMock.Object, _messengerMock.Object, _dialogServiceMock.Object)
    {
        DeliveryMethodsViewModel =
        {
            Deliverymen =
            [
                new Application.DTOs.DeliverymanDtos.DeliverymanListDto(1, "Deliveryman 1", "Riyadh", "+966 500000001"),
                new Application.DTOs.DeliverymanDtos.DeliverymanListDto(2, "Deliveryman 2", "Jeddah", "+966 500000002")
            ],
            ShippingCarriers =
            [
                new Application.DTOs.ShippingCarriersDtos.ShippingCarrierListDto(1, "Shipping Company 1", 25, "+966 511111111", "Riyadh - Street 1"),
                new Application.DTOs.ShippingCarriersDtos.ShippingCarrierListDto(2, "Shipping Company 2", 30, "+966 522222222", "Jeddah - Street 2")
            ]
        }
    };
}
