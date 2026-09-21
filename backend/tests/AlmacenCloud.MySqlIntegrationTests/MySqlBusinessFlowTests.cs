using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlmacenCloud.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlmacenCloud.MySqlIntegrationTests;

[Collection(MySqlCollection.Name)]
public sealed class MySqlBusinessFlowTests(MySqlTestDatabase database)
{
    [MySqlFact]
    public async Task Sale_IsAtomic_RollsBackWhenOneProductHasNoStock_AndAnnulmentRestoresStock()
    {
        await database.ResetAsync();
        using var factory = new MySqlApiFactory(database.RequireConnection());
        var tenant = await CreateTenant(factory, "20333333333", "mysql.sale@test.com", "A");
        var productA = await CreateProduct(tenant.Client, tenant.CategoryId, "TX-A", "Producto A");
        var productB = await CreateProduct(tenant.Client, tenant.CategoryId, "TX-B", "Producto B");
        await Entry(tenant.Client, tenant.WarehouseId, productA, 10);
        await Entry(tenant.Client, tenant.WarehouseId, productB, 1);

        var saleResponse = await tenant.Client.PostAsJsonAsync("/api/v1/ventas", Sale(tenant.WarehouseId,
            new[] { Item(productA, 2, 11.80m), Item(productB, 1, 5m) }));
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);
        var sale = await Read(saleResponse); var saleId = sale.GetProperty("id").GetGuid();

