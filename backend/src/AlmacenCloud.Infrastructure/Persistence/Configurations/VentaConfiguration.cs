using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas"); builder.HasKey(x => x.Id);
        builder.Property(x => x.ClienteNombre).HasMaxLength(250).IsRequired(); builder.Property(x => x.Numero).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(18, 2); builder.Property(x => x.Igv).HasPrecision(18, 2); builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.Observacion).HasMaxLength(1000);
        builder.Property(x => x.Version).IsConcurrencyToken(); builder.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique();
        builder.HasIndex(x => new { x.EmpresaId, x.Fecha });
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AnuladaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
