namespace OrderHub.Web.Infrastructure.DeviceAccess;

public sealed class UserAgentDeviceDetector : IDeviceDetector
{
    public DeviceType Detect(HttpRequest request)
    {
        var userAgent = request.Headers.UserAgent.ToString();
        bool Contains(string value) => userAgent.Contains(value, StringComparison.OrdinalIgnoreCase);

        // A usability policy, not a security boundary. Indistinguishable desktop UAs fail open.
        if (Contains("iPad") || (Contains("Macintosh") && Contains("Mobile")))
            return DeviceType.Tablet;

        if (Contains("Android"))
            return Contains("Mobile") ? DeviceType.Mobile : DeviceType.Tablet;

        if (Contains("iPhone") || Contains("iPod") || Contains("Windows Phone"))
            return DeviceType.Mobile;

        return DeviceType.Desktop;
    }
}
