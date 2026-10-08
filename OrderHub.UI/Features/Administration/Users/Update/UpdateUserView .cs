namespace OrderHub.UI.Features.Administration.Users.Update;

public sealed class UpdateUserView : Shared.UserEditorView
{
    public UpdateUserView(UpdateUserViewModel viewModel)
    {
        DataContext = viewModel;
    }
}