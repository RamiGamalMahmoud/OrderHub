using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Domain.Common;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Products.Create
{
    public class ViewModel : Editor.ViewModel
    {
        public ViewModel(IMediator mediator) : base(mediator)
        {
        }

        public override string Title => "إضافة منتج";

        protected override async Task Save()
        {
            IEnumerable<int> selectedSupplierIds = Suppliers.Where(s => s.IsSelected).Select(s => s.Value.Id);
            CreateProduct.ProductDto product = new CreateProduct.ProductDto(
                Name, 
                Code, 
                Price, 
                CategorySelection.SelectedCategory.Id, 
                selectedSupplierIds,
                Properties
                    .Where(x => x.IsAssigned)
                    .Select(p => new CreateProduct.ProductPropertiesDto(p.Id, p.PropertyRequirement.Value == Editor.PropertyRequirement.Required)));
            
            Result result = await _mediator.Send(new CreateProduct.Command(product));

            if(result.IsSuccess)
            {
                NotificationService.Instance.ShowSuccess("تم إنشاء المنتج");
                WeakReferenceMessenger.Default.Send(new Application.Messages.Products.ProductedCreatedMessage());
                OnRequestClose();
            }
            else
            {
                NotificationService.Instance.ShowError("خطأ أثناء إنشاء منتج");
            }
        }
    }
}
