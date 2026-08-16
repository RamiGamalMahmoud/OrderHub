using OrderHub.Application.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Application.Common.Lookups;

public static class EnumItems
{
    public static IReadOnlyList<EnumItem<TEnum>> For<TEnum>() where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Select(x => new EnumItem<TEnum>(x, x.GetDescription()))
            .ToList();
    }
}
