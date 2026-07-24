using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OrderHub.UI.Features.Settings.Properties.PropertyEditor;

public partial class OptionViewModel : ObservableValidator
{
    public OptionViewModel(int? id, string value)
    {
        Id = id;
        Value = value;

        ValidateAllProperties();
    }

    public int? Id { get; }

    [ObservableProperty]
    [Required]
    [StringLength(100)]
    [NotifyDataErrorInfo]
    private string _value;
}