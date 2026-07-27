//using System.Collections.Generic;
//using static OrderHub.Application.DTOs.CommonDtos;

//namespace OrderHub.Application.DTOs;

//public class ProductDtos
//{
//    public record ProductListDto(
//        int Id,
//        string Name,
//        decimal Price,
//        string Code,
//        string CategoryName,
//        IReadOnlyCollection<SupplierInfoDto> Suppliers,
//        IEnumerable<ProductPropertiesInfoDto> ProductProperties);

//    public record ProductPropertiesInfoDto(
//        int PropertyId,
//        bool IsRequired);
//    public record ProductPropertiesFormDto(
//        int PropertyId,
//        bool IsRequired);

//    public record ProductFormDto(
//        string Name,
//        string Code,
//        decimal Price,
//        int CategoryId,
//        IEnumerable<int> SelectedSuppliersIds,
//        IEnumerable<ProductPropertiesInfoDto> ProductProperties);
//}
