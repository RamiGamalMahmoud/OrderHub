using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Web.Components.Pages.Features.Clients.Editor;

public sealed class ClientEditorState
{
    public string Name { get; set; } = string.Empty;
    public int? CityId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "+966";
    public string? Location { get; set; }

    public static ClientEditorState FromRead(ClientInput client) => new()
    {
        Name = client.Name,
        CityId = client.CityId > 0 ? client.CityId : null,
        Street = client.Street,
        PhoneNumber = client.PhoneNumber,
        CountryCode = client.CountryCode,
        Location = client.Location
    };

    private ClientInput Input() => new(
        Name, Street, CityId.GetValueOrDefault(), PhoneNumber, CountryCode, Location!);

    public CreateClientCommand CreateCommand() => new(Input());
    public UpdateClientCommand UpdateCommand(int clientId) => new(clientId, Input());

    public static string? ValidateName(string name)
    {
        try
        {
            _ = new OrderHub.Domain.ValueObjects.EntityName(name?.Trim());
            return null;
        }
        catch (ArgumentException)
        {
            return "اسم العميل يجب أن يتكون من 2 إلى 100 حرف.";
        }
    }

    public string? ValidatePhone(string value) =>
        OrderHub.Domain.ValueObjects.PhoneNumber.CreatePhoneNumber(value?.Trim() ?? "", CountryCode?.Trim() ?? "").IsSuccess
            ? null
            : "أدخل رقم هاتف صحيحًا من 6 إلى 14 رقمًا.";

    public string? ValidateCountryCode(string value) =>
        value?.Trim().Length <= 4 &&
        OrderHub.Domain.ValueObjects.PhoneNumber.CreatePhoneNumber("123456", value?.Trim() ?? "").IsSuccess
            ? null
            : "أدخل مفتاح دولة صحيحًا مثل +20 أو +966.";

    public string? ValidateAdditional()
    {
        if (CityId is null or <= 0)
        {
            return "اختر المدينة.";
        }

        if (string.IsNullOrWhiteSpace(Street) || Street.Trim().Length > 150)
        {
            return "عنوان العميل مطلوب ويجب ألا يتجاوز 150 حرفًا.";
        }

        if (Location?.Trim().Length > 255)
        {
            return "الموقع يجب ألا يتجاوز 255 حرفًا.";
        }

        return null;
    }
}
