using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class SecuenciaCompraConfiguration : IEntityTypeConfiguration<SecuenciaCompra>
{
    public void Configure(EntityTypeBuilder<SecuenciaCompra> builder)
    {
        builder.ToTable("SecuenciasCompra"); builder.HasKey(x => x.EmpresaId); builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
