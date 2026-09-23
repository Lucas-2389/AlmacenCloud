using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AlmacenCloud.IntegrationTests;

public sealed class PurchasingCoreTests(AlmacenCloudApiFactory factory) : IClassFixture<AlmacenCloudApiFactory>
{
    [Fact]
    public async Task PurchasingCore_RespectsTransactionsTenantsSnapshotsTotalsAndAnnulment()
    {
        var clientA = await TenantClient("20777777777", "purchases.a@test.com");
        var clientB = await TenantClient("20888888888", "purchases.b@test.com");
        var supplierA = await Create(clientA, "/api/v1/proveedores", new { ruc = "20123456780", razonSocial = "Proveedor A" });
        var supplierB = await Create(clientB, "/api/v1/proveedores", new { ruc = "20987654320", razonSocial = "Proveedor B" });
        var setupA = await Catalog(clientA, "A"); var setupB = await Catalog(clientB, "B");

        var purchaseA = await Create(clientA, "/api/v1/compras", Purchase(supplierA.GetProperty("id").GetGuid(), setupA.WarehouseId,
            new[] { Item(setupA.Product1Id, 2, 11.80m), Item(setupA.Product2Id, 1, 5m) }, "F001-123"));
        var purchaseAId = purchaseA.GetProperty("id").GetGuid();
        Assert.Equal("C00000001", purchaseA.GetProperty("numero").GetString());
        Assert.Equal(24.24m, purchaseA.GetProperty("subtotal").GetDecimal());
        Assert.Equal(4.36m, purchaseA.GetProperty("igv").GetDecimal());
        Assert.Equal(28.60m, purchaseA.GetProperty("total").GetDecimal());
        Assert.NotEqual(Guid.Empty, purchaseA.GetProperty("usuarioId").GetGuid());
        Assert.Equal(2m, await Stock(clientA, setupA.WarehouseId, setupA.Product1Id));
        Assert.Equal(1m, await Stock(clientA, setupA.WarehouseId, setupA.Product2Id));

        var movements = (await Get(clientA, "/api/v1/inventario/movimientos?page=1&pageSize=100")).GetProperty("items").EnumerateArray()
            .Where(x => x.TryGetProperty("compraId", out var value) && value.ValueKind != JsonValueKind.Null && value.GetGuid() == purchaseAId).ToArray();
        Assert.Equal(2, movements.Length);
        Assert.All(movements, x => Assert.Equal("COMPRA_ENTRADA", x.GetProperty("tipo").GetString()));
        Assert.All(movements, x => Assert.Equal(purchaseA.GetProperty("usuarioId").GetGuid(), x.GetProperty("usuarioId").GetGuid()));

        var beforeFailed = await Stock(clientA, setupA.WarehouseId, setupA.Product1Id);
        var failed = await clientA.PostAsJsonAsync("/api/v1/compras", Purchase(supplierA.GetProperty("id").GetGuid(), setupA.WarehouseId,
            new[] { Item(setupA.Product1Id, 4, 2m), Item(Guid.NewGuid(), 1, 2m) }, "FAIL"));
        Assert.Equal(HttpStatusCode.NotFound, failed.StatusCode);
        Assert.Equal(beforeFailed, await Stock(clientA, setupA.WarehouseId, setupA.Product1Id));

        var crossSupplier = await clientB.PostAsJsonAsync("/api/v1/compras", Purchase(supplierA.GetProperty("id").GetGuid(), setupB.WarehouseId,
            new[] { Item(setupB.Product1Id, 1, 1m) }, "CROSS"));
        Assert.Equal(HttpStatusCode.NotFound, crossSupplier.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/compras/{purchaseAId}")).StatusCode);

        var purchaseB = await Create(clientB, "/api/v1/compras", Purchase(supplierB.GetProperty("id").GetGuid(), setupB.WarehouseId,
            new[] { Item(setupB.Product1Id, 1, 10m) }, "B001"));
        Assert.Equal("C00000001", purchaseB.GetProperty("numero").GetString());

        Assert.Equal(HttpStatusCode.OK, (await clientA.PutAsJsonAsync($"/api/v1/productos/{setupA.Product1Id}", new
        {
            categoriaId = setupA.CategoryId, codigo = "PUR-A-1", nombre = "Nombre cambiado", descripcion = "Cambio",
            unidadMedida = "UNIDAD", precioCompra = 99m, precioVenta = 99m, stockMinimo = 0m, afectoIgv = true
        })).StatusCode);
        var historical = await Get(clientA, $"/api/v1/compras/{purchaseAId}");
        Assert.Contains(historical.GetProperty("detalles").EnumerateArray(), x => x.GetProperty("nombreProducto").GetString() == "Producto compra A 1");

        Assert.Equal(HttpStatusCode.OK, (await clientA.PostAsync($"/api/v1/compras/{purchaseAId}/anular", null)).StatusCode);
        Assert.Equal(0m, await Stock(clientA, setupA.WarehouseId, setupA.Product1Id));
        Assert.Equal(0m, await Stock(clientA, setupA.WarehouseId, setupA.Product2Id));
        var inverse = (await Get(clientA, "/api/v1/inventario/movimientos?page=1&pageSize=100")).GetProperty("items").EnumerateArray()
            .Where(x => x.TryGetProperty("compraId", out var value) && value.ValueKind != JsonValueKind.Null && value.GetGuid() == purchaseAId && x.GetProperty("tipo").GetString() == "ANULACION_COMPRA_SALIDA").ToArray();
        Assert.Equal(2, inverse.Length);
        Assert.Equal(HttpStatusCode.Conflict, (await clientA.PostAsync($"/api/v1/compras/{purchaseAId}/anular", null)).StatusCode);

        var limitedProduct = await CreateProduct(clientA, setupA.CategoryId, "PUR-LIMIT", "Stock consumido");
        var limitedId = limitedProduct.GetProperty("id").GetGuid();
        var limitedPurchase = await Create(clientA, "/api/v1/compras", Purchase(supplierA.GetProperty("id").GetGuid(), setupA.WarehouseId,
            new[] { Item(limitedId, 10, 1m) }, "LIMIT"));
        Assert.Equal(HttpStatusCode.OK, (await clientA.PostAsJsonAsync("/api/v1/inventario/salida", new
        { almacenId = setupA.WarehouseId, productoId = limitedId, cantidad = 8m, motivo = "Consumo posterior" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await clientA.PostAsync($"/api/v1/compras/{limitedPurchase.GetProperty("id").GetGuid()}/anular", null)).StatusCode);
        Assert.Equal(2m, await Stock(clientA, setupA.WarehouseId, limitedId));
    }

    private async Task<HttpClient> TenantClient(string ruc, string email)
    {
        var client = factory.CreateClient();
        var registration = new { ruc, razonSocial = $"Empresa {ruc}", admin = new { nombre = "Admin", apellidos = "Compras", email, password = "Secure123!" } };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register-company", registration)).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Secure123!" });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Read(login)).GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<CatalogSetup> Catalog(HttpClient client, string suffix)
    {
        var category = await Create(client, "/api/v1/categorias", new { nombre = $"Compras {suffix}" });
        var warehouse = await Create(client, "/api/v1/almacenes", new { codigo = $"PUR-{suffix}", nombre = $"Almacén {suffix}" });
        var product1 = await CreateProduct(client, category.GetProperty("id").GetGuid(), $"PUR-{suffix}-1", $"Producto compra {suffix} 1");
        var product2 = await CreateProduct(client, category.GetProperty("id").GetGuid(), $"PUR-{suffix}-2", $"Producto compra {suffix} 2");
        return new(category.GetProperty("id").GetGuid(), warehouse.GetProperty("id").GetGuid(), product1.GetProperty("id").GetGuid(), product2.GetProperty("id").GetGuid());
    }

    private static Task<JsonElement> CreateProduct(HttpClient client, Guid categoryId, string code, string name) => Create(client, "/api/v1/productos",
        new { categoriaId = categoryId, codigo = code, nombre = name, unidadMedida = "UNIDAD", precioCompra = 1m, precioVenta = 11.80m, stockMinimo = 0m, afectoIgv = true });
    private static object Purchase(Guid supplierId, Guid warehouseId, object[] items, string document) => new
    { proveedorId = supplierId, almacenId = warehouseId, numeroDocumentoProveedor = document, items, observacion = "Test", subtotal = 1, igv = 1, total = 1 };
    private static object Item(Guid productId, decimal quantity, decimal price) => new { productoId = productId, cantidad = quantity, precioUnitario = price };
    private static async Task<decimal> Stock(HttpClient client, Guid warehouse, Guid product) => (await Get(client, $"/api/v1/inventario/{warehouse}/{product}")).GetProperty("cantidad").GetDecimal();
    private static async Task<JsonElement> Create(HttpClient client, string path, object body) { var response = await client.PostAsJsonAsync(path, body); Assert.Equal(HttpStatusCode.Created, response.StatusCode); return await Read(response); }
    private static async Task<JsonElement> Get(HttpClient client, string path) { var response = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, response.StatusCode); return await Read(response); }
    private static async Task<JsonElement> Read(HttpResponseMessage response) => (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();
    private sealed record CatalogSetup(Guid CategoryId, Guid WarehouseId, Guid Product1Id, Guid Product2Id);
}
