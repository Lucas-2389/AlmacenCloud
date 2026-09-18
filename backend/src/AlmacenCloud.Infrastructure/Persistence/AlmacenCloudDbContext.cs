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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlmacenCloudDbContext).Assembly);

        // Fail-closed: without a valid tenant, tenant-scoped queries return no rows.
        modelBuilder.Entity<Empresa>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.Id);
        modelBuilder.Entity<Usuario>().HasQueryFilter(x => tenantContext.HasTenant && tenantContext.EmpresaId == x.EmpresaId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Modified &&
                     (x.Entity is Empresa || x.Entity is Usuario)))
        {
            entry.Property("ActualizadoEn").CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
