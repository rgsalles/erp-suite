using Erp.Api.Data;
using Erp.Api.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class HealthController(ErpDbContext db, IOptions<RabbitMqOptions> rabbitMqOptions) : ControllerBase
{
    private readonly RabbitMqOptions _rabbitMqOptions = rabbitMqOptions.Value;

    [HttpGet]
    public async Task<ActionResult<object>> Get()
    {
        var databaseAvailable = await db.Database.CanConnectAsync();
        var rabbitMqAvailable = IsRabbitMqAvailable();
        var messagingHealthy = !_rabbitMqOptions.Enabled || rabbitMqAvailable;

        return Ok(new
        {
            status = databaseAvailable && messagingHealthy ? "Healthy" : "Degraded",
            database = databaseAvailable ? "Connected" : "Unavailable",
            rabbitMq = new
            {
                enabled = _rabbitMqOptions.Enabled,
                status = !_rabbitMqOptions.Enabled ? "Disabled" : rabbitMqAvailable ? "Connected" : "Unavailable",
                exchange = _rabbitMqOptions.ExchangeName,
                lowStockQueue = _rabbitMqOptions.LowStockQueueName
            },
            checkedAt = DateTime.UtcNow
        });
    }

    private bool IsRabbitMqAvailable()
    {
        if (!_rabbitMqOptions.Enabled)
        {
            return false;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqOptions.HostName,
                Port = _rabbitMqOptions.Port,
                UserName = _rabbitMqOptions.UserName,
                Password = _rabbitMqOptions.Password,
                VirtualHost = _rabbitMqOptions.VirtualHost
            };

            using var connection = factory.CreateConnection();
            return connection.IsOpen;
        }
        catch
        {
            return false;
        }
    }
}
