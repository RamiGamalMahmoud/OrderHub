using CommunityToolkit.Mvvm.ComponentModel;

namespace OrderHub.UI.Features.Dashboard;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Dashboard";

    [ObservableProperty]
    private bool _isLoading;

    public DashboardViewModel()
    {
    }
}