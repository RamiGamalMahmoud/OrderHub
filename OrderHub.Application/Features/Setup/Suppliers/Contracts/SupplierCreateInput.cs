using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Suppliers.Contracts;

public record SupplierCreateInput(
        string Name,
        TimeOnly? OpenAt,
        TimeOnly? CloseAt,
        string Street,
        int CityId,
        string PhoneNumber,
        string CountryCode,
        IEnumerable<int> SelectedProductsIds);
