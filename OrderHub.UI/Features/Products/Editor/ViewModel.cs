using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        protected ViewModel(IMediator mediator)
        {
            _mediator = mediator;
            _notifyPropertiesNames = [nameof(Name), nameof(SelectedCategory), nameof(PriceText), nameof(Code)];
            ValidateAllProperties();
        }

        public virtual async Task LoadDataAsync()
        {
            Categories = await _mediator.Send(new Application.Queries.CommonQueries.GetCategoriesInfoQuery());
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
