using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.List;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Products.Index;

public partial class ViewModel : IndexViewModelBase<ProductItem>
{
    private ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> _subCategories = new();
    private readonly IProductStore _productStore;
    public ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> SubCategories
    {
        get => _subCategories;
        set => SetProperty(ref _subCategories, value);
    }
    public ViewModel(IMediator mediator, IProductStore productStore) : base(mediator)
    {
        WeakReferenceMessenger.Default.Register<Application.Messages.Products.ProductedCreatedMessage>(this, async (r, m) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Products.ProductedDeletedMessage>(this, async (r, m) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Products.ProductedUpdatedMessage>(this, async (r, m) => await ReloadAsync());
        _productStore = productStore;
    }

    protected override async Task DeleteAsync(ProductItem dto)
    {
        if (!DialogService.Instance.Confirm($"هل تريد حذف المنتج( {dto.Name})"))
        {
            return;
        }

        Result result = await _mediator.Send(new Application.Features.Products.Delete.DeleteProductCommand(dto.Id));
        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم حذف المنتج");
            WeakReferenceMessenger.Default.Send(new Application.Messages.Products.ProductedDeletedMessage(dto.Id));

        }
        else
        {
            NotificationService.Instance.ShowError("خطأ أثناء حذف المنتج");
        }
    }

    protected override async Task LoadAsync()
    {
        IEnumerable<ListProducts.ProductDto> products = await _mediator.Send(new Application.Features.Products.List.ListProducts.Query());

        Products = products.Select(p => new ProductItem(
            p.Id,
            p.Name,
            p.Price,
            p.Code,
            p.CategoryName,
            p.Suppliers.Select(s => new ProductSupplierItem(s.Id, s.Name)).ToList()));

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

    async partial void OnFilterChanged(bool value)
    {
        if (value is false)
        {
            SelectedCategory = null;
            SubCategories.Clear();
            var products = await _mediator.Send(new Application.Features.Products.List.ListProducts.Query());
            Products = products.Select(p => new ProductItem(
                p.Id,
                p.Name,
                p.Price,
                p.Code,
                p.CategoryName,
                p.Suppliers.Select(s => new ProductSupplierItem(s.Id, s.Name)).ToList()));
        }
    }

    async partial void OnSelectedCategoryChanged(CategoryInfoDto value)
    {
        if (value is null)
        {
            await ReloadAsync();
            return;
        }
        
        var products = await _productStore.GetProductByCategoryAsync(value.Id);
        Products = products.Select(p => new ProductItem(
                p.Id,
                p.Name,
                p.Price,
                p.Code,
                p.CategoryName,
                p.Suppliers.Select(s => new ProductSupplierItem(s.Id, s.Name)).ToList()));
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
        var products = await _mediator.Send(new Application.Features.Products.List.ListProducts.Query());
        Products = products.Select(p => new ProductItem(
            p.Id,
            p.Name,
            p.Price,
            p.Code,
            p.CategoryName,
            p.Suppliers.Select(s => new ProductSupplierItem(s.Id, s.Name)).ToList()));
    }

    protected override async Task ShowEditAsync(ProductItem model)
    {
        await DialogService.Instance.ShowDialog<Update.View>("تعديل بيانات منتج", model.Id);
    }

    protected override async Task ShowCreateAsync()
    {
        await DialogService.Instance.ShowDialog<Create.View>("إضافة منتج جديد");
    }

    [ObservableProperty]
    private IEnumerable<ProductItem> _products;

    [ObservableProperty]
    private string _searchTerm;

    async partial void OnSearchTermChanged(string oldValue, string newValue)
    {
        await SearchProducts(newValue);
    }

    private async Task SearchProducts(string searchTerm)
    {
        var products = await _productStore.GetProductsByName(searchTerm);
        Products = products.Select(p => new ProductItem(
            p.Id,
            p.Name,
            p.Price,
            p.Code,
            p.CategoryName,
            p.Suppliers.Select(s => new ProductSupplierItem(s.Id, s.Name)).ToList()));
    }

}

public record ProductItem(
        int Id,
        string Name,
        decimal Price,
        string Code,
        string CategoryName,
        IReadOnlyCollection<ProductSupplierItem> Suppliers);

public record ProductSupplierItem(
        int Id,
        string Name);