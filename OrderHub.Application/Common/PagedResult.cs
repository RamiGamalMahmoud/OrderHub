using System;
using System.Collections.Generic;

namespace OrderHub.Application.Common;

public record PagedResult<T>(IEnumerable<T> Items, int TotalItems, int CurrentPage, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}