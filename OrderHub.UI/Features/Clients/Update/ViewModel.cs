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
            ClientFormDto client = await _mediator.Send(new Application.Queries.ClientQueries.GetClientEditQuery(_selectionStore.Id));

            Name = client.Name;
            Street = client.Street;
            SelectedCity = Cities.FirstOrDefault(c => c.Id == client.CityId);
            Number = client.PhoneNumber;
            CountryCode = client.CountryCode;
            Location = client.Location;
            HasChanges = false;
        }

        protected override async Task Save()
        {
            ClientFormDto client = new ClientFormDto(Name, Street, SelectedCity.Id, Number, CountryCode, Location);
            Result result = await _mediator.Send(new Application.Commands.ClienCommands.UpdateClientCommand(_selectionStore.Id, client));

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
