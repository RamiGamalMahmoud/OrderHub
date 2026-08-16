using MediatR;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.Update;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Settings.Properties.PropertyEditor;

public sealed partial class UpdatePropertyViewModel : PropertyEditorViewModelBase
{
    private readonly IMediator _mediator;

    private int _propertyId;

    public UpdatePropertyViewModel(IMediator mediator, int propertyId)
    {
        _mediator = mediator;
        _propertyId = propertyId;
        Mode = Mode.Update;
    }

    public async Task LoadAsync()
    {
        PropertyDetailsDto property =
            await _mediator.Send(new GetPropertyQuery(_propertyId));

        Name = property.Name;
        Description = property.Description;

        SelectedPropertyType =
            PropertyTypes.First(x => x.Value == property.PropertyType);

        Options.Clear();

        foreach (var option in property.Options)
        {
            Options.Add(new OptionViewModel(
                option.Id,
                option.Value));
        }
    }

    protected override async Task Save()
    {
        PropertyUpdateDto propertyUpdateDto = new PropertyUpdateDto(
            _propertyId,
            Name,
            Description,
            SelectedPropertyType.Value,
            Options.Select(x => new PropertyOptionUpdateDto(x.Id,  x.Value)).ToList());

        var command = new UpdatePropertyCommand(propertyUpdateDto);

        await _mediator.Send(command);

        await base.Save();
    }

    protected override void Cancel()
    {
        //_messenger.Send(new ClosePropertyEditorMessage());
    }
}