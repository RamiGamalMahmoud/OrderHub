using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Features.Products.Editor;

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
        CategorySelection = new CategorySelection(mediator);
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
        await CategorySelection.LoadRootCategoriesAsync();

        var properties = (await _mediator.Send(new Application.Features.Setup.Properties.GetAll.GetPropertiesQuery()))
            .Select(x => new PropertyItem()
            {
                Id = x.Id,
                Name = x.Name,
                PropertyType = x.Type.ToString()
            });

        _properties.Clear();
        foreach(var property in properties)
        {
            _properties.Add(property);
            property.PropertyChanged += OnPropertyChanged;
        }

        PropertyRequirements = Enum.GetValues<PropertyRequirement>()
        .Select(x => new EnumItem<PropertyRequirement>(x, x.GetDescription()));
        OnPropertyChanged(nameof(Properties));
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        HasChanges = true;
    }

    public CategorySelection CategorySelection { get; }
    public bool IsFullPathVisible => SelectedCategory is not null;

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
    [MinLength(2, ErrorMessage = "الاسم يجب أن يكون أكثر من 2 حرف")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    [Required(ErrorMessage = "القسم مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private CategoryInfoDto _selectedCategory;

    partial void OnSelectedCategoryChanged(CategoryInfoDto oldValue, CategoryInfoDto newValue)
    {
        CategorySelection.SelectedCategory = newValue;
    }

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

    private Collection<PropertyItem> _properties = [];
    public IReadOnlyCollection<PropertyItem> Properties => _properties.AsReadOnly();
    public IEnumerable<EnumItem<PropertyRequirement>> PropertyRequirements { get; private set; }

    [RelayCommand]
    private static void AddProperty(PropertyItem property) => property.IsAssigned = true;

    [RelayCommand]
    private static void RemoveProperty(PropertyItem property) => property.IsAssigned = false;
}

public partial class PropertyItem : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PropertyType { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBeAdded))]
    private EnumItem<PropertyRequirement> _propertyRequirement;

    [ObservableProperty]
    private bool _isAssigned;

    public bool CanBeAdded => PropertyRequirement is not null;
}

public enum PropertyRequirement
{
    [Description("اختيارية")]
    Optional,

    [Description("مطلوبة")]
    Required
}