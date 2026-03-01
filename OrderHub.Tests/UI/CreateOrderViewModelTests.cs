using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using MediatR;
using Moq;
using OrderHub.UI.Interfaces;
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
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new Domain.Enums.EnumItem<Domain.Enums.DeliveryMethod>()
        {
            Value = Domain.Enums.DeliveryMethod.ShippingCompany
        };
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
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new Domain.Enums.EnumItem<Domain.Enums.DeliveryMethod>()
        {
            Value = Domain.Enums.DeliveryMethod.ShippingCompany
        };

        viewModel.DeliveryMethodsViewModel.SelectedShippingCarrier = new Application.DTOs.CommonDtos.ShippingCarrierInfoDto(1, "Shipping Company 1");
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
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new Domain.Enums.EnumItem<Domain.Enums.DeliveryMethod>()
        {
            Value = Domain.Enums.DeliveryMethod.Pickup
        };
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
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new Domain.Enums.EnumItem<Domain.Enums.DeliveryMethod>()
        {
            Value = Domain.Enums.DeliveryMethod.Pickup
        };
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod.Should().NotBeNull();

        bool isEventFired = false;
        viewModel.DeliveryMethodsViewModel.ErrorsChanged += (sender, e) => { isEventFired = true; };
        viewModel.DeliveryMethodsViewModel.SelecteddDeliveryMethod = new Domain.Enums.EnumItem<Domain.Enums.DeliveryMethod>()
        {
            Value = Domain.Enums.DeliveryMethod.DeliveryMan
        };
        isEventFired.Should().BeTrue();
    }

    private CreateOrderViewModel CreateOrderViewModel => new CreateOrderViewModel(_mediatorMock.Object, _messengerMock.Object, _dialogServiceMock.Object);
}
