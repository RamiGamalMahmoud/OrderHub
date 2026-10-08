using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Features.Setup.Properties.Create;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Exceptions;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Application;

public class PropertyTests
{
    [Fact]
    public async Task Should_Create_Property()
    {
        var repository = new Mock<IPropertyRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        Property added = null;
        repository.Setup(x => x.Add(It.IsAny<Property>())).Callback<Property>(property => added = property);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => typeof(ModelBase).GetProperty(nameof(ModelBase.Id)).SetValue(added, 1)).ReturnsAsync(1);
        using var cancellation = new CancellationTokenSource();
        var handler = new CreatePropertyCommandHandler(repository.Object, unitOfWork.Object);
        var id = await handler.Handle(new("Color", PropertyType.List, "Description", [new(1, "Red"), new(2, "Blue")]), cancellation.Token);
        Assert.Equal(1, id);
        Assert.Equal("Color", added.Name);
        Assert.Equal(new[] { "Red", "Blue" }, added.Options.Select(option => option.Value));
        repository.Verify(x => x.Add(It.IsAny<Property>()), Times.Once);
        repository.Verify(x => x.ExistsByNameAsync("Color", cancellation.Token), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Duplicate_property_does_not_add_or_commit()
    {
        var repository = new Mock<IPropertyRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(x => x.ExistsByNameAsync("Color", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreatePropertyCommandHandler(repository.Object, unitOfWork.Object);
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new("Color", PropertyType.Text, "", []), default));
        repository.Verify(x => x.Add(It.IsAny<Property>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_commit_preserves_the_original_failure_for_the_caller(bool create)
    {
        var repository = new Mock<IPropertyRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var originalFailure = new InvalidOperationException("Original provider diagnostics");
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PersistenceException(originalFailure.Message, originalFailure));
        repository.Setup(x => x.GetForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Property.Create("Color", PropertyType.Text));
        Task operation = create
            ? new CreatePropertyCommandHandler(repository.Object, unitOfWork.Object).Handle(new("Color", PropertyType.Text, "", []), default)
            : new UpdatePropertyCommandHandler(repository.Object, unitOfWork.Object).Handle(new(new(1, "Color", "", PropertyType.Text, [])), default);
        Assert.Same(originalFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => operation));
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
