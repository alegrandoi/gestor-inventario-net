using System;
using System.Linq;
using System.Text.Json;
using GestorInventario.Application.Common.Caching;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Interfaces.Caching;
using GestorInventario.Application.Common.Models;
using GestorInventario.Application.Products.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Application.Products.Queries;

public record GetProductsQuery(
    string? SearchTerm,
    int? CategoryId,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IDistributedCache cache;
    private readonly ICacheKeyRegistry cacheKeyRegistry;
    private readonly ILogger<GetProductsQueryHandler> logger;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public GetProductsQueryHandler(
        IGestorInventarioDbContext context,
        IDistributedCache cache,
        ICacheKeyRegistry cacheKeyRegistry,
        ILogger<GetProductsQueryHandler> logger)
    {
        this.context = context;
        this.cache = cache;
        this.cacheKeyRegistry = cacheKeyRegistry;
        this.logger = logger;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 200) : 50;
        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        var cacheKey = CacheKeys.ProductCatalog(request.SearchTerm, request.CategoryId, request.IsActive, pageNumber, pageSize);

        var cached = await cache.TryGetStringAsync(cacheKey, logger, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedValue = JsonSerializer.Deserialize<PagedResult<ProductDto>>(cached, SerializerOptions);
            if (cachedValue is not null)
            {
                return cachedValue;
            }
        }

        var query = context.Products
            .AsNoTracking()
            .Include(product => product.Variants)
            .Include(product => product.Images)
            .Include(product => product.TaxRate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(product => product.Name.Contains(term) || product.Code.Contains(term));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == request.CategoryId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(product => product.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        var products = await query
            .OrderBy(product => product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = products
            .Select(product => product.ToDto())
            .ToList();

        var result = new PagedResult<ProductDto>(items, pageNumber, pageSize, totalCount, totalPages);

        var serialized = JsonSerializer.Serialize(result, SerializerOptions);
        await cache.TrySetStringAsync(cacheKey, serialized, CacheOptions, logger, cancellationToken).ConfigureAwait(false);
        await cacheKeyRegistry.TryRegisterKeyAsync(CacheRegions.ProductCatalog, cacheKey, logger, cancellationToken).ConfigureAwait(false);

        return result;
    }
}
