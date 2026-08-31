using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.UI.Features.Clients.Update
{
    internal class ViewModel : Edit.ViewModelBase, IParameterizedViewModel
    {
        private int _clientId;

        public ViewModel(IMediator mediator) : base(mediator)
        {
        }

        public override string Title => "تحديث بيانات عميل";

        public Task Initialize(object parameter)
        {
            if(parameter is not null && parameter is int id)
            {
                _clientId = id;
            }
            return Task.CompletedTask;
        }

        public override async Task LoadAsync()
        {
            await base.LoadAsync();
            ClientFormDto client = await _mediator.Send(new Application.Queries.ClientQueries.GetClientEditQuery(_clientId));

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
            Result result = await _mediator.Send(new Application.Commands.ClienCommands.UpdateClientCommand(_clientId, client));

            if (result.IsSuccess)
            {
                NotificationService.Instance.ShowSuccess("تم تحديث بيانات عميل بنجاح");
                WeakReferenceMessenger.Default.Send(new Application.Messages.Clients.ClientUpdatedMessage());
                OnRequestClose();
                HasChanges = false;
            }
            else
            {
                NotificationService.Instance.ShowError(result.ErrorMessage);
            }
        }
    }
}
