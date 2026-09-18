using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmacenCloud.Infrastructure.Persistence.Configurations;

public sealed class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> builder)
    {
        builder.ToTable("UsuarioRoles");
        builder.HasKey(x => new { x.UsuarioId, x.RolId });
        builder.HasIndex(x => new { x.UsuarioId, x.RolId }).IsUnique();
        builder.HasOne(x => x.Usuario).WithMany(x => x.UsuarioRoles).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Rol).WithMany(x => x.UsuarioRoles).HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
    }
}
