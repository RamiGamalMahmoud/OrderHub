using System;

namespace OrderHub.UI
{
    internal record NavigationCommand(string Name, string Icon = null, Action Action = null, bool IsEnabled = true);
}
