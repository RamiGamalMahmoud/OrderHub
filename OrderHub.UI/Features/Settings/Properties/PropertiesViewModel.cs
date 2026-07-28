using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Features.Settings.Properties.PropertyEditor;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Settings.Properties;

public partial class PropertiesViewModel : ObservableObject
{
    private readonly ObservableCollection<PropertyViewModel> _propertyItems = [];
    private readonly IMediator _mediator;
    private readonly IMessenger _messenger;
    private readonly INotificationService _notificationService;

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

    public PropertiesViewModel(IMediator mediator, IMessenger messenger, INotificationService notificationService)
    {
        PropertyItems = new ReadOnlyObservableCollection<PropertyViewModel>(_propertyItems);
        _mediator = mediator;
        _messenger = messenger;
        _notificationService = notificationService;
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
        PropertyEditorViewModel = new CreatePropertyViewModel(_mediator, _messenger);
    }

    [RelayCommand]
    private void Edit(int id)
    {
        PropertyEditorViewModel = new UpdatePropertyViewModel(_mediator, _messenger, id);
        _ = (PropertyEditorViewModel as UpdatePropertyViewModel).LoadAsync();
    }

    [RelayCommand]
    private async Task Delete(int id)
    {
        Result result = await _mediator.Send(new Application.Features.Setup.Properties.Delete.DeleteProperty.Command(id));
        if (result.IsSuccess)
        {
            _propertyItems.Remove(_propertyItems.Where(x => x.Id == id).First());
            _notificationService.ShowSuccess("تم حذف الخاصية");
        }
        else
        {
            _notificationService.ShowError("لم يتم حذف الخاصية , الخاصية مرتبطة ببيانات أخرى");
        }
    }
}
