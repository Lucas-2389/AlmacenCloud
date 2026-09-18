using AlmacenCloud.Domain.Common;

namespace AlmacenCloud.Application.Tests;

public sealed class RoleNamesTests
{
    [Fact]
    public void InitialRole_IsAdminEmpresa()
    {
        Assert.Equal("ADMIN_EMPRESA", RoleNames.AdminEmpresa);
    }
}
