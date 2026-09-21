using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AlmacenCloud.MySqlIntegrationTests;

public sealed class MySqlApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:MySql", connectionString);
        builder.UseSetting("Jwt:Issuer", "AlmacenCloud.MySqlTests");
        builder.UseSetting("Jwt:Audience", "AlmacenCloud.MySqlTests.Client");
        builder.UseSetting("Jwt:Secret", "mysql-integration-tests-only-secret-at-least-32-bytes");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
    }
}
