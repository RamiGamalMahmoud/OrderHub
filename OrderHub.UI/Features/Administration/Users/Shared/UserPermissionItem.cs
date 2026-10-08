using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Domain.Enums;

namespace OrderHub.UI.Features.Administration.Users.Shared;

public partial class UserPermissionItem : ObservableObject
{
    public UserPermissionItem(
        PermissionFeature feature,
        string name,
        bool canRead = false,
        bool canWrite = false)
    {
        Feature = feature;
        Name = name;
        _canRead = canRead || canWrite;
        _canWrite = canWrite;
    }

    public PermissionFeature Feature { get; }

    public string Name { get; }

    [ObservableProperty]
    private bool _canRead;

    [ObservableProperty]
    private bool _canWrite;

    partial void OnCanReadChanged(bool value)
    {
        if (!value)
            CanWrite = false;
    }

    partial void OnCanWriteChanged(bool value)
    {
        if (value)
            CanRead = true;
    }
}