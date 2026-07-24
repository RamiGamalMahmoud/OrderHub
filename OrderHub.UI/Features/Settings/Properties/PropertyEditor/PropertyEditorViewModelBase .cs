using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Domain.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Settings.Properties.PropertyEditor;

public abstract partial class PropertyEditorViewModelBase : ObservableValidator
{
    protected PropertyEditorViewModelBase()
    {
        PropertyTypes = new ObservableCollection<EnumItem<PropertyType>>(
            Enum.GetValues<PropertyType>()
                .Select(x => new EnumItem<PropertyType>(x, x.GetDescription())));

        SelectedPropertyType = PropertyTypes.First();

        Options.CollectionChanged += (_, _) =>
        {
            SaveCommand.NotifyCanExecuteChanged();
        };

        ValidateAllProperties();
    }

    public Mode Mode { get; protected set; }

    [ObservableProperty]
    [Required(ErrorMessage = "الاسم مطلوب")]
    [StringLength(100)]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(500)]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _description;

    [ObservableProperty]
    [Required]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private EnumItem<PropertyType> _selectedPropertyType = null!;

    public ObservableCollection<EnumItem<PropertyType>> PropertyTypes { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOptionCommand))]
    private string? _newOptionValue;

    public ObservableCollection<OptionViewModel> Options { get; } = [];

    partial void OnSelectedPropertyTypeChanged(EnumItem<PropertyType> value)
    {
        if (value.Value != PropertyType.List)
            Options.Clear();

        AddOptionCommand.NotifyCanExecuteChanged();
    }

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
        Options.Add(new OptionViewModel(null, NewOptionValue!.Trim()));

        NewOptionValue = string.Empty;
    }

    [RelayCommand]
    private void RemoveOption(OptionViewModel option)
    {
        if (option is null)
            return;

        Options.Remove(option);
    }

    protected virtual bool CanSave()
    {
        if (HasErrors)
            return false;

        if (SelectedPropertyType.Value == PropertyType.List &&
            Options.Count == 0)
        {
            return false;
        }

        return true;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    protected virtual Task Save()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    protected virtual void Cancel()
    {
    }
}