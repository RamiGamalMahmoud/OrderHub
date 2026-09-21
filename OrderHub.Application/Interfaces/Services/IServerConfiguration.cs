namespace OrderHub.Application.Interfaces.Services;
public interface IServerConfiguration
{
    string ServerAddress { get; }
    string ApiBaseUrl { get; }
}