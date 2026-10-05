using OrderHub.Domain.Models;
using System;

namespace OrderHub.Application.Features.Setup.Suppliers.Contracts;

public record SupplierUpdateInput(
        int Id,
        string Name,
        TimeOnly OpenAt,
        TimeOnly CloseAt,
        string Street,
        int CityId,
        string PhoneNumber,
        string CountryCode);
