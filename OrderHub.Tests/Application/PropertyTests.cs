using FluentAssertions;
using Moq;
using OrderHub.Application.Features.Setup.Properties.Create;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests.Application;

public class PropertyTests
{
    [Fact]
    public async Task Should_Create_Property()
    {
        Mock<IPropertyStore> propertyStore = new Mock<IPropertyStore>();

        propertyStore.Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        propertyStore.Setup(x => x.CreateAsync(It.IsAny<Property>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreatePropertyCommandHandler(propertyStore.Object);

        var command = new CreatePropertyCommand(
            "Color",
            OrderHub.Domain.Enums.PropertyType.List,
            "Description", 
            [
                new PropertyOptionCreateDto(1, "Red"),
                new PropertyOptionCreateDto(2, "Blue")
            ]);

        int id = await handler.Handle(command, CancellationToken.None);

        id.Should().Be(1);
        
        propertyStore.Verify(
            x => x.CreateAsync(It.IsAny<Property>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
