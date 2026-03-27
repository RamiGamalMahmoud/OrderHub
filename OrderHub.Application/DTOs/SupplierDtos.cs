using OrderHub.Domain.Models;
using System;

namespace OrderHub.Application.DTOs;

public static class SupplierDtos
{
    public record SupplierListDto(int Id, string Name, TimeOnly OpenAt, TimeOnly CloseAt, string Address, string PhoneNumber, string WhatsappGroup);
    public record SupplierUpdateDto(int Id, string Name, TimeOnly OpenAt, TimeOnly CloseAt, string Street, int CityId, string PhoneNumber, string CountryCode, int WhatsappGroupId);
    public record SupplierCreateDto(string Name, TimeOnly OpenAt, TimeOnly CloseAt, string Street, int CityId, string PhoneNumber, string CountryCode, int WhatsappGroupId);
    public record SupplierEditDto(int Id, string Name, TimeOnly OpenAt, TimeOnly CloseAt, string Street, int CityId, string PhoneNumber, string CountryCode, int? WhatsappGroupId);
}
