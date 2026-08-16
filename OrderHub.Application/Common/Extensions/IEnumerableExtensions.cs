using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Application.Common.Extensions;

public static class IEnumerableExtensions
{
    public static IEnumerable<TItem> MergeSelectedItem<TItem>(this IEnumerable<TItem> items, TItem selectedItem)
    where TItem : class
    {
        TItem[] results = (items ?? Enumerable.Empty<TItem>()).ToArray();

        if (selectedItem is null ||
            results.Any(item => EqualityComparer<TItem>.Default.Equals(item, selectedItem)))
        {
            return results;
        }

        return new[] { selectedItem }.Concat(results);
    }
}
