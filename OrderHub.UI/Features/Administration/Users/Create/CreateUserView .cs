namespace OrderHub.UI.Features.Administration.Users.Create;

public sealed class CreateUserView : Shared.UserEditorView
{
    public CreateUserView(CreateUserViewModel viewModel)
    {
        DataContext = viewModel;
    }
}