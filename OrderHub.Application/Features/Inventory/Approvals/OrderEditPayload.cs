using System.Text.Json;
using System.Text.Json.Serialization;
using OrderHub.Application.Features.Orders.Contracts;

namespace OrderHub.Application.Features.Inventory.Approvals;

internal static class OrderEditPayload
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static string Serialize(OrderInput.Order input) =>
        JsonSerializer.Serialize(input, Options);

    public static OrderInput.Order Deserialize(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            return JsonSerializer.Deserialize<OrderInput.Order>(payload, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
