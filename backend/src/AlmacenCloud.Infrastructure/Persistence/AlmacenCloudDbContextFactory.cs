using AlmacenCloud.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlmacenCloud.Infrastructure.Persistence;

public sealed class AlmacenCloudDbContextFactory : IDesignTimeDbContextFactory<AlmacenCloudDbContext>
{
    public AlmacenCloudDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AlmacenCloudDbContext>()
            .UseMySQL("Server=localhost;Database=AlmacenCloud;User=design_time;Password=design_time;")
            .Options;
        return new AlmacenCloudDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public bool HasTenant => false;
        public Guid? EmpresaId => null;
    }
}
