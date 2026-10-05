using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Setup.Properties.Create;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Domain.Enums;

namespace OrderHub.Tests.Infrastructure.Stores;

public class PropertyPersistenceTests
{
    [Fact]
    public async Task CreateAsync_Should_Save_Property()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var id = await mediator.Send(new CreatePropertyCommand("Property 1", PropertyType.Text, "Description", []));
        Assert.Equal(1, id);
        Assert.Single(await mediator.Send(new GetPropertiesQuery()));
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Property()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var id = await mediator.Send(new CreatePropertyCommand("Property 1", PropertyType.List, "This is a list property",
            [new(1, "option 1"), new(2, "option 2"), new(3, "option 5")]));
        var original = await mediator.Send(new GetPropertyQuery(id));
        await mediator.Send(new UpdatePropertyCommand(new(id, "New Property Name", "New Description", PropertyType.List,
            [new(original.Options.ElementAt(0).Id, "Option 1"), new(original.Options.ElementAt(2).Id, "Option 3"), new(null, "Option 4")])));
        var updated = await mediator.Send(new GetPropertyQuery(id));
        Assert.Equal("Option 3", updated.Options.ElementAt(1).Value);
        Assert.Equal(3, updated.Options.Count);
        Assert.Equal("New Property Name", updated.Name);
        Assert.Equal("New Description", updated.Description);
    }
}
