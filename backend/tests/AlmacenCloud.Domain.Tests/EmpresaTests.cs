using AlmacenCloud.Domain.Entities;

namespace AlmacenCloud.Domain.Tests;

public sealed class EmpresaTests
{
    [Fact]
    public void Create_RejectsInvalidRuc()
    {
        Assert.Throws<ArgumentException>(() => Empresa.Create("123", "Empresa", null, null, null, null));
    }

    [Fact]
    public void Create_UsesGuidAndUtcAuditDates()
    {
        var empresa = Empresa.Create("20123456789", "Empresa", null, null, null, null);

        Assert.NotEqual(Guid.Empty, empresa.Id);
        Assert.Equal(DateTimeKind.Utc, empresa.CreadoEn.Kind);
        Assert.True(empresa.Activo);
    }
}
