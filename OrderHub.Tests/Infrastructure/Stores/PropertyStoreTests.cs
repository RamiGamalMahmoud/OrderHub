using FluentAssertions;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Stores;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests.Infrastructure.Stores
{
    public class PropertyStoreTests
    {
        [Fact]
        public async Task CreateAsync_Should_Save_Property()
        {
            PropertyStore store = new PropertyStore(TestDb.CreateFactory());
            
            Property property = Property.Create(
                "Property 1", 
                OrderHub.Domain.Enums.PropertyType.Text, 
                "Description");
            
            int id =  await store.CreateAsync(property);

            id.Should().Be(1);
            
            var properties = await store.GetAllAsync(CancellationToken.None);
            
            properties.Should().HaveCount(1);
        }

        [Fact]
        public async Task UpdateAsync_Should_Update_Property()
        {
            PropertyStore store = new PropertyStore(TestDb.CreateFactory());

            Property property = Property.Create(
                "Property 1",
                OrderHub.Domain.Enums.PropertyType.List,
                "This is a list property",
                ["option 1", "option 2", "option 5"]);

            int propertyId = await store.CreateAsync(property);
            
            PropertyDetailsDto storedProperty = await store.GetByIdAsync(propertyId);

            // remove option
            // rename option
            // add option
            PropertyUpdateDto updateDto = new PropertyUpdateDto(
                storedProperty.Id,
                "New Property Name",
                "New Description",
                OrderHub.Domain.Enums.PropertyType.List,
                [
                    new PropertyOptionUpdateDto(1, "Option 1"),
                    new PropertyOptionUpdateDto(3, "Option 3"),
                    new PropertyOptionUpdateDto(null, "Option 4")
                    ]);

            await store.UpdateAsync(updateDto);
            PropertyDetailsDto updatedProperty = await store.GetByIdAsync(propertyId);

            updatedProperty.Options.ElementAt(1).Value.Should().Be("Option 3");
            updatedProperty.Options.Count.Should().Be(3);
        }
    }
}
