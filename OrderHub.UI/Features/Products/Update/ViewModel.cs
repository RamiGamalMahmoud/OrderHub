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

        ProductEdtiDto productEdtiDto = await _mediator.Send(new Application.Queries.ProductQueries.GetProductForEditQuery(_selectionStore.Id));

        Name = productEdtiDto.Name;
        Code = productEdtiDto.Code;
        PriceText = productEdtiDto.Price.ToString();
        SelectedCategory = Categories.Where(c => c.Id == productEdtiDto.CategoryId).FirstOrDefault();

        foreach (int supplierId in productEdtiDto.SelectedSuppliersIds)
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

        ProductUpdateDto productUpdateDto = new ProductUpdateDto(_selectionStore.Id, Name, Code, Price, SelectedCategory.Id, selectedSuppliersIds);
        Result result = await _mediator.Send(new Application.Commands.ProductCommands.UpdateProductCommand(productUpdateDto));

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
