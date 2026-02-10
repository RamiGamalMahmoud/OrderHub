using OrderHub.Application.Interfaces.Services;
using RestSharp;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class OrderService : IOrderService
{
    private readonly ISessionManager _sessionManager;

    public OrderService(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async Task RequestAsync()
    {
        RestClient client = new RestClient("https://api.salla.dev/admin/v2/countries?page");

        RestRequest request = new RestRequest();
        request.AddHeader("Authorization", $"Bearer {_sessionManager.CurrentSession.Token.AccessToken}");
        RestResponse response = await client.GetAsync(request);
    }
}
