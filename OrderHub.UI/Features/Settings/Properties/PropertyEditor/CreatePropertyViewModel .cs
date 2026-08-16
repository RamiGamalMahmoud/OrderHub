using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Features.Setup.Properties.Create;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Settings.Properties.PropertyEditor;

public sealed partial class CreatePropertyViewModel : PropertyEditorViewModelBase
{
    private readonly IMediator _mediator;

    public CreatePropertyViewModel(
        IMediator mediator,
        IMessenger messenger) : base(messenger)
    {
        _mediator = mediator;
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

        await base.Save();
    }

    protected override void Cancel()
    {
        //_messenger.Send(new ClosePropertyEditorMessage());
    }
}