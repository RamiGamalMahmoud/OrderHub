using MediatR;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Notifications;
using System.Threading.Tasks;

namespace OrderHub.UI.Services;

public class Notifier : INotifier
{
    private readonly IMediator _mediator;

    public Notifier(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task Error(string message) => _mediator.Publish(new ErrorNotification(message));

    public Task Notify(string message) => _mediator.Publish(new AppliationNotification(message));

    public Task Success(string message) => _mediator.Publish(new SuccessNotification(message));
}