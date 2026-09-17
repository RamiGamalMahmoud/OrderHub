using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Moq;
using OrderHub.Application.DTOs;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Queries;
using OrderHub.UI.Features.Orders.Index;
using OrderHub.UI.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests.UI;

public class CrudRefreshTests
{
    [Fact]
    public void CategoryDeletionReloadsRootAndClearsDeletedSelection()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<IRequest<IEnumerable<CategoryDtos.CategoryListDto>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new CategoryDtos.CategoryListDto(2, "Remaining", true, 0, 0, null) });
        var viewModel = new OrderHub.UI.Features.Categories.Index.ViewModel(mediator.Object)
        {
            SelectedCategory = new CategoryDtos.CategoryListDto(1, "Deleted", true, 0, 0, null)
        };
        try
        {
            WeakReferenceMessenger.Default.Send(new CategoriesMessages.CategoryDeletedMessage(1));

            Assert.Null(viewModel.SelectedCategory);
            Assert.Empty(viewModel.SelectedCategoriesList);
            Assert.Equal(2, Assert.Single(viewModel.CategoriesList).Id);
            mediator.Verify(x => x.Send(
                It.Is<IRequest<IEnumerable<CategoryDtos.CategoryListDto>>>(q =>
                    q is CategoryQueries.GetCategoryListQuery && ((CategoryQueries.GetCategoryListQuery)q).ParentId == null),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(viewModel);
        }
    }

    [Fact]
    public async Task DraftDeletionReloadsTheDrawerFromTheService()
    {
        var drafts = new Mock<IDraftService>();
        drafts.Setup(x => x.GetAllAsync()).ReturnsAsync(Array.Empty<OrderDraftSummary>());
        var viewModel = new OrderDraftsDrawerViewModel(drafts.Object);
        try
        {
            await viewModel.LoadAsync();
            drafts.Invocations.Clear();

            WeakReferenceMessenger.Default.Send(new OrdersMessages.OrderDraftDeletedMessage(Guid.NewGuid()));

            drafts.Verify(x => x.GetAllAsync(), Times.Once);
            Assert.Empty(viewModel.Drafts);
            Assert.False(viewModel.IsLoading);
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(viewModel);
        }
    }
}
