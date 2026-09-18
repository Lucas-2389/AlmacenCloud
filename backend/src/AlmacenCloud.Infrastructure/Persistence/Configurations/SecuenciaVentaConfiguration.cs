using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class SecuenciaVentaConfiguration : IEntityTypeConfiguration<SecuenciaVenta>
{
    public void Configure(EntityTypeBuilder<SecuenciaVenta> builder)
    {
        builder.ToTable("SecuenciasVenta"); builder.HasKey(x => x.EmpresaId); builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
