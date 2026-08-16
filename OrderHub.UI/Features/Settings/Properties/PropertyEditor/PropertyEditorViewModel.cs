using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Domain.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Settings.Properties.PropertyEditor;

public partial class PropertyEditorViewModel : ObservableValidator
{
    private readonly IMediator _mediator;

    public PropertyEditorViewModel(IMediator mediator, Mode mode)
    {
        PropertyTypes = new ObservableCollection<EnumItem<PropertyType>>(EnumItems.For<PropertyType>());

        SelectedPropertyType = PropertyTypes.First();

        Options.CollectionChanged += Options_CollectionChanged;

        _mediator = mediator;
        Mode = mode;
        ValidateAllProperties();
    }

    private void Options_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private Mode _mode = Mode.ReadOnly;

    [ObservableProperty]
    [Required(ErrorMessage = "الاسم مطلوب")]
    [StringLength(100)]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyDataErrorInfo]
    [StringLength(500)]
    private string _description;

    [ObservableProperty]
    [Required]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyDataErrorInfo]
    private EnumItem<PropertyType> _selectedPropertyType;

    public ObservableCollection<EnumItem<PropertyType>> PropertyTypes { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOptionCommand))]
    private string _newOptionValue;

    public ObservableCollection<OptionViewModel> Options { get; } = [];

    private bool CanAddOption()
    {
        if (SelectedPropertyType.Value != PropertyType.List)
            return false;

        if (string.IsNullOrWhiteSpace(NewOptionValue))
            return false;

        var value = NewOptionValue.Trim();

        return !Options.Any(x =>
            x.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand(CanExecute = nameof(CanAddOption))]
    private void AddOption()
    {
        var value = NewOptionValue!.Trim();

        Options.Add(new OptionViewModel(null, value));

        NewOptionValue = string.Empty;

        AddOptionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveOption(OptionViewModel option)
    {
        if (option is null)
            return;

        Options.Remove(option);

        AddOptionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPropertyTypeChanged(EnumItem<PropertyType> value)
    {
        if (value.Value != PropertyType.List)
            Options.Clear();
    }

    public async Task LoadAsync(int id)
    {
        PropertyDetailsDto property = await _mediator.Send(new GetPropertyQuery(id));
        Name = property.Name;
        SelectedPropertyType = PropertyTypes.Where(x => x.Value == property.PropertyType).First();
        Description = property.Description;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
    }

    private bool CanSave()
    {
        if (Mode == Mode.ReadOnly)
            return false;

        if (HasErrors)
            return false;

        if(SelectedPropertyType.Value == PropertyType.List && Options.Count == 0)
        {
            return false;
        }

        return true;
    }

    [RelayCommand]
    private void Cancel()
    {
    }
}