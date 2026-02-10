using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.UI.Features.Products.Index
{
    public partial class ViewModel : IndexViewModelBase<ProductListDto>
    {
        private readonly IDialogService _dialogService;
        private readonly ISelectionStore<IProductMarker, int> _selectionStore;

        public ViewModel(IMediator mediator, IDialogService dialogService, ISelectionStore<IProductMarker, int> selectionStore, IMessenger messenger) : base(mediator, messenger)
        {
            _dialogService = dialogService;
            _selectionStore = selectionStore;

            _messenger.Register<Application.Messages.Products.ProductedCreatedMessage>(this, async (r, m) => await ReloadAsync());
            _messenger.Register<Application.Messages.Products.ProductedDeletedMessage>(this, async (r, m) => await ReloadAsync());
            _messenger.Register<Application.Messages.Products.ProductedUpdatedMessage>(this, async (r, m) => await ReloadAsync());
        }

        protected override async Task DeleteAsync(ProductListDto dto)
        {
            if (!_dialogService.Confirm($"هل تريد حذف المنتج( {dto.Name})"))
            {
                return;
            }

            Result result = await _mediator.Send(new Application.Commands.ProductCommands.DeleteProductCommand(dto.Id));
            if (result.IsSuccess)
            {
                await _mediator.Publish(new Application.Notifications.SuccessNotification("تم حذف المنتج"));
                _messenger.Send(new Application.Messages.Products.ProductedDeletedMessage(dto.Id));
                
            }
            else
            {
                await _mediator.Publish(new Application.Notifications.ErrorNotification("خطأ أثناء حذف المنتج"));
            }
        }

        protected override async Task LoadAsync()
        {
            Products = await _mediator.Send(new Application.Queries.ProductQueries.GetAllProductsQuery());
        }

        protected override async Task ReloadAsync()
        {
            Products = await _mediator.Send(new Application.Queries.ProductQueries.GetAllProductsQuery());
        }

        protected override Task ShowEditAsync(ProductListDto model)
        {
            _selectionStore.Id = model.Id;
            _dialogService.ShowDialog<Update.View>();
            return Task.CompletedTask;
        }

        protected override Task ShowCreateAsync()
        {
            _dialogService.ShowDialog<Create.View>();
            return Task.CompletedTask;
        }

        [ObservableProperty]
        private IEnumerable<ProductListDto> _products;

    }
}
