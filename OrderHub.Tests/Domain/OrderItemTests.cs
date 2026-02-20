//using FluentAssertions;
//using OrderHub.Domain.Common;
//using OrderHub.Domain.Models;

//namespace OrderHub.Tests.Domain
//{
//    public class OrderItemTests
//    {
//        [Fact]
//        public void OrderItemShouldBeCreated()
//        {
//            Category category = new Category("Category 1");
//            Product product = new Product("Product 1", "000-00-00", category, new OrderHub.Domain.ValueObjects.Money(20.0M));

//            Result<OrderItem> orderItemCreatResult = OrderItem.Create(product, 2);
//            OrderItem orderItem = orderItemCreatResult.Value;

//            orderItemCreatResult.IsSuccess.Should().BeTrue();
//            orderItem.SubTotal.Value.Should().Be(40.0M);
//        }

//        [Fact]
//        public void OrderItemQuantityShouldBeIncreased()
//        {
//            Category category = new Category("Category 1");
//            Product product = new Product("Product 1", "000-00-00", category, new OrderHub.Domain.ValueObjects.Money(20.0M));

//            Result<OrderItem> orderItemCreatResult = OrderItem.Create(product, 2);
//            OrderItem orderItem = orderItemCreatResult.Value;

//            orderItem.IncreaseQuantity(2);
//            orderItem.SubTotal.Value.Should().Be(80);
//        }

//        [Fact]
//        public void OrderItemQuantitySouldBeDecreased()
//        {
//            Category category = new Category("Category 1");
//            Product product = new Product("Product 1", "000-00-00", category, new OrderHub.Domain.ValueObjects.Money(20.0M));

//            Result<OrderItem> orderItemCreatResult = OrderItem.Create(product, 2);
//            OrderItem orderItem = orderItemCreatResult.Value;

//            orderItem.DecreaseQuantity(1);
//            orderItem.SubTotal.Value.Should().Be(20);
//        }

//        [Fact]
//        public void OrderItemQuantityShouldBeUpdated()
//        {
//            Category category = new Category("Category 1");
//            Product product = new Product("Product 1", "000-00-00", category, new OrderHub.Domain.ValueObjects.Money(20.0M));

//            Result<OrderItem> orderItemCreatResult = OrderItem.Create(product, 2);
//            OrderItem orderItem = orderItemCreatResult.Value;

//            orderItem.UpdateQuantity(5);
//            orderItem.SubTotal.Value.Should().Be(100);
//        }

//        [Theory]
//        [InlineData(10.0, 1, 10.0)]
//        [InlineData(10.0, 2, 20.0)]
//        [InlineData(10.0, 3, 30.0)]
//        public void OrderItemQuantityShouldBeUpdatedMany(decimal unitPrice, int quantity, decimal totalPrice)
//        {
//            Category category = new Category("Category 1");
//            Product product = new Product("Product 1", "000-00-00", category, new OrderHub.Domain.ValueObjects.Money(unitPrice));

//            Result<OrderItem> orderItemCreatResult = OrderItem.Create(product, quantity);
//            OrderItem orderItem = orderItemCreatResult.Value;
//            orderItem.SubTotal.Value.Should().Be(totalPrice);
//            orderItem.UpdateQuantity(5);
//            orderItem.SubTotal.Value.Should().Be(unitPrice * 5);
//        }
//    }
//}
