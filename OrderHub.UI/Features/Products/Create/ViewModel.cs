using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.UI.Features.Products.Create
{
    public class ViewModel : Editor.ViewModel
    {
        public ViewModel(IMediator mediator, IMessenger messenger) : base(mediator, messenger)
        {
        }

        public override string Title => "إضافة منتج";

        protected override async Task Save()
        {
            IEnumerable<int> selectedSupplierIds = Suppliers.Where(s => s.IsSelected).Select(s => s.Value.Id);
            ProductFormDto product = new ProductFormDto(Name, Code, Price, CategorySelection.SelectedCategory.Id, selectedSupplierIds);
            Result result = await _mediator.Send(new Application.Commands.ProductCommands.CreateProductCommand(product));

            if(result.IsSuccess)
            {
                await _mediator.Publish(new Application.Notifications.SuccessNotification("تم إنشاء المنتج"));
                _messenger.Send(new Application.Messages.Products.ProductedCreatedMessage());
                OnRequestClose();
            }
            else
            {
                await _mediator.Publish(new Application.Notifications.ErrorNotification("خطأ أثناء إنشاء منتج"));
            }
        }
    }
}
