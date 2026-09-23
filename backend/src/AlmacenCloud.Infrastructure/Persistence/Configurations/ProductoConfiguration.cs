using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(1000);
        builder.Property(x => x.ImagenUrl).HasMaxLength(500);
        builder.Property(x => x.UnidadMedida).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.PrecioCompra).HasPrecision(18, 2);
        builder.Property(x => x.PrecioVenta).HasPrecision(18, 2);
        builder.Property(x => x.StockMinimo).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique();
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
    }
}
