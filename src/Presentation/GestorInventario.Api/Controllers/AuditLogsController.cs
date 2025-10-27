using System;
using System.Collections.Generic;
using GestorInventario.Application.AuditLogs.Commands;
using GestorInventario.Application.AuditLogs.Models;
using GestorInventario.Application.AuditLogs.Queries;
using GestorInventario.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

[Authorize(Policy = "RequireAdministrator")]
public class AuditLogsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<PagedResult<AuditLogDto>> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] string? action,
        [FromQuery] int? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        return await Sender
            .Send(new GetAuditLogsQuery(entityName, action, userId, from, to, pageNumber, pageSize), cancellationToken)
            .ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await Sender
            .Send(new DeleteAuditLogCommand(id), cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }

    [HttpGet("exports/{format}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export(
        [FromRoute] string format,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string jurisdiction = "EU",
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AuditLogExportFormat>(format, ignoreCase: true, out var exportFormat))
        {
            return BadRequest($"Unsupported export format '{format}'.");
        }

        if (from == default || to == default)
        {
            return BadRequest("The 'from' and 'to' parameters are required.");
        }

        var requestedBy = User?.Identity?.Name ?? "admin-api";

        var export = await Sender
            .Send(new GenerateAuditLogExportQuery(exportFormat, from, to, requestedBy, jurisdiction), cancellationToken)
            .ConfigureAwait(false);

        Response.Headers["X-Audit-Report-Signature"] = export.Signature;
        Response.Headers["X-Audit-Report-Format"] = export.Format.ToString();
        Response.Headers["X-Audit-Report-Jurisdiction"] = jurisdiction;

        return File(export.Content, export.ContentType, export.FileName);
    }
}
