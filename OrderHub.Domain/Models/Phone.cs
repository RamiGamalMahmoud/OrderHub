using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;

namespace OrderHub.Domain.Models;

public partial class Phone : ModelBase
{
    public PhoneNumber Number { get; private set; }
    public bool IsPrimary { get; private set; }
    private Phone() { }

    private Phone(string number, string countryCode, bool isPrimary = false)
    {
        ChangeNumber(number, countryCode);
        IsPrimary = isPrimary;
    }

    public static Result<Phone> Create(string number, string countryCode, bool isPrimary = false)
    {
        Result<PhoneNumber> phoneNumberResult = PhoneNumber.CreatePhoneNumber(number, countryCode);

        if (!phoneNumberResult.IsSuccess)
            return Result<Phone>.Failure(phoneNumberResult.ErrorMessage);
        return Result<Phone>.Success(new Phone(number, countryCode, isPrimary));
    }

    public void ChangeNumber(string number, string countryCode)
    {
        Number = PhoneNumber.CreatePhoneNumber(number, countryCode).Value;
    }

    public void SetPrimary() => IsPrimary = true;
    public void UnsetPrimary() => IsPrimary = false;
    public override string ToString() => Number.FullNumber;
}
