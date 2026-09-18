using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Ruc).HasMaxLength(11).IsRequired(); builder.Property(x => x.RazonSocial).HasMaxLength(250).IsRequired();
        builder.Property(x => x.NombreComercial).HasMaxLength(250); builder.Property(x => x.Direccion).HasMaxLength(500);
        builder.Property(x => x.Telefono).HasMaxLength(30); builder.Property(x => x.Email).HasMaxLength(254);
        builder.HasIndex(x => new { x.EmpresaId, x.Ruc }).IsUnique();
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
