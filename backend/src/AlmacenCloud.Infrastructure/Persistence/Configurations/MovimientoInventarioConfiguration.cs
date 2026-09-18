using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("MovimientosInventario");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TipoMovimiento).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Cantidad).HasPrecision(18, 4);
        builder.Property(x => x.StockAnterior).HasPrecision(18, 4);
        builder.Property(x => x.StockPosterior).HasPrecision(18, 4);
        builder.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Referencia).HasMaxLength(150);
        builder.HasIndex(x => new { x.EmpresaId, x.CreadoEn });
        builder.HasIndex(x => x.TransferenciaId);
        builder.HasIndex(x => x.VentaId);
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Venta).WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
    }
}
