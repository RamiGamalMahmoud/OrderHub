using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Features.Setup.Properties.Create;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Settings.Properties.PropertyEditor;

public sealed partial class CreatePropertyViewModel : PropertyEditorViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMessenger _messenger;

    public CreatePropertyViewModel(
        IMediator mediator,
        IMessenger messenger)
    {
        _mediator = mediator;
        _messenger = messenger;
        Mode = Mode.Create;
    }

    protected override async Task Save()
    {
        var command = new CreatePropertyCommand(
            Name,
            SelectedPropertyType.Value,
            Description,
            Options
                .Select(x => new PropertyOptionCreateDto(x.Id, x.Value))
                .ToList());

        var id = await _mediator.Send(command);

        //_messenger.Send(new PropertyCreatedMessage(id));
    }

    protected override void Cancel()
    {
        //_messenger.Send(new ClosePropertyEditorMessage());
    }
}