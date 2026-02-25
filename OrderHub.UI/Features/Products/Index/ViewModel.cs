using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.UI.Features.Products.Index
{
    public partial class ViewModel : IndexViewModelBase<ProductListDto>
    {
        private readonly IDialogService _dialogService;
        private readonly ISelectionStore<IProductMarker, int> _selectionStore;
        private ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> _subCategories = new();
        public ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> SubCategories
        {
            get => _subCategories;
            set => SetProperty(ref _subCategories, value);
        }
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
            RootCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetRootCategoriesQuery());
        }

        async partial void OnSelectedCategoryChanging(CategoryInfoDto oldValue, CategoryInfoDto newValue)
        {
            if (newValue is null)
            {
                SubCategories.Clear();
                return;
            }

            if (newValue.ParentId is null)
            {
                SubCategories.Clear();
            }
            else
            {
                RemoveSubCategoriesAfterParent((int)newValue.ParentId);
            }

            IEnumerable<CategoryInfoDto> subCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetSubCategoriesQuery(newValue.Id));

            bool exists = _subCategories.Any(s => s.Key.Id == newValue.Id);

            if (subCategories.Any() && !exists)
            {
                SubCategories.Add(new KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>(newValue, subCategories));
            }
        }

        [ObservableProperty]
        private bool _filter;

        async partial void OnFilterChanged(bool oldValue, bool newValue)
        {
            if (newValue is false)
            {
                SelectedCategory = null;
                SubCategories.Clear();
                Products = await _mediator.Send(new Application.Queries.ProductQueries.GetAllProductsQuery());
            }
        }

        async partial void OnSelectedCategoryChanged(CategoryInfoDto oldValue, CategoryInfoDto newValue)
        {
            if (newValue is null)
            {
                Products = await _mediator.Send(new Application.Queries.ProductQueries.GetAllProductsQuery());
                return;
            }
            Products = await _mediator.Send(new Application.Queries.ProductQueries.GetProductsByCategoryQuery(newValue.Id));
        }

        private void RemoveSubCategoriesAfterParent(int parentId)
        {
            List<int> ids = SubCategories.Select(c => c.Key.Id).ToList();

            int parentIndex = ids.IndexOf(parentId);

            if (parentIndex + 1 < ids.Count)
            {
                for (int i = parentIndex + 1; i < ids.Count; i++)
                {
                    SubCategories.RemoveAt(i);
                }
            }
        }

        [ObservableProperty]
        private IEnumerable<CategoryInfoDto> _rootCategories;

        [ObservableProperty]
        private CategoryInfoDto _selectedCategory;

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

        [ObservableProperty]
        private string _searchTerm;

        async partial void OnSearchTermChanged(string oldValue, string newValue)
        {
            await SearchProducts(newValue);
        }

        private async Task SearchProducts(string searchTerm)
        {
            IEnumerable<ProductListDto> products = await _mediator.Send(new Application.Queries.ProductQueries.GetProductsByNameQuery(searchTerm));
            Products = products;
        }

    }
}
