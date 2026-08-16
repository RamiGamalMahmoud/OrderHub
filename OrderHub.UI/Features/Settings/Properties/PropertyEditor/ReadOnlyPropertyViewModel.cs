using MediatR;
using OrderHub.Application.Features.Setup.Properties.Get;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Settings.Properties.PropertyEditor;

public sealed partial class ReadOnlyPropertyViewModel : PropertyEditorViewModelBase
{
    private readonly IMediator _mediator;
    private readonly int _propertyId;

    public ReadOnlyPropertyViewModel(IMediator mediator, int propertyId)
    {
        _mediator = mediator;
        _propertyId = propertyId;
        Mode = Mode.ReadOnly;
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

    protected override bool CanSave()
    {
        return false;
    }

    protected override Task Save()
    {
        return Task.CompletedTask;
    }

    protected override void Cancel()
    {
    }
}