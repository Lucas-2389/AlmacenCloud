using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("Empresas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Ruc).HasMaxLength(11).IsRequired();
        builder.HasIndex(x => x.Ruc).IsUnique();
        builder.Property(x => x.RazonSocial).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NombreComercial).HasMaxLength(200);
        builder.Property(x => x.Direccion).HasMaxLength(300);
        builder.Property(x => x.Telefono).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(254);
    }
}
