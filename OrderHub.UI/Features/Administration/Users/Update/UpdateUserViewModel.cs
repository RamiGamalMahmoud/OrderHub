namespace OrderHub.UI.Features.Administration.Users.Update;
public sealed class UpdateUserViewModel : UserEditorViewModel
{
    public UpdateUserViewModel()
    {
        IsReadOnly = false;
        ShowSaveButton = true;
        SaveButtonText = "حفظ التعديلات";

    }
    public override string Title => "تعديل المستخدم";
    public override bool ShowPassword => false;
}
