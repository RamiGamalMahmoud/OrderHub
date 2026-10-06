using System;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Domain;

public class ClientTests
{
    [Fact]
    public void Client_normalizes_and_updates_existing_contact_objects()
    {
        var city = new City("City");
        var client = new Client(" Client ", " Street ", city, " 123456 ", " +20 ");
        var phone = client.Phone;
        var address = client.Address;
        Assert.Equal("Client", client.Name.Value);
        Assert.Equal("Street", address.Street);
        Assert.Equal("123456", phone.Number.NationalNumber);
        client.UpdateName(" Renamed ");
        client.UpdatePhone(" +966 ", " 987654 ");
        client.UpdateAddress(new City("Other"), " Changed ");
        Assert.Same(phone, client.Phone);
        Assert.Same(address, client.Address);
        Assert.Equal("Changed", address.Street);
        Assert.Equal("+966", phone.Number.CountryCode);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("phone")]
    [InlineData("street")]
    [InlineData("city")]
    public void Invalid_contact_updates_leave_existing_values_intact(string field)
    {
        var city = new City("City");
        var client = new Client("Client", "Street", city, "123456", "+20");
        Assert.ThrowsAny<ArgumentException>(() =>
        {
            switch (field)
            {
                case "name": client.UpdateName(" x "); break;
                case "phone": client.UpdatePhone("+20", "bad"); break;
                case "street": client.UpdateAddress(new City("Other"), " "); break;
                default: client.UpdateAddress(null, "Changed"); break;
            }
        });
        Assert.Equal("Client", client.Name.Value);
        Assert.Equal("Street", client.Address.Street);
        Assert.Same(city, client.Address.City);
        Assert.Equal("123456", client.Phone.Number.NationalNumber);
    }

    [Theory]
    [InlineData("A", "Street", "123456", "+20")]
    [InlineData("Client", " ", "123456", "+20")]
    [InlineData("Client", "Street", "12345", "+20")]
    [InlineData("Client", "Street", "123456", "20")]
    public void Invalid_construction_rejects_domain_invariants(string name, string street, string number, string country)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Client(name, street, new City("City"), number, country));
    }
}
