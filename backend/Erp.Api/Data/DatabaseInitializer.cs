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
        await EnsureOrganizationTablesAsync(db);
        await EnsureWarehouseBranchColumnAsync(db);
        await EnsureCurrencyUnitTableAsync(db);
        await EnsureExchangeRateTableAsync(db);

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

    private static async Task EnsureCurrencyUnitTableAsync(ErpDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[CurrencyUnits]', N'U') IS NULL
            BEGIN
                CREATE TABLE [CurrencyUnits] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_CurrencyUnits] PRIMARY KEY,
                    [Code] nvarchar(3) NOT NULL,
                    [Name] nvarchar(80) NOT NULL,
                    [Symbol] nvarchar(8) NOT NULL,
                    [IsDefault] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL
                );

                CREATE UNIQUE INDEX [IX_CurrencyUnits_Code] ON [CurrencyUnits] ([Code]);
            END
            """);
    }

    private static async Task EnsureOrganizationTablesAsync(ErpDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[Companies]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Companies] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Companies] PRIMARY KEY,
                    [Code] nvarchar(20) NOT NULL,
                    [Name] nvarchar(160) NOT NULL,
                    [TaxId] nvarchar(40) NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL
                );

                CREATE UNIQUE INDEX [IX_Companies_Code] ON [Companies] ([Code]);
            END

            IF OBJECT_ID(N'[Branches]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Branches] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Branches] PRIMARY KEY,
                    [CompanyId] uniqueidentifier NOT NULL,
                    [Code] nvarchar(20) NOT NULL,
                    [Name] nvarchar(160) NOT NULL,
                    [TaxId] nvarchar(40) NULL,
                    [Address] nvarchar(240) NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [FK_Branches_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
                );

                CREATE UNIQUE INDEX [IX_Branches_CompanyId_Code] ON [Branches] ([CompanyId], [Code]);
            END

            IF OBJECT_ID(N'[CostCenters]', N'U') IS NULL
            BEGIN
                CREATE TABLE [CostCenters] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_CostCenters] PRIMARY KEY,
                    [CompanyId] uniqueidentifier NOT NULL,
                    [Code] nvarchar(30) NOT NULL,
                    [Name] nvarchar(120) NOT NULL,
                    [Description] nvarchar(300) NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [FK_CostCenters_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
                );

                CREATE UNIQUE INDEX [IX_CostCenters_CompanyId_Code] ON [CostCenters] ([CompanyId], [Code]);
            END
            """);
    }

    private static async Task EnsureWarehouseBranchColumnAsync(ErpDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[Warehouses]', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Warehouses', N'BranchId') IS NULL
            BEGIN
                ALTER TABLE [Warehouses] ADD [BranchId] uniqueidentifier NULL;
            END

            IF OBJECT_ID(N'[Warehouses]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[Branches]', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Warehouses_BranchId' AND [object_id] = OBJECT_ID(N'[Warehouses]'))
            BEGIN
                CREATE INDEX [IX_Warehouses_BranchId] ON [Warehouses] ([BranchId]);
            END

            IF OBJECT_ID(N'[Warehouses]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[Branches]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[FK_Warehouses_Branches_BranchId]', N'F') IS NULL
            BEGIN
                ALTER TABLE [Warehouses] WITH CHECK ADD CONSTRAINT [FK_Warehouses_Branches_BranchId]
                    FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE SET NULL;
            END
            """);
    }

    private static async Task EnsureExchangeRateTableAsync(ErpDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[ExchangeRates]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ExchangeRates] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_ExchangeRates] PRIMARY KEY,
                    [FromCurrencyId] uniqueidentifier NOT NULL,
                    [ToCurrencyId] uniqueidentifier NOT NULL,
                    [RateDate] date NOT NULL,
                    [Rate] decimal(18,8) NOT NULL,
                    [Source] nvarchar(120) NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [FK_ExchangeRates_CurrencyUnits_FromCurrencyId] FOREIGN KEY ([FromCurrencyId]) REFERENCES [CurrencyUnits] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ExchangeRates_CurrencyUnits_ToCurrencyId] FOREIGN KEY ([ToCurrencyId]) REFERENCES [CurrencyUnits] ([Id]) ON DELETE NO ACTION
                );

                CREATE UNIQUE INDEX [IX_ExchangeRates_FromCurrencyId_ToCurrencyId_RateDate] ON [ExchangeRates] ([FromCurrencyId], [ToCurrencyId], [RateDate]);
                CREATE INDEX [IX_ExchangeRates_ToCurrencyId] ON [ExchangeRates] ([ToCurrencyId]);
            END
            """);
    }
}
