using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OrderHub.Domain.ValueObjects;

public record PhoneNumber
{
    public string CountryCode { get; }
    public string NationalNumber { get; }
    public string FullNumber => $"{CountryCode} - {NationalNumber}";

    private PhoneNumber(string nationalNumber, string countryCode)
    {
        CountryCode = countryCode;
        NationalNumber = nationalNumber;
    }

    public static Result<PhoneNumber> CreatePhoneNumber(string number, string countryCode)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(countryCode))
            errors.Add("Country code cannot be empty");

        if (!Regex.IsMatch(countryCode, @"^\+\d{1,4}$"))
            errors.Add("Invalid country code format. Example: +20, +966");

        if (string.IsNullOrWhiteSpace(number))
            errors.Add("Phone number cannot be empty");

        if (!Regex.IsMatch(number, @"^\d{6,14}$"))
            errors.Add("Invalid national number format");

        if (errors.Any())
            return Result<PhoneNumber>.Failure(string.Join(", ", errors));
        return Result<PhoneNumber>.Success(new PhoneNumber(number, countryCode));
    }

    public override string ToString() => FullNumber;
}
