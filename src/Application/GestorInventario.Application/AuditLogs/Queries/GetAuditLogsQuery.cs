using System;
using System.Collections.Generic;
using System.Linq;
using GestorInventario.Application.AuditLogs.Models;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.AuditLogs.Queries;

public sealed record GetAuditLogsQuery(
    string? EntityName,
    string? Action,
    int? UserId,
    DateTime? From,
    DateTime? To,
    int PageNumber = 1,
    int PageSize = 25) : IRequest<PagedResult<AuditLogDto>>;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetAuditLogsQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = context.AuditLogs
            .Include(log => log.User)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            query = query.Where(log => log.EntityName == request.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(log => log.Action == request.Action);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(log => log.UserId == request.UserId);
        }

        if (request.From.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= request.To.Value);
        }

        var pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 200) : 25;
        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AuditLogDto(
                log.Id,
                log.EntityName,
                log.EntityId,
                log.Action,
                log.Changes,
                log.UserId,
                log.User != null ? log.User.Username : null,
                log.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<AuditLogDto>(logs, pageNumber, pageSize, totalCount, totalPages);
    }
}
