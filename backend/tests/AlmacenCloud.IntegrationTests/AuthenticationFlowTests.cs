using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace AlmacenCloud.IntegrationTests;

public sealed class AuthenticationFlowTests(AlmacenCloudApiFactory factory) : IClassFixture<AlmacenCloudApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task RegisterTwoCompanies_LoginAndTenantIsolationClaims_WorkAsExpected()
    {
        var companyA = Registration("20123456789", "Empresa A SAC", "admin.a@empresa.com");
        var companyB = Registration("20987654321", "Empresa B SAC", "admin.b@empresa.com");

        var responseA = await _client.PostAsJsonAsync("/api/v1/auth/register-company", companyA);
        var responseB = await _client.PostAsJsonAsync("/api/v1/auth/register-company", companyB);
        Assert.Equal(HttpStatusCode.Created, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, responseB.StatusCode);

        var registeredA = await ReadJson(responseA);
        var registeredB = await ReadJson(responseB);
        var empresaAId = registeredA.GetProperty("empresaId").GetGuid();
        var empresaBId = registeredB.GetProperty("empresaId").GetGuid();
        Assert.NotEqual(empresaAId, empresaBId);

        var loginA = await Login("admin.a@empresa.com", "Secure123!");
        var loginB = await Login("admin.b@empresa.com", "Secure123!");
        Assert.Equal(HttpStatusCode.OK, loginA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginB.StatusCode);

        var tokenA = (await ReadJson(loginA)).GetProperty("accessToken").GetString()!;
        var tokenB = (await ReadJson(loginB)).GetProperty("accessToken").GetString()!;
        Assert.Equal(empresaAId.ToString(), Claim(tokenA, "empresa_id"));
        Assert.Equal(empresaBId.ToString(), Claim(tokenB, "empresa_id"));

        var wrongPassword = await Login("admin.a@empresa.com", "IncorrectPassword!");
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);

        var duplicateRuc = await _client.PostAsJsonAsync("/api/v1/auth/register-company",
            Registration("20123456789", "Duplicada SAC", "otra@empresa.com"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateRuc.StatusCode);

        var duplicateEmail = await _client.PostAsJsonAsync("/api/v1/auth/register-company",
            Registration("20444555666", "Otra SAC", "admin.a@empresa.com"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateEmail.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var me = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var meBody = await ReadJson(me);
        Assert.Equal(empresaAId, meBody.GetProperty("empresaId").GetGuid());
        Assert.Contains("ADMIN_EMPRESA", meBody.GetProperty("roles").EnumerateArray().Select(x => x.GetString()));

        var tenant = await _client.GetAsync("/api/v1/tenant/context");
        Assert.Equal(HttpStatusCode.OK, tenant.StatusCode);
        Assert.Equal(empresaAId, (await ReadJson(tenant)).GetProperty("empresaId").GetGuid());
    }

    [Fact]
    public async Task Me_WithoutJwt_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PasswordReset_IsGenericOneTimeAndChangesThePassword()
    {
        const string email = "reset@empresa.com";
        var registration = await _client.PostAsJsonAsync("/api/v1/auth/register-company",
            Registration("20555123456", "Empresa Reset SAC", email));
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var unknown = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "no-existe@empresa.com" });
        var requested = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, requested.StatusCode);

        var notifier = factory.Services.GetRequiredService<TestPasswordResetNotifier>();
        var token = notifier.TokenFor(email);
        var reset = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token, newPassword = "NuevaClave123!" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(email, "Secure123!")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(email, "NuevaClave123!")).StatusCode);

        var reuse = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token, newPassword = "OtraClave123!" });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }

    private Task<HttpResponseMessage> Login(string email, string password) =>
        _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    private static object Registration(string ruc, string razonSocial, string email) => new
    {
        ruc,
        razonSocial,
        nombreComercial = razonSocial,
        admin = new { nombre = "Admin", apellidos = "Prueba", email, password = "Secure123!" }
    };

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();

    private static string? Claim(string token, string type) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(x => x.Type == type).Value;
}
