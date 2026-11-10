using OrderHub.Domain.Enums;
using System;
using System.Linq;

namespace OrderHub.Domain.Models.Trips;

public class TripParty
{
    public TripPartyType PartyType { get; private set; }
    public int? PartyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? Location { get; private set; }

    // Factory Methods
    public static TripParty FromClient(Client client)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));

        return new TripParty
        {
            PartyType = TripPartyType.Client,
            PartyId = client.Id,
            Name = Normalize(client.Name.Value) ?? throw new InvalidOperationException("اسم العميل مطلوب."),
            Phone = NormalizeToNull(client.Phone?.Number.FullNumber),
            Address = NormalizeToNull(client.Address?.FullAddress),
            Location = NormalizeToNull(client.Location)
        };
    }

    public static TripParty FromSupplier(Supplier supplier)
    {
        if (supplier == null) throw new ArgumentNullException(nameof(supplier));

        return new TripParty
        {
            PartyType = TripPartyType.Supplier,
            PartyId = supplier.Id,
            Name = Normalize(supplier.Name.Value) ?? throw new InvalidOperationException("اسم المورد مطلوب."),
            Phone = NormalizeToNull(supplier.Phone?.Number.FullNumber),
            Address = NormalizeToNull(supplier.Address?.FullAddress),
            Location = null
        };
    }

    public static TripParty FromShippingCarrier(ShippingCarrier carrier)
    {
        if (carrier == null) throw new ArgumentNullException(nameof(carrier));

        return new TripParty
        {
            PartyType = TripPartyType.ShippingCarrier,
            PartyId = carrier.Id,
            Name = Normalize(carrier.Name.Value) ?? throw new InvalidOperationException("اسم شركة الشحن مطلوب."),
            Phone = NormalizeToNull(carrier.Phone?.Number.FullNumber),
            Address = NormalizeToNull(carrier.Address?.FullAddress),
            Location = null
        };
    }

    public static TripParty External(
        string? name,
        string? phone,
        string? address,
        string? location)
    {
        var normalizedName = Normalize(name);
        var normalizedPhone = Normalize(phone);
        var normalizedAddress = Normalize(address);

        if (string.IsNullOrEmpty(normalizedName) || normalizedName.Length > 100)
            throw new InvalidOperationException("اسم الطرف الخارجي مطلوب ويجب ألا يتجاوز 100 حرف.");

        if (string.IsNullOrEmpty(normalizedPhone))
            throw new InvalidOperationException("رقم الهاتف مطلوب للطرف الخارجي.");

        if (normalizedPhone.Length < 6 || normalizedPhone.Length > 22)
            throw new InvalidOperationException("رقم الهاتف يجب أن يكون بين 6 و 22 حرفاً.");

        var digitCount = normalizedPhone.Count(char.IsDigit);
        if (digitCount < 6)
            throw new InvalidOperationException("رقم الهاتف يجب أن يحتوي على 6 أرقام على الأقل.");

        if (string.IsNullOrEmpty(normalizedAddress) || normalizedAddress.Length > 255)
            throw new InvalidOperationException("العنوان مطلوب ويجب ألا يتجاوز 255 حرفاً.");

        var normalizedLocation = NormalizeToNull(location);
        if (normalizedLocation != null && normalizedLocation.Length > 255)
            throw new InvalidOperationException("الموقع يجب ألا يتجاوز 255 حرفاً.");

        return new TripParty
        {
            PartyType = TripPartyType.External,
            PartyId = null,
            Name = normalizedName,
            Phone = normalizedPhone,
            Address = normalizedAddress,
            Location = normalizedLocation
        };
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? NormalizeToNull(string? value)
    {
        var normalized = Normalize(value);
        // Map empty strings to null (from supplier/carrier readers)
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    // Private constructor for EF
    private TripParty() { }
}
