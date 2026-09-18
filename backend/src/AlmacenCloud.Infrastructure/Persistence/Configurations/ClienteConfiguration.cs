using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes"); builder.HasKey(x => x.Id);
        builder.Property(x => x.TipoDocumento).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.NumeroDocumento).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NombreRazonSocial).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Direccion).HasMaxLength(500); builder.Property(x => x.Telefono).HasMaxLength(30); builder.Property(x => x.Email).HasMaxLength(254);
        builder.HasIndex(x => new { x.EmpresaId, x.TipoDocumento, x.NumeroDocumento }).IsUnique();
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
