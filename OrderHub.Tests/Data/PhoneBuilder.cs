using OrderHub.Domain.Models;
using System;

namespace OrderHub.Tests.Data;

public class PhoneBuilder
{
    private string _number = "1000000000";
    private string _countryCode = "20";
    private bool _isPrimary = true;

    public PhoneBuilder WithNumber(string number)
    {
        _number = number;
        return this;
    }

    public PhoneBuilder WithCountryCode(string code)
    {
        _countryCode = code;
        return this;
    }

    public PhoneBuilder AsPrimary()
    {
        _isPrimary = true;
        return this;
    }

    public PhoneBuilder AsSecondary()
    {
        _isPrimary = false;
        return this;
    }

    public Phone Build()
    {
        var result = Phone.Create(_number, _countryCode, _isPrimary);

        if (!result.IsSuccess)
            throw new InvalidOperationException(result.ErrorMessage);

        return result.Value;
    }
}