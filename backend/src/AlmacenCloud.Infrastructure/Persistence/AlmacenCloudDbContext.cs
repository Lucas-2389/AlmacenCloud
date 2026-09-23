using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlmacenCloud.Infrastructure.Persistence;

public sealed class AlmacenCloudDbContext(DbContextOptions<AlmacenCloudDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<Inventario> Inventarios => Set<Inventario>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<SecuenciaVenta> SecuenciasVenta => Set<SecuenciaVenta>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> CompraDetalles => Set<CompraDetalle>();
    public DbSet<SecuenciaCompra> SecuenciasCompra => Set<SecuenciaCompra>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlmacenCloudDbContext).Assembly);

        // Fail-closed: without a valid tenant, tenant-scoped queries return no rows.
        modelBuilder.Entity<Empresa>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.Id);
        modelBuilder.Entity<Usuario>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<UsuarioRol>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.Usuario.EmpresaId);
        modelBuilder.Entity<PasswordResetToken>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<Categoria>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<Producto>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<Almacen>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<Inventario>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<MovimientoInventario>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<Cliente>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<Proveedor>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<Venta>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<VentaDetalle>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<SecuenciaVenta>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<Compra>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<CompraDetalle>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
        modelBuilder.Entity<SecuenciaCompra>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Modified &&
                     (x.Entity is Empresa || x.Entity is Usuario || x.Entity is Cliente || x.Entity is Proveedor)))
        {
            entry.Property("ActualizadoEn").CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
