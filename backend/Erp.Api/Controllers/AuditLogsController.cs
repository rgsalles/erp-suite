using Erp.Api.Data;
using Erp.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin,Manager")]
public sealed class AuditLogsController(ErpDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AuditLogDto>>> Get(
        [FromQuery] Guid? userId,
        [FromQuery] string? entityName,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 500);

        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(x => x.EntityName == entityName);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.OccurredAt <= to.Value);
        }

        var logs = await query
            .OrderByDescending(x => x.OccurredAt)
            .Take(take)
            .Select(x => new AuditLogDto(
                x.Id,
                x.OccurredAt,
                x.UserId,
                x.UserName,
                x.UserEmail,
                x.Action,
                x.HttpMethod,
                x.Path,
                x.Controller,
                x.EntityName,
                x.EntityId,
                x.StatusCode,
                x.IpAddress,
                x.UserAgent,
                x.Details))
            .ToListAsync();

        return Ok(logs);
    }
}
