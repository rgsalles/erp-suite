using Erp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Data;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FinancialEntry> FinancialEntries => Set<FinancialEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
        });

        modelBuilder.Entity<MaterialCategory>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<Supplier>(ConfigureBusinessPartner);
        modelBuilder.Entity<Customer>(ConfigureBusinessPartner);

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(200);
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240).IsRequired();
            entity.Property(x => x.StandardCost).HasPrecision(18, 2);
            entity.Property(x => x.SalePrice).HasPrecision(18, 2);
            entity.Property(x => x.MinimumStock).HasPrecision(18, 3);
            entity.HasOne(x => x.Category).WithMany(x => x.Materials).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UnitOfMeasure).WithMany(x => x.Materials).HasForeignKey(x => x.UnitOfMeasureId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Supplier).WithMany(x => x.Materials).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.Property(x => x.Reference).HasMaxLength(80);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasOne(x => x.Material).WithMany(x => x.StockMovements).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Warehouse).WithMany(x => x.StockMovements).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasOne(x => x.Supplier).WithMany(x => x.PurchaseOrders).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Items).WithOne(x => x.PurchaseOrder).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.Property(x => x.ReceivedQuantity).HasPrecision(18, 3);
            entity.HasOne(x => x.Material).WithMany(x => x.PurchaseOrderItems).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasOne(x => x.Customer).WithMany(x => x.SalesOrders).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Items).WithOne(x => x.SalesOrder).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesOrderItem>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.ShippedQuantity).HasPrecision(18, 3);
            entity.HasOne(x => x.Material).WithMany(x => x.SalesOrderItems).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(x => x.OccurredAt);
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.UserName).HasMaxLength(160);
            entity.Property(x => x.UserEmail).HasMaxLength(200);
            entity.Property(x => x.Action).HasMaxLength(120).IsRequired();
            entity.Property(x => x.HttpMethod).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Path).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Controller).HasMaxLength(120);
            entity.Property(x => x.EntityName).HasMaxLength(120);
            entity.Property(x => x.EntityId).HasMaxLength(80);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
        });

        modelBuilder.Entity<FinancialEntry>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.HasIndex(x => new { x.Type, x.Status, x.DueDate });
            entity.Property(x => x.Number).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.PaidAmount).HasPrecision(18, 2);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.SettledByUser).WithMany().HasForeignKey(x => x.SettledByUserId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureBusinessPartner<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entity)
        where T : class
    {
        entity.Property<string>("Name").HasMaxLength(160).IsRequired();
        entity.Property<string>("TaxId").HasMaxLength(40);
        entity.Property<string>("Email").HasMaxLength(200);
        entity.Property<string>("Phone").HasMaxLength(40);
        entity.Property<string>("ContactName").HasMaxLength(120);
    }
}
