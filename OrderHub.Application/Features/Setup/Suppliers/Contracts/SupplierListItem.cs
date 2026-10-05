using OrderHub.Domain.Models;
using System;

namespace OrderHub.Application.Features.Setup.Suppliers.Contracts;

public record SupplierListItem(
        int Id,
        string Name,
        TimeOnly OpenAt,
        TimeOnly CloseAt,
        string Address,
        string PhoneNumber);
