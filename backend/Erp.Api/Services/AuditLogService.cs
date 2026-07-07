using Erp.Api.Data;
using Erp.Api.Models;

namespace Erp.Api.Services;

public sealed record AuditEntry(
    Guid? UserId,
    string? UserName,
    string? UserEmail,
    string Action,
    string HttpMethod,
    string Path,
    string? Controller,
    string? EntityName,
    string? EntityId,
    int StatusCode,
    string? IpAddress,
    string? UserAgent,
    string? Details);

public sealed class AuditLogService(ErpDbContext db)
{
    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = entry.UserId,
            UserName = entry.UserName,
            UserEmail = entry.UserEmail,
            Action = entry.Action,
            HttpMethod = entry.HttpMethod,
            Path = entry.Path,
            Controller = entry.Controller,
            EntityName = entry.EntityName,
            EntityId = entry.EntityId,
            StatusCode = entry.StatusCode,
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            Details = entry.Details
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
