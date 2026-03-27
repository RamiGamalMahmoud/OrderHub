using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.UI.Features.Products.Update;

public class ViewModel : Editor.ViewModel
{
    private readonly ISelectionStore<IProductMarker, int> _selectionStore;
    public ViewModel(IMediator mediator, ISelectionStore<IProductMarker, int> selectionStore, IMessenger messenger) : base(mediator, messenger)
    {
        _selectionStore = selectionStore;
        HasChanges = false;
    }

    public override string Title => "تعديل بيانات منتج";

    public override async Task LoadDataAsync()
    {
        await base.LoadDataAsync();

        ProductFormDto product = await _mediator.Send(new Application.Queries.ProductQueries.GetProductForEditQuery(_selectionStore.Id));

        Name = product.Name;
        Code = product.Code;
        PriceText = product.Price.ToString();
        SelectedCategory = Categories.Where(c => c.Id == product.CategoryId).FirstOrDefault();

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

        ProductFormDto product = new ProductFormDto(Name, Code, Price, SelectedCategory.Id, selectedSuppliersIds);
        Result result = await _mediator.Send(new Application.Commands.ProductCommands.UpdateProductCommand(_selectionStore.Id, product));

        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم تحديث بيانات المنتج"));
            _messenger.Send(new Application.Messages.Products.ProductedUpdatedMessage());
            _selectionStore.Clear();
            OnRequestClose();
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("خطأ أثناء تحديث بيانات المنتج"));
        }
    }
}
