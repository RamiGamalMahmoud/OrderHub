using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Features.Products.Editor;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Products.Update;

public class ViewModel : Editor.ViewModel, IParameterizedViewModel
{
    private int _productId;
    public ViewModel(IMediator mediator) : base(mediator)
    {
        HasChanges = false;
    }

    public override string Title => "تعديل بيانات منتج";

    public Task Initialize(object parameter)
    {
        System.ArgumentNullException.ThrowIfNull(parameter);
        _productId = (int) parameter;
        if(_productId == 0)
        {
            throw new System.Exception();
        }

        return Task.CompletedTask;
    }

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();

        GetProduct.ProductDetails product = await _mediator.Send(new GetProduct.Query(_productId));

        Name = product.Name;
        Code = product.Code;
        PriceText = product.Price.ToString();
        SelectedCategory = (await _mediator.Send(new Application.Queries.CommonQueries.GetCategoriesInfoQuery())).Where(c => c.Id == product.CategoryId).FirstOrDefault();

        foreach (GetProduct.ProductProperty prop in product.ProductProperties)
        {
            var assigned = Properties.Where(p => p.Id == prop.PropertyId).FirstOrDefault();
            if(assigned is not null)
            {
                assigned.IsAssigned = true;
                PropertyRequirement propertyRequirement = prop.isRequired ? PropertyRequirement.Required : PropertyRequirement.Optional;
                assigned.PropertyRequirement = PropertyRequirements.Where(r => r.Value == propertyRequirement).FirstOrDefault();
            }
        }


        foreach (int supplierId in product.SelectedSuppliersIds)
        {
            Suppliers.Where(supplier => supplier.Value.Id == supplierId).FirstOrDefault().IsSelected = true;
        }

        HasChanges = false;
    }

    protected override async Task Save()
    {
        IEnumerable<int> selectedSuppliersIds = Suppliers
            .Where(supplier => supplier.IsSelected)
            .Select(supplier => supplier.Value.Id);

        UpdateProduct.ProductDto product = new UpdateProduct.ProductDto(
            Name,
            Code,
            Price,
            CategorySelection.SelectedCategory.Id,
            selectedSuppliersIds,
            Properties
            .Where(p => p.IsAssigned)
            .Select(p => new UpdateProduct.ProductPropertiesDto(p.Id, p.PropertyRequirement.Value == Editor.PropertyRequirement.Required)));

        Result result = await _mediator.Send(new UpdateProduct.Command(_productId, product));

        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم تحديث بيانات المنتج");
            WeakReferenceMessenger.Default.Send(new Application.Messages.Products.ProductedUpdatedMessage());
            OnRequestClose();
        }
        else
        {
            NotificationService.Instance.ShowError("خطأ أثناء تحديث بيانات المنتج");
        }
    }
}
