namespace OrderHub.UI.Features.Administration.Users.Details;

public sealed class UserDetailsView : Shared.UserEditorView
{
    public UserDetailsView(UserDetailsViewModel viewModel)
    {
        DataContext = viewModel;
    }
}