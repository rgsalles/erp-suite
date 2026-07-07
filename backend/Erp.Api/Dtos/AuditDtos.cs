namespace Erp.Api.Dtos;

public sealed record AuditLogDto(
    Guid Id,
    DateTime OccurredAt,
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
