using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class CompraDetalleConfiguration : IEntityTypeConfiguration<CompraDetalle>
{
    public void Configure(EntityTypeBuilder<CompraDetalle> builder)
    {
        builder.ToTable("CompraDetalles"); builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoProducto).HasMaxLength(80).IsRequired(); builder.Property(x => x.NombreProducto).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UnidadMedida).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Cantidad).HasPrecision(18, 4); builder.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2); builder.Property(x => x.Igv).HasPrecision(18, 2); builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompraId, x.ProductoId }).IsUnique();
        builder.HasOne(x => x.Compra).WithMany(x => x.Detalles).HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}
