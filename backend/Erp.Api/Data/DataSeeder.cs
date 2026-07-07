using Erp.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ErpDbContext db, PasswordHasher<AppUser> passwordHasher)
    {
        var now = DateTime.UtcNow;

        if (!await db.Users.AnyAsync())
        {
            var admin = new AppUser
            {
                Id = SeedIds.AdminUser,
                FullName = "Administrador ERP",
                Email = "admin@erp.local",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = now
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123");
            db.Users.Add(admin);
        }

        await SeedDemoUsersAsync(db, passwordHasher, now);

        if (!await db.Companies.AnyAsync(x => x.Id == SeedIds.CompanyMain))
        {
            db.Companies.Add(new Company
            {
                Id = SeedIds.CompanyMain,
                Code = "MATRIZ",
                Name = "ERP Suite Brasil",
                TaxId = "00.000.000/0001-00",
                CreatedAt = now
            });
        }

        if (!await db.Branches.AnyAsync(x => x.Id == SeedIds.BranchHeadquarters))
        {
            db.Branches.Add(new Branch { Id = SeedIds.BranchHeadquarters, CompanyId = SeedIds.CompanyMain, Code = "SP", Name = "Matriz Sao Paulo", TaxId = "00.000.000/0001-00", Address = "Sao Paulo, SP", CreatedAt = now });
        }

        if (!await db.Branches.AnyAsync(x => x.Id == SeedIds.BranchDistribution))
        {
            db.Branches.Add(new Branch { Id = SeedIds.BranchDistribution, CompanyId = SeedIds.CompanyMain, Code = "CD", Name = "Centro de Distribuicao", TaxId = "00.000.000/0002-00", Address = "Campinas, SP", CreatedAt = now });
        }

        if (!await db.CostCenters.AnyAsync(x => x.Id == SeedIds.CostCenterAdmin))
        {
            db.CostCenters.Add(new CostCenter { Id = SeedIds.CostCenterAdmin, CompanyId = SeedIds.CompanyMain, Code = "ADM", Name = "Administrativo", Description = "Despesas administrativas e corporativas", CreatedAt = now });
        }

        if (!await db.CostCenters.AnyAsync(x => x.Id == SeedIds.CostCenterOperations))
        {
            db.CostCenters.Add(new CostCenter { Id = SeedIds.CostCenterOperations, CompanyId = SeedIds.CompanyMain, Code = "OPS", Name = "Operacoes", Description = "Compras, estoque e producao", CreatedAt = now });
        }

        if (!await db.CostCenters.AnyAsync(x => x.Id == SeedIds.CostCenterSales))
        {
            db.CostCenters.Add(new CostCenter { Id = SeedIds.CostCenterSales, CompanyId = SeedIds.CompanyMain, Code = "COM", Name = "Comercial", Description = "Vendas e relacionamento com clientes", CreatedAt = now });
        }

        if (!await db.UnitOfMeasures.AnyAsync())
        {
            db.UnitOfMeasures.AddRange(
                new UnitOfMeasure { Id = SeedIds.UnitEach, Code = "UN", Name = "Unidade", CreatedAt = now },
                new UnitOfMeasure { Id = SeedIds.UnitKg, Code = "KG", Name = "Quilograma", CreatedAt = now },
                new UnitOfMeasure { Id = SeedIds.UnitMeter, Code = "M", Name = "Metro", CreatedAt = now },
                new UnitOfMeasure { Id = SeedIds.UnitLiter, Code = "L", Name = "Litro", CreatedAt = now });
        }

        if (!await db.CurrencyUnits.AnyAsync())
        {
            db.CurrencyUnits.AddRange(
                new CurrencyUnit { Id = SeedIds.CurrencyBrl, Code = "BRL", Name = "Real brasileiro", Symbol = "R$", IsDefault = true, CreatedAt = now },
                new CurrencyUnit { Id = SeedIds.CurrencyUsd, Code = "USD", Name = "US Dollar", Symbol = "$", CreatedAt = now },
                new CurrencyUnit { Id = SeedIds.CurrencyEur, Code = "EUR", Name = "Euro", Symbol = "EUR", CreatedAt = now });
        }

        if (!await db.ExchangeRates.AnyAsync())
        {
            var today = DateOnly.FromDateTime(now.Date);
            db.ExchangeRates.AddRange(
                new ExchangeRate { Id = SeedIds.ExchangeBrlUsd, FromCurrencyId = SeedIds.CurrencyBrl, ToCurrencyId = SeedIds.CurrencyUsd, RateDate = today, Rate = 0.20000000m, Source = "Demo", CreatedAt = now },
                new ExchangeRate { Id = SeedIds.ExchangeBrlEur, FromCurrencyId = SeedIds.CurrencyBrl, ToCurrencyId = SeedIds.CurrencyEur, RateDate = today, Rate = 0.18000000m, Source = "Demo", CreatedAt = now });
        }

        if (!await db.MaterialCategories.AnyAsync())
        {
            db.MaterialCategories.AddRange(
                new MaterialCategory { Id = SeedIds.CategoryRaw, Name = "Materia-prima", Description = "Insumos usados na producao", CreatedAt = now },
                new MaterialCategory { Id = SeedIds.CategoryPackaging, Name = "Embalagem", Description = "Itens de embalagem e expedicao", CreatedAt = now },
                new MaterialCategory { Id = SeedIds.CategoryFinished, Name = "Produto acabado", Description = "Itens prontos para venda", CreatedAt = now });
        }

        if (!await db.Warehouses.AnyAsync())
        {
            db.Warehouses.AddRange(
                new Warehouse { Id = SeedIds.WarehouseMain, Code = "CENTRAL", Name = "Almoxarifado Central", Location = "Matriz", BranchId = SeedIds.BranchHeadquarters, CreatedAt = now },
                new Warehouse { Id = SeedIds.WarehouseFinished, Code = "PA", Name = "Produtos Acabados", Location = "Expedicao", BranchId = SeedIds.BranchDistribution, CreatedAt = now });
        }
        else
        {
            var mainWarehouse = await db.Warehouses.FindAsync(SeedIds.WarehouseMain);
            if (mainWarehouse is { BranchId: null })
            {
                mainWarehouse.BranchId = SeedIds.BranchHeadquarters;
            }

            var finishedWarehouse = await db.Warehouses.FindAsync(SeedIds.WarehouseFinished);
            if (finishedWarehouse is { BranchId: null })
            {
                finishedWarehouse.BranchId = SeedIds.BranchDistribution;
            }
        }

        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.AddRange(
                new Supplier { Id = SeedIds.SupplierMetal, Name = "Fornecedor Metal Norte", TaxId = "00.000.000/0001-10", Email = "compras@metalnorte.local", Phone = "(11) 3000-1000", ContactName = "Marina", CreatedAt = now },
                new Supplier { Id = SeedIds.SupplierPack, Name = "Pack Brasil", TaxId = "00.000.000/0001-20", Email = "vendas@packbrasil.local", Phone = "(11) 3000-2000", ContactName = "Lucas", CreatedAt = now });
        }

        if (!await db.Customers.AnyAsync())
        {
            db.Customers.AddRange(
                new Customer { Id = SeedIds.CustomerRetail, Name = "Cliente Varejo Sul", TaxId = "00.000.000/0001-30", Email = "pedidos@varejosul.local", Phone = "(41) 3000-3000", ContactName = "Bianca", CreatedAt = now },
                new Customer { Id = SeedIds.CustomerIndustry, Name = "Industria Modelo", TaxId = "00.000.000/0001-40", Email = "compras@industriamodelo.local", Phone = "(31) 3000-4000", ContactName = "Rafael", CreatedAt = now });
        }

        if (!await db.Materials.AnyAsync())
        {
            db.Materials.AddRange(
                new Material
                {
                    Id = SeedIds.MaterialSteel,
                    Code = "MP-ACO-001",
                    Description = "Chapa de aco carbono",
                    CategoryId = SeedIds.CategoryRaw,
                    UnitOfMeasureId = SeedIds.UnitKg,
                    SupplierId = SeedIds.SupplierMetal,
                    StandardCost = 8.40m,
                    SalePrice = 0,
                    MinimumStock = 500,
                    CreatedAt = now
                },
                new Material
                {
                    Id = SeedIds.MaterialBox,
                    Code = "EMB-CX-001",
                    Description = "Caixa papelao reforcada",
                    CategoryId = SeedIds.CategoryPackaging,
                    UnitOfMeasureId = SeedIds.UnitEach,
                    SupplierId = SeedIds.SupplierPack,
                    StandardCost = 2.20m,
                    SalePrice = 0,
                    MinimumStock = 300,
                    CreatedAt = now
                },
                new Material
                {
                    Id = SeedIds.MaterialKit,
                    Code = "PA-KIT-001",
                    Description = "Kit comercial padrao",
                    CategoryId = SeedIds.CategoryFinished,
                    UnitOfMeasureId = SeedIds.UnitEach,
                    StandardCost = 38.90m,
                    SalePrice = 89.90m,
                    MinimumStock = 50,
                    CreatedAt = now
                });
        }

        await SeedDemoCatalogAsync(db, now);

        if (!await db.StockMovements.AnyAsync())
        {
            db.StockMovements.AddRange(
                new StockMovement { MaterialId = SeedIds.MaterialSteel, WarehouseId = SeedIds.WarehouseMain, Type = StockMovementType.Adjustment, Quantity = 950, UnitCost = 8.40m, Reference = "SEED", Notes = "Saldo inicial", CreatedByUserId = SeedIds.AdminUser, CreatedAt = now, MovementDate = now },
                new StockMovement { MaterialId = SeedIds.MaterialBox, WarehouseId = SeedIds.WarehouseMain, Type = StockMovementType.Adjustment, Quantity = 420, UnitCost = 2.20m, Reference = "SEED", Notes = "Saldo inicial", CreatedByUserId = SeedIds.AdminUser, CreatedAt = now, MovementDate = now },
                new StockMovement { MaterialId = SeedIds.MaterialKit, WarehouseId = SeedIds.WarehouseFinished, Type = StockMovementType.Adjustment, Quantity = 75, UnitCost = 38.90m, Reference = "SEED", Notes = "Saldo inicial", CreatedByUserId = SeedIds.AdminUser, CreatedAt = now, MovementDate = now });
        }

        await SeedDemoOrdersAndMovementsAsync(db, now);

        if (!await db.FinancialEntries.AnyAsync())
        {
            db.FinancialEntries.AddRange(
                new FinancialEntry
                {
                    Number = "AP-SEED-0001",
                    Type = FinancialEntryType.Payable,
                    Status = FinancialEntryStatus.Open,
                    IssueDate = now.AddDays(-20),
                    DueDate = now.AddDays(-3),
                    Amount = 1850.00m,
                    PaidAmount = 0,
                    SupplierId = SeedIds.SupplierPack,
                    Description = "Despesa de embalagens para demonstracao",
                    CreatedByUserId = SeedIds.AdminUser,
                    CreatedAt = now
                },
                new FinancialEntry
                {
                    Number = "AR-SEED-0001",
                    Type = FinancialEntryType.Receivable,
                    Status = FinancialEntryStatus.Open,
                    IssueDate = now.AddDays(-8),
                    DueDate = now.AddDays(10),
                    Amount = 3200.00m,
                    PaidAmount = 0,
                    CustomerId = SeedIds.CustomerRetail,
                    Description = "Recebivel comercial para demonstracao",
                    CreatedByUserId = SeedIds.AdminUser,
                    CreatedAt = now
                });
        }

        await SeedDemoFinancialEntriesAsync(db, now);
        await SeedDemoAuditLogsAsync(db, now);

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoUsersAsync(ErpDbContext db, PasswordHasher<AppUser> passwordHasher, DateTime now)
    {
        var demoUsers = new[]
        {
            new AppUser { Id = SeedIds.ManagerUser, FullName = "Mariana Gerente", Email = "gerente@erp.local", Role = UserRole.Manager, IsActive = true, CreatedAt = now },
            new AppUser { Id = SeedIds.BuyerUser, FullName = "Bruno Comprador", Email = "comprador@erp.local", Role = UserRole.Buyer, IsActive = true, CreatedAt = now },
            new AppUser { Id = SeedIds.SellerUser, FullName = "Camila Vendedora", Email = "vendedor@erp.local", Role = UserRole.Seller, IsActive = true, CreatedAt = now },
            new AppUser { Id = SeedIds.StockUser, FullName = "Diego Estoquista", Email = "estoque@erp.local", Role = UserRole.Stock, IsActive = true, CreatedAt = now },
            new AppUser { Id = SeedIds.OperatorUser, FullName = "Paula Operadora", Email = "operador@erp.local", Role = UserRole.Operator, IsActive = true, CreatedAt = now }
        };

        foreach (var user in demoUsers)
        {
            if (await db.Users.AnyAsync(x => x.Email == user.Email))
            {
                continue;
            }

            user.PasswordHash = passwordHasher.HashPassword(user, "Admin@123");
            db.Users.Add(user);
        }
    }

    private static async Task SeedDemoCatalogAsync(ErpDbContext db, DateTime now)
    {
        if (!await db.Suppliers.AnyAsync(x => x.Id == SeedIds.SupplierChemical || x.Name == "Quimica Nova"))
        {
            db.Suppliers.Add(new Supplier
            {
                Id = SeedIds.SupplierChemical,
                Name = "Quimica Nova",
                TaxId = "00.000.000/0001-50",
                Email = "atendimento@quimicanova.local",
                Phone = "(11) 3000-5000",
                ContactName = "Renata",
                CreatedAt = now
            });
        }

        if (!await db.Suppliers.AnyAsync(x => x.Id == SeedIds.SupplierLogistics || x.Name == "Logistica Expressa"))
        {
            db.Suppliers.Add(new Supplier
            {
                Id = SeedIds.SupplierLogistics,
                Name = "Logistica Expressa",
                TaxId = "00.000.000/0001-60",
                Email = "operacoes@logisticaexpressa.local",
                Phone = "(11) 3000-6000",
                ContactName = "Felipe",
                CreatedAt = now
            });
        }

        if (!await db.Customers.AnyAsync(x => x.Id == SeedIds.CustomerConstruction || x.Name == "Construtora Horizonte"))
        {
            db.Customers.Add(new Customer
            {
                Id = SeedIds.CustomerConstruction,
                Name = "Construtora Horizonte",
                TaxId = "00.000.000/0001-70",
                Email = "suprimentos@horizonte.local",
                Phone = "(21) 3000-7000",
                ContactName = "Natalia",
                CreatedAt = now
            });
        }

        if (!await db.Customers.AnyAsync(x => x.Id == SeedIds.CustomerDistributor || x.Name == "Distribuidora Alfa"))
        {
            db.Customers.Add(new Customer
            {
                Id = SeedIds.CustomerDistributor,
                Name = "Distribuidora Alfa",
                TaxId = "00.000.000/0001-80",
                Email = "pedidos@distribuidoraalfa.local",
                Phone = "(51) 3000-8000",
                ContactName = "Sergio",
                CreatedAt = now
            });
        }

        if (!await db.Materials.AnyAsync(x => x.Id == SeedIds.MaterialPaint || x.Code == "MP-TIN-001"))
        {
            db.Materials.Add(new Material
            {
                Id = SeedIds.MaterialPaint,
                Code = "MP-TIN-001",
                Description = "Tinta industrial base agua",
                CategoryId = SeedIds.CategoryRaw,
                UnitOfMeasureId = SeedIds.UnitLiter,
                SupplierId = SeedIds.SupplierChemical,
                StandardCost = 18.90m,
                SalePrice = 0,
                MinimumStock = 80,
                CreatedAt = now
            });
        }

        if (!await db.Materials.AnyAsync(x => x.Id == SeedIds.MaterialPallet || x.Code == "EMB-PLT-001"))
        {
            db.Materials.Add(new Material
            {
                Id = SeedIds.MaterialPallet,
                Code = "EMB-PLT-001",
                Description = "Pallet plastico retornavel",
                CategoryId = SeedIds.CategoryPackaging,
                UnitOfMeasureId = SeedIds.UnitEach,
                SupplierId = SeedIds.SupplierLogistics,
                StandardCost = 64.00m,
                SalePrice = 0,
                MinimumStock = 25,
                CreatedAt = now
            });
        }

        if (!await db.Materials.AnyAsync(x => x.Id == SeedIds.MaterialModule || x.Code == "PA-MOD-001"))
        {
            db.Materials.Add(new Material
            {
                Id = SeedIds.MaterialModule,
                Code = "PA-MOD-001",
                Description = "Modulo industrial compacto",
                CategoryId = SeedIds.CategoryFinished,
                UnitOfMeasureId = SeedIds.UnitEach,
                StandardCost = 820.00m,
                SalePrice = 1490.00m,
                MinimumStock = 10,
                CreatedAt = now
            });
        }
    }

    private static async Task SeedDemoOrdersAndMovementsAsync(ErpDbContext db, DateTime now)
    {
        if (!await db.StockMovements.AnyAsync(x => x.Reference == "DEMO-STOCK"))
        {
            db.StockMovements.AddRange(
                new StockMovement { MaterialId = SeedIds.MaterialPaint, WarehouseId = SeedIds.WarehouseMain, Type = StockMovementType.Adjustment, Quantity = 180, UnitCost = 18.90m, Reference = "DEMO-STOCK", Notes = "Saldo inicial de demonstracao", CreatedByUserId = SeedIds.StockUser, CreatedAt = now, MovementDate = now.AddDays(-15) },
                new StockMovement { MaterialId = SeedIds.MaterialPallet, WarehouseId = SeedIds.WarehouseMain, Type = StockMovementType.Adjustment, Quantity = 40, UnitCost = 64.00m, Reference = "DEMO-STOCK", Notes = "Saldo inicial de demonstracao", CreatedByUserId = SeedIds.StockUser, CreatedAt = now, MovementDate = now.AddDays(-15) },
                new StockMovement { MaterialId = SeedIds.MaterialModule, WarehouseId = SeedIds.WarehouseFinished, Type = StockMovementType.Adjustment, Quantity = 12, UnitCost = 820.00m, Reference = "DEMO-STOCK", Notes = "Saldo inicial de demonstracao", CreatedByUserId = SeedIds.StockUser, CreatedAt = now, MovementDate = now.AddDays(-15) });
        }

        if (!await db.PurchaseOrders.AnyAsync(x => x.Number == "PO-DEMO-0001"))
        {
            db.PurchaseOrders.Add(new PurchaseOrder
            {
                Id = SeedIds.PurchaseOrderReceivedDemo,
                Number = "PO-DEMO-0001",
                SupplierId = SeedIds.SupplierMetal,
                Status = OrderStatus.Received,
                OrderDate = now.AddDays(-12),
                ExpectedDate = now.AddDays(-5),
                ReceivedAt = now.AddDays(-4),
                Notes = "Pedido de compra recebido para demonstracao",
                CreatedAt = now.AddDays(-12),
                UpdatedAt = now.AddDays(-4),
                Items =
                [
                    new PurchaseOrderItem { MaterialId = SeedIds.MaterialSteel, Quantity = 350, UnitCost = 8.65m, ReceivedQuantity = 350 },
                    new PurchaseOrderItem { MaterialId = SeedIds.MaterialBox, Quantity = 250, UnitCost = 2.35m, ReceivedQuantity = 250 }
                ]
            });
        }

        if (!await db.StockMovements.AnyAsync(x => x.Reference == "PO-DEMO-0001"))
        {
            db.StockMovements.AddRange(
                new StockMovement { MaterialId = SeedIds.MaterialSteel, WarehouseId = SeedIds.WarehouseMain, Type = StockMovementType.PurchaseReceipt, Quantity = 350, UnitCost = 8.65m, Reference = "PO-DEMO-0001", Notes = "Recebimento de compra demonstracao", CreatedByUserId = SeedIds.StockUser, CreatedAt = now.AddDays(-4), MovementDate = now.AddDays(-4) },
                new StockMovement { MaterialId = SeedIds.MaterialBox, WarehouseId = SeedIds.WarehouseMain, Type = StockMovementType.PurchaseReceipt, Quantity = 250, UnitCost = 2.35m, Reference = "PO-DEMO-0001", Notes = "Recebimento de compra demonstracao", CreatedByUserId = SeedIds.StockUser, CreatedAt = now.AddDays(-4), MovementDate = now.AddDays(-4) });
        }

        if (!await db.PurchaseOrders.AnyAsync(x => x.Number == "PO-DEMO-0002"))
        {
            db.PurchaseOrders.Add(new PurchaseOrder
            {
                Id = SeedIds.PurchaseOrderOpenDemo,
                Number = "PO-DEMO-0002",
                SupplierId = SeedIds.SupplierChemical,
                Status = OrderStatus.Confirmed,
                OrderDate = now.AddDays(-2),
                ExpectedDate = now.AddDays(8),
                Notes = "Pedido de compra em aberto para demonstracao",
                CreatedAt = now.AddDays(-2),
                Items =
                [
                    new PurchaseOrderItem { MaterialId = SeedIds.MaterialPaint, Quantity = 200, UnitCost = 18.90m },
                    new PurchaseOrderItem { MaterialId = SeedIds.MaterialPallet, Quantity = 30, UnitCost = 64.00m }
                ]
            });
        }

        if (!await db.SalesOrders.AnyAsync(x => x.Number == "SO-DEMO-0001"))
        {
            db.SalesOrders.Add(new SalesOrder
            {
                Id = SeedIds.SalesOrderShippedDemo,
                Number = "SO-DEMO-0001",
                CustomerId = SeedIds.CustomerDistributor,
                Status = OrderStatus.Shipped,
                OrderDate = now.AddDays(-9),
                ShippedAt = now.AddDays(-3),
                Notes = "Pedido de venda expedido para demonstracao",
                CreatedAt = now.AddDays(-9),
                UpdatedAt = now.AddDays(-3),
                Items =
                [
                    new SalesOrderItem { MaterialId = SeedIds.MaterialKit, Quantity = 8, UnitPrice = 89.90m, ShippedQuantity = 8 },
                    new SalesOrderItem { MaterialId = SeedIds.MaterialModule, Quantity = 3, UnitPrice = 1490.00m, ShippedQuantity = 3 }
                ]
            });
        }

        if (!await db.StockMovements.AnyAsync(x => x.Reference == "SO-DEMO-0001"))
        {
            db.StockMovements.AddRange(
                new StockMovement { MaterialId = SeedIds.MaterialKit, WarehouseId = SeedIds.WarehouseFinished, Type = StockMovementType.SalesShipment, Quantity = 8, Reference = "SO-DEMO-0001", Notes = "Expedicao de venda demonstracao", CreatedByUserId = SeedIds.StockUser, CreatedAt = now.AddDays(-3), MovementDate = now.AddDays(-3) },
                new StockMovement { MaterialId = SeedIds.MaterialModule, WarehouseId = SeedIds.WarehouseFinished, Type = StockMovementType.SalesShipment, Quantity = 3, Reference = "SO-DEMO-0001", Notes = "Expedicao de venda demonstracao", CreatedByUserId = SeedIds.StockUser, CreatedAt = now.AddDays(-3), MovementDate = now.AddDays(-3) });
        }

        if (!await db.SalesOrders.AnyAsync(x => x.Number == "SO-DEMO-0002"))
        {
            db.SalesOrders.Add(new SalesOrder
            {
                Id = SeedIds.SalesOrderOpenDemo,
                Number = "SO-DEMO-0002",
                CustomerId = SeedIds.CustomerConstruction,
                Status = OrderStatus.Confirmed,
                OrderDate = now.AddDays(-1),
                Notes = "Pedido de venda aguardando expedicao",
                CreatedAt = now.AddDays(-1),
                Items =
                [
                    new SalesOrderItem { MaterialId = SeedIds.MaterialKit, Quantity = 15, UnitPrice = 92.50m },
                    new SalesOrderItem { MaterialId = SeedIds.MaterialModule, Quantity = 4, UnitPrice = 1510.00m }
                ]
            });
        }
    }

    private static async Task SeedDemoFinancialEntriesAsync(ErpDbContext db, DateTime now)
    {
        if (!await db.FinancialEntries.AnyAsync(x => x.Number == "AP-DEMO-0001"))
        {
            db.FinancialEntries.Add(new FinancialEntry
            {
                Id = SeedIds.PayablePaidDemo,
                Number = "AP-DEMO-0001",
                Type = FinancialEntryType.Payable,
                Status = FinancialEntryStatus.Paid,
                IssueDate = now.AddDays(-12),
                DueDate = now.AddDays(-5),
                SettledAt = now.AddDays(-2),
                Amount = 3615.00m,
                PaidAmount = 3615.00m,
                SupplierId = SeedIds.SupplierMetal,
                PurchaseOrderId = SeedIds.PurchaseOrderReceivedDemo,
                Description = "Conta a pagar baixada referente ao PO-DEMO-0001",
                CreatedByUserId = SeedIds.BuyerUser,
                SettledByUserId = SeedIds.ManagerUser,
                CreatedAt = now.AddDays(-12),
                UpdatedAt = now.AddDays(-2)
            });
        }

        if (!await db.FinancialEntries.AnyAsync(x => x.Number == "AP-DEMO-0002"))
        {
            db.FinancialEntries.Add(new FinancialEntry
            {
                Id = SeedIds.PayableOpenDemo,
                Number = "AP-DEMO-0002",
                Type = FinancialEntryType.Payable,
                Status = FinancialEntryStatus.Open,
                IssueDate = now.AddDays(-2),
                DueDate = now.AddDays(8),
                Amount = 5700.00m,
                PaidAmount = 0,
                SupplierId = SeedIds.SupplierChemical,
                PurchaseOrderId = SeedIds.PurchaseOrderOpenDemo,
                Description = "Conta a pagar em aberto referente ao PO-DEMO-0002",
                CreatedByUserId = SeedIds.BuyerUser,
                CreatedAt = now.AddDays(-2)
            });
        }

        if (!await db.FinancialEntries.AnyAsync(x => x.Number == "AR-DEMO-0001"))
        {
            db.FinancialEntries.Add(new FinancialEntry
            {
                Id = SeedIds.ReceivablePaidDemo,
                Number = "AR-DEMO-0001",
                Type = FinancialEntryType.Receivable,
                Status = FinancialEntryStatus.Paid,
                IssueDate = now.AddDays(-9),
                DueDate = now.AddDays(5),
                SettledAt = now.AddDays(-1),
                Amount = 5189.20m,
                PaidAmount = 5189.20m,
                CustomerId = SeedIds.CustomerDistributor,
                SalesOrderId = SeedIds.SalesOrderShippedDemo,
                Description = "Conta a receber baixada referente ao SO-DEMO-0001",
                CreatedByUserId = SeedIds.SellerUser,
                SettledByUserId = SeedIds.ManagerUser,
                CreatedAt = now.AddDays(-9),
                UpdatedAt = now.AddDays(-1)
            });
        }

        if (!await db.FinancialEntries.AnyAsync(x => x.Number == "AR-DEMO-0002"))
        {
            db.FinancialEntries.Add(new FinancialEntry
            {
                Id = SeedIds.ReceivableOpenDemo,
                Number = "AR-DEMO-0002",
                Type = FinancialEntryType.Receivable,
                Status = FinancialEntryStatus.Open,
                IssueDate = now.AddDays(-1),
                DueDate = now.AddDays(18),
                Amount = 7427.50m,
                PaidAmount = 0,
                CustomerId = SeedIds.CustomerConstruction,
                SalesOrderId = SeedIds.SalesOrderOpenDemo,
                Description = "Conta a receber em aberto referente ao SO-DEMO-0002",
                CreatedByUserId = SeedIds.SellerUser,
                CreatedAt = now.AddDays(-1)
            });
        }
    }

    private static async Task SeedDemoAuditLogsAsync(ErpDbContext db, DateTime now)
    {
        if (await db.AuditLogs.AnyAsync(x => x.Id == SeedIds.DemoAuditLog))
        {
            return;
        }

        db.AuditLogs.Add(new AuditLog
        {
            Id = SeedIds.DemoAuditLog,
            OccurredAt = now,
            UserId = SeedIds.AdminUser,
            UserName = "Administrador ERP",
            UserEmail = "admin@erp.local",
            Action = "DemoData.Seeded",
            HttpMethod = "SEED",
            Path = "DataSeeder",
            Controller = "DatabaseInitializer",
            EntityName = "DemoData",
            EntityId = "ERP-DEMO",
            StatusCode = StatusCodes.Status200OK,
            IpAddress = "localhost",
            UserAgent = "ERP Suite Seeder",
            Details = "Dados de demonstracao inseridos para testes de usuarios, catalogo, estoque, pedidos e financeiro."
        });
    }
}
