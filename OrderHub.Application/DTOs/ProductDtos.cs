using System.Collections.Generic;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.Application.DTOs;

public class ProductDtos
{
    public record ProductCreateDto(string Name, string Code, decimal Price, int CategoryId, IEnumerable<int> SelectedSuppliersIds);
    public record ProductListDto(int Id, string Name, decimal Price, string Code, string CategoryName, IReadOnlyCollection<int> SupplierIds, IReadOnlyCollection<SupplierInfoDto> Suppliers);
    public record ProductUpdateDto(int Id, string Name, string Code, decimal Price, int CategoryId, IEnumerable<int> SelectedSuppliersIds);
    public record ProductEdtiDto(int Id, string Name, string Code, decimal Price, int CategoryId, IEnumerable<int> SelectedSuppliersIds);
}
