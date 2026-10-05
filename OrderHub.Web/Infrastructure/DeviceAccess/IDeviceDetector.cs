namespace OrderHub.Web.Infrastructure.DeviceAccess;

public interface IDeviceDetector
{
    DeviceType Detect(HttpRequest request);
}
