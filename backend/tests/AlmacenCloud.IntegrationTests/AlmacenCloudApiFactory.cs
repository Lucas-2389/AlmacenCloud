using AlmacenCloud.Infrastructure.Persistence;
using AlmacenCloud.Application.Abstractions;
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
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused;User=unused;Password=unused;");
        builder.UseSetting("Jwt:Issuer", "AlmacenCloud.Tests");
        builder.UseSetting("Jwt:Audience", "AlmacenCloud.Tests.Client");
        builder.UseSetting("Jwt:Secret", "integration-tests-only-secret-with-at-least-32-bytes");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
        builder.UseSetting("PasswordReset:Enabled", "true");
        builder.UseSetting("PasswordReset:FrontendBaseUrl", "http://localhost");
        builder.UseSetting("Smtp:Host", "test.invalid");
        builder.UseSetting("Smtp:FromAddress", "tests@example.test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AlmacenCloudDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AlmacenCloudDbContext>>();
            services.RemoveAll<AlmacenCloudDbContext>();
            services.AddDbContext<AlmacenCloudDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.RemoveAll<IPasswordResetNotifier>();
            services.AddSingleton<TestPasswordResetNotifier>();
            services.AddSingleton<IPasswordResetNotifier>(sp => sp.GetRequiredService<TestPasswordResetNotifier>());
        });
    }
}

public sealed class TestPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly Dictionary<string, string> _tokens = new(StringComparer.OrdinalIgnoreCase);
    public Task SendAsync(string email, string name, string rawToken, CancellationToken cancellationToken)
    {
        lock (_tokens) _tokens[email] = rawToken;
        return Task.CompletedTask;
    }

    public string TokenFor(string email)
    {
        lock (_tokens) return _tokens[email];
    }
}
