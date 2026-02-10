using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Interfaces;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.UI.Features.Clients.Create
{
    internal class ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService) : Edit.ViewModelBase(mediator, dialogService, messenger)
    {
        public override string Title => "إنشاء عميل جديد";

        protected override async Task Save()
        {
            ClientCreateDto clientCreateDto = new ClientCreateDto(Name, Street, SelectedCity.Id, Number, CountryCode);
            Result result = await _mediator.Send(new Application.Commands.ClienCommands.CreateClientCommand(clientCreateDto));
            if(result.IsSuccess)
            {
                await _mediator.Publish(new Application.Notifications.SuccessNotification("تم انشاء العميل بنجاح"));
                _messenger.Send(new Application.Messages.Clients.ClientCreatedMessage());
                OnRequestClose();
            }

            else
            {
                await _mediator.Publish(new Application.Notifications.ErrorNotification("خطاء في انشاء العميل"));
            }
        }
    }
}
