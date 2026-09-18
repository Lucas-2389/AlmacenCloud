using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class InventarioConfiguration : IEntityTypeConfiguration<Inventario>
{
    public void Configure(EntityTypeBuilder<Inventario> builder)
    {
        builder.ToTable("Inventarios", table => table.HasCheckConstraint("CK_Inventarios_Cantidad_NoNegativa", "`Cantidad` >= 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Cantidad).HasPrecision(18, 4);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.EmpresaId, x.AlmacenId, x.ProductoId }).IsUnique();
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}
