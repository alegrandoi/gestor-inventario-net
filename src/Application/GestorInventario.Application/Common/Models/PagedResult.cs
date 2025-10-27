using System.Collections.Generic;

namespace GestorInventario.Application.Common.Models;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
