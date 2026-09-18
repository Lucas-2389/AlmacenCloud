using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AlmacenCloud.IntegrationTests;

public sealed class InventoryCoreTests(AlmacenCloudApiFactory factory) : IClassFixture<AlmacenCloudApiFactory>
{
    [Fact]
    public async Task InventoryCore_RespectsTenantStockTransferAuditAndConcurrency()
    {
        var clientA = await TenantClient("20111111111", "inventory.a@test.com");
        var clientB = await TenantClient("20222222222", "inventory.b@test.com");
        var userAId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(clientA.Token).Claims.Single(x => x.Type == "sub").Value);

        var categoryA = await Create(clientA.Client, "/api/v1/categorias", new { nombre = "Bebidas", descripcion = "Tenant A" });
        var categoryAId = categoryA.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.Client.GetAsync($"/api/v1/categorias/{categoryAId}")).StatusCode);

        var categoryB = await Create(clientB.Client, "/api/v1/categorias", new { nombre = "Bebidas", descripcion = "Tenant B" });
        var productRequestA = Product(categoryAId, "SKU-001", "Agua");
        var productA = await Create(clientA.Client, "/api/v1/productos", productRequestA);
        var productAId = productA.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await clientA.Client.PostAsJsonAsync("/api/v1/productos", productRequestA)).StatusCode);

        var productB = await clientB.Client.PostAsJsonAsync("/api/v1/productos", Product(categoryB.GetProperty("id").GetGuid(), "SKU-001", "Agua B"));
        Assert.Equal(HttpStatusCode.Created, productB.StatusCode);

        var origin = await Create(clientA.Client, "/api/v1/almacenes", new { codigo = "ALM-01", nombre = "Principal", direccion = "Lima" });
        var destination = await Create(clientA.Client, "/api/v1/almacenes", new { codigo = "ALM-02", nombre = "Secundario", direccion = "Callao" });
        var originId = origin.GetProperty("id").GetGuid();
        var destinationId = destination.GetProperty("id").GetGuid();

        var forbiddenMovement = await clientB.Client.PostAsJsonAsync("/api/v1/inventario/entrada",
            Movement(originId, productAId, 1, "Intento cruzado"));
        Assert.Equal(HttpStatusCode.NotFound, forbiddenMovement.StatusCode);

        Assert.Equal(10m, await MoveAndQuantity(clientA.Client, "entrada", originId, productAId, 10));
        Assert.Equal(15m, await MoveAndQuantity(clientA.Client, "entrada", originId, productAId, 5));
        Assert.Equal(12m, await MoveAndQuantity(clientA.Client, "salida", originId, productAId, 3));

        var excessiveExit = await clientA.Client.PostAsJsonAsync("/api/v1/inventario/salida", Movement(originId, productAId, 20, "Salida excesiva"));
        Assert.Equal(HttpStatusCode.Conflict, excessiveExit.StatusCode);
        Assert.Equal(12m, await InventoryQuantity(clientA.Client, originId, productAId));

        var transfer = await clientA.Client.PostAsJsonAsync("/api/v1/inventario/transferencia",
            new { productoId = productAId, almacenOrigenId = originId, almacenDestinoId = destinationId, cantidad = 5, motivo = "Reposición" });
        Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);
        Assert.Equal(7m, await InventoryQuantity(clientA.Client, originId, productAId));
        Assert.Equal(5m, await InventoryQuantity(clientA.Client, destinationId, productAId));

        var history = await GetJson(clientA.Client, "/api/v1/inventario/movimientos?page=1&pageSize=100");
        var movements = history.GetProperty("items").EnumerateArray().ToArray();
        var transferMovements = movements.Where(x => x.GetProperty("transferenciaId").ValueKind != JsonValueKind.Null).ToArray();
        Assert.Equal(2, transferMovements.Length);
        Assert.Equal(transferMovements[0].GetProperty("transferenciaId").GetGuid(), transferMovements[1].GetProperty("transferenciaId").GetGuid());
        Assert.All(movements, x => Assert.Equal(userAId, x.GetProperty("usuarioId").GetGuid()));

        var inventoryList = await GetJson(clientA.Client, $"/api/v1/inventario?almacenId={originId}&productoId={productAId}");
        Assert.Single(inventoryList.EnumerateArray());

        var exits = await Task.WhenAll(
            clientA.Client.PostAsJsonAsync("/api/v1/inventario/salida", Movement(originId, productAId, 5, "Concurrente 1")),
            clientA.Client.PostAsJsonAsync("/api/v1/inventario/salida", Movement(originId, productAId, 5, "Concurrente 2")));
        Assert.Single(exits, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(exits, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(2m, await InventoryQuantity(clientA.Client, originId, productAId));
    }

    private async Task<(HttpClient Client, string Token)> TenantClient(string ruc, string email)
    {
        var client = factory.CreateClient();
        var registration = new { ruc, razonSocial = $"Empresa {ruc}", admin = new { nombre = "Admin", apellidos = "Test", email, password = "Secure123!" } };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register-company", registration)).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Secure123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await Read(login)).GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token);
    }

    private static object Product(Guid categoryId, string code, string name) => new
    {
        categoriaId = categoryId, codigo = code, nombre = name, descripcion = "Prueba", unidadMedida = "UNIDAD",
        precioCompra = 1.25m, precioVenta = 2.50m, stockMinimo = 2m, afectoIgv = true
    };
    private static object Movement(Guid warehouseId, Guid productId, decimal quantity, string reason) =>
        new { almacenId = warehouseId, productoId = productId, cantidad = quantity, motivo = reason, referencia = "TEST" };
    private static async Task<JsonElement> Create(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await Read(response);
    }
    private static async Task<decimal> MoveAndQuantity(HttpClient client, string operation, Guid warehouseId, Guid productId, decimal quantity)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/inventario/{operation}", Movement(warehouseId, productId, quantity, operation));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await Read(response)).GetProperty("cantidad").GetDecimal();
    }
    private static async Task<decimal> InventoryQuantity(HttpClient client, Guid warehouseId, Guid productId) =>
        (await GetJson(client, $"/api/v1/inventario/{warehouseId}/{productId}")).GetProperty("cantidad").GetDecimal();
    private static async Task<JsonElement> GetJson(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await Read(response);
    }
    private static async Task<JsonElement> Read(HttpResponseMessage response) =>
        (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();
}
