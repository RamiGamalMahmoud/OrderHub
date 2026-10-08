using OrderHub.Domain.Common;
using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Application.Features.Products;

internal static class ProductInputValidation
{
    public static Result Validate(string name, string code, decimal price, int categoryId,
        IReadOnlyCollection<int> suppliers, IReadOnlyCollection<int> properties)
    {
        try
        {
            _ = new EntityName(name?.Trim());
        }
        catch (ArgumentException)
        {
            return Result.Failure("اسم المنتج يجب أن يتكون من 2 إلى 100 حرف.");
        }
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 10)
        {
            return Result.Failure("كود المنتج مطلوب ويجب ألا يتجاوز 10 أحرف.");
        }
        if (price < 0 || price > 9999999999999999.99m || decimal.Round(price, 2) != price)
        {
            return Result.Failure("أدخل سعرًا غير سالب لا يتجاوز منزلتين عشريتين وحدود السعر المتاحة.");
        }
        if (categoryId <= 0)
        {
            return Result.Failure("اختر قسمًا للمنتج.");
        }
        if (suppliers.Any(id => id <= 0) || suppliers.Distinct().Count() != suppliers.Count)
        {
            return Result.Failure("اختيار الموردين غير صالح أو يحتوي على تكرار.");
        }
        if (properties.Any(id => id <= 0) || properties.Distinct().Count() != properties.Count)
        {
            return Result.Failure("اختيار الخصائص غير صالح أو يحتوي على تكرار.");
        }
        return Result.Success();
    }
}
