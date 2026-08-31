using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Features.Settings.Properties.PropertyEditor;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Settings.Properties;

public partial class PropertiesViewModel : ObservableObject
{
    private readonly ObservableCollection<PropertyViewModel> _propertyItems = [];
    private readonly IMediator _mediator;

    public ReadOnlyObservableCollection<PropertyViewModel> PropertyItems { get; }

    [ObservableProperty]
    public PropertyViewModel _selectedProperty;

    partial void OnSelectedPropertyChanged(PropertyViewModel value)
    {
        if (value is null)
        {
            PropertyEditorViewModel = null;
            return;
        }
        PropertyEditorViewModel = new ReadOnlyPropertyViewModel(_mediator, value.Id);
        _ = (PropertyEditorViewModel as ReadOnlyPropertyViewModel).LoadAsync();
    }

    [ObservableProperty]
    private PropertyEditorViewModelBase _propertyEditorViewModel;

    public PropertiesViewModel(IMediator mediator)
    {
        PropertyItems = new ReadOnlyObservableCollection<PropertyViewModel>(_propertyItems);
        _mediator = mediator;

        WeakReferenceMessenger.Default.Register<PropertyMessage>(this, async (r, m) =>
        {
            await LoadAsync();
        });
    }

    public async Task LoadAsync()
    {
        _propertyItems.Clear();

        IEnumerable<PropertyListDto> items = await _mediator.Send(new GetPropertiesQuery());
        foreach (var item in items)
        {
            _propertyItems.Add(new PropertyViewModel()
            {
                Id = item.Id,
                Name = item.Name,
                Type = new EnumItem<PropertyType>(item.Type, item.Type.GetDescription()),
            });
        }
    }

    [RelayCommand]
    private void Create()
    {
        SelectedProperty = null;
        PropertyEditorViewModel = new CreatePropertyViewModel(_mediator);
    }

    [RelayCommand]
    private void Edit(int id)
    {
        PropertyEditorViewModel = new UpdatePropertyViewModel(_mediator, id);
        _ = (PropertyEditorViewModel as UpdatePropertyViewModel).LoadAsync();
    }

    [RelayCommand]
    private async Task Delete(int id)
    {
        Result result = await _mediator.Send(new Application.Features.Setup.Properties.Delete.DeleteProperty.Command(id));
        if (result.IsSuccess)
        {
            _propertyItems.Remove(_propertyItems.Where(x => x.Id == id).First());
            NotificationService.Instance.ShowSuccess("تم حذف الخاصية");
        }
        else
        {
            NotificationService.Instance.ShowError("لم يتم حذف الخاصية , الخاصية مرتبطة ببيانات أخرى");
        }
    }
}
