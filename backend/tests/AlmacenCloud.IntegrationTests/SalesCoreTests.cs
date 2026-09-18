using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AlmacenCloud.IntegrationTests;

public sealed class SalesCoreTests(AlmacenCloudApiFactory factory) : IClassFixture<AlmacenCloudApiFactory>
{
    [Fact]
    public async Task SalesCore_RespectsTenantTransactionsSnapshotsAnnulmentAndConcurrency()
    {
        var clientA = await TenantClient("20333333333", "sales.a@test.com");
        var clientB = await TenantClient("20444444444", "sales.b@test.com");
        var customerA = await Create(clientA, "/api/v1/clientes", new { tipoDocumento = "DNI", numeroDocumento = "12345678", nombreRazonSocial = "Cliente A" });
        var customerAId = customerA.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/clientes/{customerAId}")).StatusCode);

        var supplier = new { ruc = "20555555555", razonSocial = "Proveedor SAC", nombreComercial = "Proveedor" };
        Assert.Equal(HttpStatusCode.Created, (await clientA.PostAsJsonAsync("/api/v1/proveedores", supplier)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await clientA.PostAsJsonAsync("/api/v1/proveedores", supplier)).StatusCode);

        var category = await Create(clientA, "/api/v1/categorias", new { nombre = "Ventas", descripcion = "Prueba" });
        var warehouse = await Create(clientA, "/api/v1/almacenes", new { codigo = "VENTA-01", nombre = "Tienda" });
        var warehouseId = warehouse.GetProperty("id").GetGuid();
        var product1 = await CreateProduct(clientA, category.GetProperty("id").GetGuid(), "SALE-1", "Producto histórico");
        var product2 = await CreateProduct(clientA, category.GetProperty("id").GetGuid(), "SALE-2", "Producto secundario");
        var product1Id = product1.GetProperty("id").GetGuid(); var product2Id = product2.GetProperty("id").GetGuid();
        await Entry(clientA, warehouseId, product1Id, 10); await Entry(clientA, warehouseId, product2Id, 1);

        var sale = await Create(clientA, "/api/v1/ventas", Sale(customerAId, warehouseId,
            new[] { Item(product1Id, 2, 11.80m), Item(product2Id, 1, 5m) }));
        var saleId = sale.GetProperty("id").GetGuid();
        Assert.Equal(8m, await Stock(clientA, warehouseId, product1Id));
        Assert.Equal(0m, await Stock(clientA, warehouseId, product2Id));
        Assert.Equal(24.24m, sale.GetProperty("subtotal").GetDecimal());
        Assert.Equal(4.36m, sale.GetProperty("igv").GetDecimal());
        Assert.Equal(28.60m, sale.GetProperty("total").GetDecimal());

        var movements = await Get(clientA, $"/api/v1/inventario/movimientos?page=1&pageSize=100");
        var saleMovements = movements.GetProperty("items").EnumerateArray().Where(x =>
            x.TryGetProperty("ventaId", out var value) && value.ValueKind != JsonValueKind.Null && value.GetGuid() == saleId).ToArray();
        Assert.Equal(2, saleMovements.Length);
        Assert.All(saleMovements, x => Assert.NotEqual(Guid.Empty, x.GetProperty("usuarioId").GetGuid()));

        var failed = await clientA.PostAsJsonAsync("/api/v1/ventas", Sale(customerAId, warehouseId,
            new[] { Item(product1Id, 1, 2m), Item(product2Id, 2, 5m) }));
        Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);
        Assert.Equal(8m, await Stock(clientA, warehouseId, product1Id));

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/ventas/{saleId}")).StatusCode);
        var crossTenantSale = await clientB.PostAsJsonAsync("/api/v1/ventas", Sale(null, warehouseId, new[] { Item(product1Id, 1, 1m) }));
        Assert.Equal(HttpStatusCode.NotFound, crossTenantSale.StatusCode);

