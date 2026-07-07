using Erp.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        var autoCreate = configuration.GetValue("Database:AutoCreate", true);
        if (autoCreate)
        {
            logger.LogInformation("Ensuring SQL Server database exists.");
            await db.Database.EnsureCreatedAsync();
        }
        else if (!await db.Database.CanConnectAsync())
        {
            throw new InvalidOperationException("Could not connect to SQL Server. Check the connection string.");
        }

        await EnsureAuditLogTableAsync(db);
        await EnsureFinancialEntryTableAsync(db);

        var shouldSeed = configuration.GetValue("Database:Seed", true);
        if (shouldSeed)
        {
            logger.LogInformation("Seeding initial ERP data when needed.");
            var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<AppUser>>();
            await DataSeeder.SeedAsync(db, passwordHasher);
        }
    }

    private static async Task EnsureAuditLogTableAsync(ErpDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[AuditLogs]', N'U') IS NULL
            BEGIN
                CREATE TABLE [AuditLogs] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_AuditLogs] PRIMARY KEY,
                    [OccurredAt] datetime2 NOT NULL,
                    [UserId] uniqueidentifier NULL,
                    [UserName] nvarchar(160) NULL,
                    [UserEmail] nvarchar(200) NULL,
                    [Action] nvarchar(120) NOT NULL,
                    [HttpMethod] nvarchar(12) NOT NULL,
                    [Path] nvarchar(300) NOT NULL,
                    [Controller] nvarchar(120) NULL,
                    [EntityName] nvarchar(120) NULL,
                    [EntityId] nvarchar(80) NULL,
                    [StatusCode] int NOT NULL,
                    [IpAddress] nvarchar(64) NULL,
                    [UserAgent] nvarchar(512) NULL,
                    [Details] nvarchar(max) NULL
                );

                CREATE INDEX [IX_AuditLogs_OccurredAt] ON [AuditLogs] ([OccurredAt]);
                CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
            END
            """);
    }

    private static async Task EnsureFinancialEntryTableAsync(ErpDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[FinancialEntries]', N'U') IS NULL
            BEGIN
                CREATE TABLE [FinancialEntries] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_FinancialEntries] PRIMARY KEY,
                    [Number] nvarchar(40) NOT NULL,
                    [Type] nvarchar(40) NOT NULL,
                    [Status] nvarchar(40) NOT NULL,
                    [IssueDate] datetime2 NOT NULL,
                    [DueDate] datetime2 NOT NULL,
                    [SettledAt] datetime2 NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [PaidAmount] decimal(18,2) NOT NULL,
                    [Description] nvarchar(500) NULL,
                    [SupplierId] uniqueidentifier NULL,
                    [CustomerId] uniqueidentifier NULL,
                    [PurchaseOrderId] uniqueidentifier NULL,
                    [SalesOrderId] uniqueidentifier NULL,
                    [CreatedByUserId] uniqueidentifier NULL,
                    [SettledByUserId] uniqueidentifier NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL
                );

                CREATE UNIQUE INDEX [IX_FinancialEntries_Number] ON [FinancialEntries] ([Number]);
                CREATE INDEX [IX_FinancialEntries_Type_Status_DueDate] ON [FinancialEntries] ([Type], [Status], [DueDate]);
            END
            """);
    }
}
