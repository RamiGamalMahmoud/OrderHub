using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.UI.Features.Clients.Update
{
    internal class ViewModel : Edit.ViewModelBase
    {
        private readonly ISelectionStore<IClientMarker, int> _selectionStore;

        public ViewModel(IMediator mediator, ISelectionStore<IClientMarker, int> selectionStore, IDialogService dialogService, IMessenger messenger) : base(mediator, dialogService, messenger)
        {
            _selectionStore = selectionStore;
        }

        public override string Title => "تحديث بيانات عميل";

        public override async Task LoadAsync()
        {
            await base.LoadAsync();
            ClientEditDto clientEditDto = await _mediator.Send(new Application.Queries.ClientQueries.GetClientEditQuery(_selectionStore.Id));

            Name = clientEditDto.Name;
            Street = clientEditDto.Street;
            SelectedCity = Cities.FirstOrDefault(c => c.Id == clientEditDto.CityId);
            Number = clientEditDto.PhoneNumber;
            CountryCode = clientEditDto.CountryCode;
            HasChanges = false;
        }

        protected override async Task Save()
        {
            ClientUpdateDto clientUpdateDto = new ClientUpdateDto(_selectionStore.Id, Name, Street, SelectedCity.Id, Number, CountryCode);
            Result result = await _mediator.Send(new Application.Commands.ClienCommands.UpdateClientCommand(clientUpdateDto));

            if (result.IsSuccess)
            {
                await _mediator.Publish(new Application.Notifications.SuccessNotification("تم تحديث بيانات عميل بنجاح"));
                _messenger.Send(new Application.Messages.Clients.ClientUpdatedMessage());
                OnRequestClose();
                _selectionStore.Clear();
                HasChanges = false;
            }
            else
            {
                await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
            }
        }
    }
}
