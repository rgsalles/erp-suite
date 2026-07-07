using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Erp.Api.Services;

public sealed class AuditActionFilter(AuditLogService auditLogService) : IAsyncActionFilter
{
    private static readonly HashSet<string> AuditedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executedContext = await next();
        var httpContext = context.HttpContext;
        var method = httpContext.Request.Method;

        if (!ShouldAudit(context, executedContext, method))
        {
            return;
        }

        var user = httpContext.User;
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();
        var entityId = context.RouteData.Values["id"]?.ToString();
        var path = httpContext.Request.Path.Value ?? string.Empty;

        await auditLogService.LogAsync(new AuditEntry(
            UserId: user.GetUserId(),
            UserName: user.FindFirstValue(ClaimTypes.Name),
            UserEmail: user.FindFirstValue(ClaimTypes.Email),
            Action: $"{controller}.{action}",
            HttpMethod: method,
            Path: path,
            Controller: controller,
            EntityName: controller,
            EntityId: entityId,
            StatusCode: httpContext.Response.StatusCode,
            IpAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: httpContext.Request.Headers.UserAgent.ToString(),
            Details: BuildDetails(context)),
            httpContext.RequestAborted);
    }

    private static bool ShouldAudit(ActionExecutingContext context, ActionExecutedContext executedContext, string method)
    {
        if (!AuditedMethods.Contains(method))
        {
            return false;
        }

        if (executedContext.Exception is not null)
        {
            return false;
        }

        if (context.HttpContext.Response.StatusCode < 200 || context.HttpContext.Response.StatusCode >= 400)
        {
            return false;
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        return !string.Equals(controller, "Auth", StringComparison.OrdinalIgnoreCase);
    }

    private static string? BuildDetails(ActionExecutingContext context)
    {
        if (context.ActionArguments.Count == 0)
        {
            return null;
        }

        var node = JsonSerializer.SerializeToNode(context.ActionArguments, JsonOptions);
        RedactSensitiveValues(node);
        return node?.ToJsonString(JsonOptions);
    }

    private static void RedactSensitiveValues(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (IsSensitiveName(property.Key))
                {
                    jsonObject[property.Key] = "[REDACTED]";
                    continue;
                }

                RedactSensitiveValues(property.Value);
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                RedactSensitiveValues(item);
            }
        }
    }

    private static bool IsSensitiveName(string name)
    {
        return name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("hash", StringComparison.OrdinalIgnoreCase);
    }
}
