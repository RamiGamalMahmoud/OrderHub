using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Products.Editor
{
    public abstract partial class ViewModel : EditorViewModelBase
    {
        protected readonly IMediator _mediator;
        protected readonly IMessenger _messenger;

        protected ViewModel(IMediator mediator, IMessenger messenger)
        {
            _mediator = mediator;
            _messenger = messenger;
            _notifyPropertiesNames = [nameof(Name), nameof(SelectedCategory), nameof(PriceText), nameof(Code)];
            ValidateAllProperties();
            RegisterMessages();
        }

        private void RegisterMessages()
        {
            _messenger.Register<Application.Messages.Categories.CategoryCreatedMessage>(this, async (_, _) => await RefreshCategoriesAsync());
            _messenger.Register<Application.Messages.Categories.CategoryUpdatedMessage>(this, async (_, _) => await RefreshCategoriesAsync());
            _messenger.Register<Application.Messages.Suppliers.SupplierCreatedMessage>(this, async (_, _) => await RefreshSuppliersAsync());
            _messenger.Register<Application.Messages.Suppliers.SupplierUpdatedMessage>(this, async (_, _) => await RefreshSuppliersAsync());
        }

        public virtual async Task LoadDataAsync()
        {
            await RefreshCategoriesAsync();
            await RefreshSuppliersAsync();
        }

        private async Task RefreshCategoriesAsync()
            => Categories = await _mediator.Send(new Application.Queries.CommonQueries.GetCategoriesInfoQuery());

        private async Task RefreshSuppliersAsync()
        {
            IEnumerable<SupplierInfoDto> suppliers = await _mediator.Send(new Application.Queries.CommonQueries.GetSuppliersInfoQuery());
            Suppliers = suppliers.Select(s => new SelectableObject<SupplierInfoDto>(s)).ToList();
        }

        [RelayCommand]
        private void SelectSupplier() => HasChanges = true;

        partial void OnPriceTextChanged(string oldValue, string newValue)
        {
            if(decimal.TryParse(newValue, out decimal price))
            {
                Price = price;
            }
        }

        [ObservableProperty]
        [Required(ErrorMessage = "اسم المنتج مطلوب")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _name;

        [ObservableProperty]
        [Required(ErrorMessage = "القسم مطلوب")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private CategoryInfoDto _selectedCategory;

        [ObservableProperty]
        [Required(ErrorMessage = "السعر مطلوب")]
        [RegularExpression(@"^\d+([.,]\d{0,2})?$", ErrorMessage = "السعر غير صالح")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _priceText;

        [ObservableProperty]
        [Required(ErrorMessage = "كود المنتج مطلوب")]
        [RegularExpression(@"^\d{3}-\d{2}-\d{2}", ErrorMessage = "كود المنتج يتكون من الصورة 000-00-00")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _code;

        public decimal Price { get; private set; }

        [ObservableProperty]
        private IEnumerable<SelectableObject<SupplierInfoDto>> _suppliers;

        [ObservableProperty]
        private IEnumerable<CategoryInfoDto> _categories;
    }
}
