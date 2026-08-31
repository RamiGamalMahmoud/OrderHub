using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Services;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.UI.Features.Clients.Create
{
    internal class ViewModel(IMediator mediator) : Edit.ViewModelBase(mediator)
    {
        public override string Title => "إنشاء عميل جديد";

        protected override async Task Save()
        {
            ClientFormDto client = new ClientFormDto(Name, Street, SelectedCity.Id, Number, CountryCode, Location);
            Result result = await _mediator.Send(new Application.Commands.ClienCommands.CreateClientCommand(client));
            if(result.IsSuccess)
            {
                NotificationService.Instance.ShowSuccess("تم انشاء العميل بنجاح");
                WeakReferenceMessenger.Default.Send(new Application.Messages.Clients.ClientCreatedMessage());
                OnRequestClose();
            }

            else
            {
                NotificationService.Instance.ShowError("خطاء في انشاء العميل");
            }
        }
    }
}
