using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.UI.Common;
using System.ComponentModel.DataAnnotations;

namespace OrderHub.UI.Features.Cities.Editor;

public abstract partial class ViewModelBase : EditorViewModelBase
{
    protected ViewModelBase()
    {
        _notifyPropertiesNames = [nameof(Name)];
        ValidateAllProperties();
    }

    [ObservableProperty]
    [Required(ErrorMessage = "اسم المدينة مطلوب")]
    [MinLength(2, ErrorMessage = "الاسم يجب أن يكون أكثر من 2 حرف")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;
}
