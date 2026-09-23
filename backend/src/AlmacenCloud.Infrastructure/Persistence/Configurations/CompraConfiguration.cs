using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> builder)
    {
        builder.ToTable("Compras"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Numero).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NumeroDocumentoProveedor).HasMaxLength(100);
        builder.Property(x => x.Observacion).HasMaxLength(1000);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2); builder.Property(x => x.Igv).HasPrecision(18, 2); builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique();
        builder.HasIndex(x => new { x.EmpresaId, x.Fecha });
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AnuladoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