        var changedProduct = new { categoriaId = category.GetProperty("id").GetGuid(), codigo = "SALE-1", nombre = "Nombre cambiado", descripcion = "Cambio", unidadMedida = "UNIDAD", precioCompra = 1m, precioVenta = 99m, stockMinimo = 0m, afectoIgv = true };
        Assert.Equal(HttpStatusCode.OK, (await clientA.PutAsJsonAsync($"/api/v1/productos/{product1Id}", changedProduct)).StatusCode);
        var historical = await Get(clientA, $"/api/v1/ventas/{saleId}");
        Assert.Contains(historical.GetProperty("detalles").EnumerateArray(), x => x.GetProperty("nombreProducto").GetString() == "Producto histórico");

        var annulled = await clientA.PostAsync($"/api/v1/ventas/{saleId}/anular", null);
        Assert.Equal(HttpStatusCode.OK, annulled.StatusCode);
        Assert.Equal(10m, await Stock(clientA, warehouseId, product1Id));
        Assert.Equal(1m, await Stock(clientA, warehouseId, product2Id));
        var inverse = (await Get(clientA, "/api/v1/inventario/movimientos?page=1&pageSize=100")).GetProperty("items").EnumerateArray()
            .Where(x => x.TryGetProperty("ventaId", out var value) && value.ValueKind != JsonValueKind.Null && value.GetGuid() == saleId && x.GetProperty("tipo").GetString() == "AJUSTE_ENTRADA").ToArray();
        Assert.Equal(2, inverse.Length);
        Assert.Equal(HttpStatusCode.Conflict, (await clientA.PostAsync($"/api/v1/ventas/{saleId}/anular", null)).StatusCode);

        var lastProduct = await CreateProduct(clientA, category.GetProperty("id").GetGuid(), "LAST-1", "Última unidad");
        var lastProductId = lastProduct.GetProperty("id").GetGuid(); await Entry(clientA, warehouseId, lastProductId, 1);
        var request = Sale(customerAId, warehouseId, new[] { Item(lastProductId, 1, 10m) });
        var concurrent = await Task.WhenAll(clientA.PostAsJsonAsync("/api/v1/ventas", request), clientA.PostAsJsonAsync("/api/v1/ventas", request));
        Assert.Single(concurrent, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(concurrent, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(0m, await Stock(clientA, warehouseId, lastProductId));
    }

    private async Task<HttpClient> TenantClient(string ruc, string email)
    {
        var client = factory.CreateClient();
        var registration = new { ruc, razonSocial = $"Empresa {ruc}", admin = new { nombre = "Admin", apellidos = "Ventas", email, password = "Secure123!" } };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register-company", registration)).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Secure123!" });
        var token = (await Read(login)).GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); return client;
    }
    private static async Task<JsonElement> Create(HttpClient client, string path, object body)
    { var response = await client.PostAsJsonAsync(path, body); Assert.Equal(HttpStatusCode.Created, response.StatusCode); return await Read(response); }
    private static Task<JsonElement> CreateProduct(HttpClient client, Guid categoryId, string code, string name) => Create(client, "/api/v1/productos",
        new { categoriaId = categoryId, codigo = code, nombre = name, unidadMedida = "UNIDAD", precioCompra = 1m, precioVenta = 11.80m, stockMinimo = 0m, afectoIgv = true });
    private static async Task Entry(HttpClient client, Guid warehouseId, Guid productId, decimal quantity) =>
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/inventario/entrada", new { almacenId = warehouseId, productoId = productId, cantidad = quantity, motivo = "Stock venta" })).StatusCode);
    private static object Sale(Guid? clientId, Guid warehouseId, object[] items) => new { clienteId = clientId, almacenId = warehouseId, items, observacion = "Test" };
    private static object Item(Guid productId, decimal quantity, decimal price) => new { productoId = productId, cantidad = quantity, precioUnitario = price };
    private static async Task<decimal> Stock(HttpClient client, Guid warehouse, Guid product) => (await Get(client, $"/api/v1/inventario/{warehouse}/{product}")).GetProperty("cantidad").GetDecimal();
    private static async Task<JsonElement> Get(HttpClient client, string path) { var response = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, response.StatusCode); return await Read(response); }
    private static async Task<JsonElement> Read(HttpResponseMessage response) => (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();
}