        await using (var context = database.CreateContext())
        {
            Assert.Equal(1, await context.Ventas.IgnoreQueryFilters().CountAsync());
            Assert.Equal(2, await context.VentaDetalles.IgnoreQueryFilters().CountAsync(x => x.VentaId == saleId));
            Assert.Equal(2, await context.MovimientosInventario.IgnoreQueryFilters().CountAsync(x => x.VentaId == saleId && x.TipoMovimiento == TipoMovimiento.Salida));
            Assert.Equal(8m, (await context.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.AlmacenId == tenant.WarehouseId && x.ProductoId == productA)).Cantidad);
            Assert.Equal(0m, (await context.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.AlmacenId == tenant.WarehouseId && x.ProductoId == productB)).Cantidad);
        }

        var failed = await tenant.Client.PostAsJsonAsync("/api/v1/ventas", Sale(tenant.WarehouseId,
            new[] { Item(productA, 3, 2m), Item(productB, 2, 5m) }));
        Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);
        await using (var rollbackCheck = database.CreateContext())
        {
            Assert.Equal(1, await rollbackCheck.Ventas.IgnoreQueryFilters().CountAsync());
            Assert.Equal(2, await rollbackCheck.VentaDetalles.IgnoreQueryFilters().CountAsync());
            Assert.Equal(2, await rollbackCheck.MovimientosInventario.IgnoreQueryFilters().CountAsync(x => x.VentaId != null));
            Assert.Equal(8m, (await rollbackCheck.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.AlmacenId == tenant.WarehouseId && x.ProductoId == productA)).Cantidad);
            Assert.Equal(0m, (await rollbackCheck.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.AlmacenId == tenant.WarehouseId && x.ProductoId == productB)).Cantidad);
        }

        Assert.Equal(HttpStatusCode.OK, (await tenant.Client.PostAsync($"/api/v1/ventas/{saleId}/anular", null)).StatusCode);
        await using (var annulCheck = database.CreateContext())
        {
            Assert.Equal(10m, (await annulCheck.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.AlmacenId == tenant.WarehouseId && x.ProductoId == productA)).Cantidad);
            Assert.Equal(1m, (await annulCheck.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.AlmacenId == tenant.WarehouseId && x.ProductoId == productB)).Cantidad);
            Assert.Equal(2, await annulCheck.MovimientosInventario.IgnoreQueryFilters().CountAsync(x => x.VentaId == saleId && x.TipoMovimiento == TipoMovimiento.AjusteEntrada));
        }
    }

    [MySqlFact]
    public async Task ConcurrentSales_ProtectLastUnit_AndGenerateUniqueTenantNumbers()
    {
        await database.ResetAsync();
        using var factory = new MySqlApiFactory(database.RequireConnection());
        var tenantA = await CreateTenant(factory, "20444444444", "mysql.concurrent.a@test.com", "A");
        var tenantB = await CreateTenant(factory, "20555555555", "mysql.concurrent.b@test.com", "B");
        var productA = await CreateProduct(tenantA.Client, tenantA.CategoryId, "NUM-A", "Numeración A");
        var productB = await CreateProduct(tenantB.Client, tenantB.CategoryId, "NUM-B", "Numeración B");
        await Entry(tenantA.Client, tenantA.WarehouseId, productA, 4);
        await Entry(tenantB.Client, tenantB.WarehouseId, productB, 1);

        var firstA = await CreateSale(tenantA.Client, tenantA.WarehouseId, productA, 1);
        var firstB = await CreateSale(tenantB.Client, tenantB.WarehouseId, productB, 1);
        Assert.Equal("V00000001", firstA.GetProperty("numero").GetString());
        Assert.Equal("V00000001", firstB.GetProperty("numero").GetString());

        var simultaneous = await Task.WhenAll(
            tenantA.Client.PostAsJsonAsync("/api/v1/ventas", Sale(tenantA.WarehouseId, new[] { Item(productA, 1, 10m) })),
            tenantA.Client.PostAsJsonAsync("/api/v1/ventas", Sale(tenantA.WarehouseId, new[] { Item(productA, 1, 10m) })));
        Assert.All(simultaneous, x => Assert.Equal(HttpStatusCode.Created, x.StatusCode));
        var concurrentNumbers = await Task.WhenAll(simultaneous.Select(async x => (await Read(x)).GetProperty("numero").GetString()));
        Assert.Equal(2, concurrentNumbers.Distinct().Count());

        var lastProduct = await CreateProduct(tenantA.Client, tenantA.CategoryId, "LAST-MYSQL", "Última unidad");
        await Entry(tenantA.Client, tenantA.WarehouseId, lastProduct, 1);
        var competing = await Task.WhenAll(
            tenantA.Client.PostAsJsonAsync("/api/v1/ventas", Sale(tenantA.WarehouseId, new[] { Item(lastProduct, 1, 10m) })),
            tenantA.Client.PostAsJsonAsync("/api/v1/ventas", Sale(tenantA.WarehouseId, new[] { Item(lastProduct, 1, 10m) })));
        Assert.Single(competing, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(competing, x => x.StatusCode == HttpStatusCode.Conflict);
        await using var context = database.CreateContext();
        Assert.Equal(0m, (await context.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.ProductoId == lastProduct)).Cantidad);
    }

    [MySqlFact]
    public async Task Transfer_CreatesExactlyTwoMovements_AndKeepsBalancesConsistent()
    {
        await database.ResetAsync();
        using var factory = new MySqlApiFactory(database.RequireConnection());
        var tenant = await CreateTenant(factory, "20666666666", "mysql.transfer@test.com", "T");
        var product = await CreateProduct(tenant.Client, tenant.CategoryId, "TRANSFER", "Transferencia");
        var destination = await Create(tenant.Client, "/api/v1/almacenes", new { codigo = "DEST", nombre = "Destino" });
        var destinationId = destination.GetProperty("id").GetGuid();
        await Entry(tenant.Client, tenant.WarehouseId, product, 10);
        Assert.Equal(HttpStatusCode.NoContent, (await tenant.Client.PostAsJsonAsync("/api/v1/inventario/transferencia",
            new { productoId = product, almacenOrigenId = tenant.WarehouseId, almacenDestinoId = destinationId, cantidad = 4, motivo = "Test MySQL" })).StatusCode);

        await using var context = database.CreateContext();
        var transfer = await context.MovimientosInventario.IgnoreQueryFilters().Where(x => x.TransferenciaId != null).ToArrayAsync();
        Assert.Equal(2, transfer.Length); Assert.Single(transfer.Select(x => x.TransferenciaId).Distinct());
        Assert.Equal(6m, (await context.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.AlmacenId == tenant.WarehouseId && x.ProductoId == product)).Cantidad);
        Assert.Equal(4m, (await context.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.AlmacenId == destinationId && x.ProductoId == product)).Cantidad);
    }

    private async Task<TenantSetup> CreateTenant(MySqlApiFactory factory, string ruc, string email, string suffix)
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register-company", new { ruc, razonSocial = $"Empresa {suffix}", admin = new { nombre = "Admin", apellidos = suffix, email, password = "Secure123!" } })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Secure123!" });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Read(login)).GetProperty("accessToken").GetString());
        var category = await Create(client, "/api/v1/categorias", new { nombre = $"Categoría {suffix}" });
        var warehouse = await Create(client, "/api/v1/almacenes", new { codigo = $"ALM-{suffix}", nombre = $"Almacén {suffix}" });
        return new(client, category.GetProperty("id").GetGuid(), warehouse.GetProperty("id").GetGuid());
    }
    private static async Task<Guid> CreateProduct(HttpClient client, Guid categoryId, string code, string name) =>
        (await Create(client, "/api/v1/productos", new { categoriaId = categoryId, codigo = code, nombre = name, unidadMedida = "UNIDAD", precioCompra = 1m, precioVenta = 10m, stockMinimo = 0m, afectoIgv = true })).GetProperty("id").GetGuid();
    private static async Task Entry(HttpClient client, Guid warehouse, Guid product, decimal quantity) => Assert.Equal(HttpStatusCode.OK,
        (await client.PostAsJsonAsync("/api/v1/inventario/entrada", new { almacenId = warehouse, productoId = product, cantidad = quantity, motivo = "Test MySQL" })).StatusCode);
    private static async Task<JsonElement> CreateSale(HttpClient client, Guid warehouse, Guid product, decimal quantity) => await Create(client, "/api/v1/ventas", Sale(warehouse, new[] { Item(product, quantity, 10m) }));
    private static object Sale(Guid warehouse, object[] items) => new { almacenId = warehouse, items, observacion = "MySQL test" };
    private static object Item(Guid product, decimal quantity, decimal price) => new { productoId = product, cantidad = quantity, precioUnitario = price };
    private static async Task<JsonElement> Create(HttpClient client, string path, object body) { var response = await client.PostAsJsonAsync(path, body); Assert.Equal(HttpStatusCode.Created, response.StatusCode); return await Read(response); }
    private static async Task<JsonElement> Read(HttpResponseMessage response) => (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();
    private sealed record TenantSetup(HttpClient Client, Guid CategoryId, Guid WarehouseId);
}
