using AlmacenCloud.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlmacenCloud.IntegrationTests;

public sealed class AlmacenCloudApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AlmacenCloudTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:MySql", "Server=unused;Database=unused;User=unused;Password=unused;");
        builder.UseSetting("Jwt:Issuer", "AlmacenCloud.Tests");
        builder.UseSetting("Jwt:Audience", "AlmacenCloud.Tests.Client");
        builder.UseSetting("Jwt:Secret", "integration-tests-only-secret-with-at-least-32-bytes");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AlmacenCloudDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AlmacenCloudDbContext>>();
            services.RemoveAll<AlmacenCloudDbContext>();
            services.AddDbContext<AlmacenCloudDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
