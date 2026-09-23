using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlmacenCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlmacenCloud.MySqlIntegrationTests;

[Collection(MySqlCollection.Name)]
public sealed class MySqlPurchasingTests(MySqlTestDatabase database)
{
    [MySqlFact]
    public async Task PurchaseNumberUniqueConstraint_IsEnforcedPerTenant()
    {
        await database.ResetAsync();
        var company = Empresa.Create("20303030303", "Empresa constraint", null, null, null, null);
        var user = Usuario.Create(company.Id, "Admin", "Compras", "mysql.purchase.constraint@test.com");
        user.SetPasswordHash("not-a-real-password-hash");
        var supplier = Proveedor.Create(company.Id, "20404040404", "Proveedor constraint", null, null, null, null);
        var warehouse = Almacen.Create(company.Id, "CP-C", "Compras constraint", null);
        await using (var setup = database.CreateContext())
        {
            setup.AddRange(company, user, supplier, warehouse);
            await setup.SaveChangesAsync();
        }

        await using (var first = database.CreateContext(company.Id))
        {
            first.Compras.Add(Compra.Create(company.Id, supplier.Id, warehouse.Id, user.Id, "C00000001", 10, 1.8m, 11.8m, null, null));
            await first.SaveChangesAsync();
        }

        await using var duplicate = database.CreateContext(company.Id);
        duplicate.Compras.Add(Compra.Create(company.Id, supplier.Id, warehouse.Id, user.Id, "C00000001", 20, 3.6m, 23.6m, null, null));
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
    }

    [MySqlFact]
    public async Task ConcurrentPurchases_DoNotLoseStock_AndKeepTenantNumbersUnique()
    {
        await database.ResetAsync();
        using var factory = new MySqlApiFactory(database.RequireConnection());
        var tenantA = await Setup(factory, "20101010101", "mysql.purchase.a@test.com", "A");
        var tenantB = await Setup(factory, "20202020202", "mysql.purchase.b@test.com", "B");

        var concurrent = await Task.WhenAll(
            tenantA.Client.PostAsJsonAsync("/api/v1/compras", Purchase(tenantA, 10)),
            tenantA.Client.PostAsJsonAsync("/api/v1/compras", Purchase(tenantA, 5)));
        Assert.All(concurrent, x => Assert.Equal(HttpStatusCode.Created, x.StatusCode));
        var purchases = await Task.WhenAll(concurrent.Select(Read));
        var numbers = purchases.Select(x => x.GetProperty("numero").GetString()).ToArray();
        Assert.Equal(2, numbers.Distinct().Count());
        Assert.Contains("C00000001", numbers); Assert.Contains("C00000002", numbers);

        var tenantBPurchase = await tenantB.Client.PostAsJsonAsync("/api/v1/compras", Purchase(tenantB, 1));
        Assert.Equal(HttpStatusCode.Created, tenantBPurchase.StatusCode);
        Assert.Equal("C00000001", (await Read(tenantBPurchase)).GetProperty("numero").GetString());

        await using (var context = database.CreateContext())
        {
            Assert.Equal(15m, (await context.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.EmpresaId == tenantA.EmpresaId && x.ProductoId == tenantA.ProductId)).Cantidad);
            Assert.Equal(2, await context.Compras.IgnoreQueryFilters().CountAsync(x => x.EmpresaId == tenantA.EmpresaId));
            Assert.Equal(2, await context.CompraDetalles.IgnoreQueryFilters().CountAsync(x => x.EmpresaId == tenantA.EmpresaId));
            Assert.Equal(2, await context.MovimientosInventario.IgnoreQueryFilters().CountAsync(x => x.EmpresaId == tenantA.EmpresaId && x.CompraId != null));
        }

        var purchaseToAnnul = purchases.Single(x => x.GetProperty("detalles")[0].GetProperty("cantidad").GetDecimal() == 10m);
        var purchaseId = purchaseToAnnul.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await tenantA.Client.PostAsync($"/api/v1/compras/{purchaseId}/anular", null)).StatusCode);
        await using var annulCheck = database.CreateContext();
        Assert.Equal(5m, (await annulCheck.Inventarios.IgnoreQueryFilters().SingleAsync(x => x.EmpresaId == tenantA.EmpresaId && x.ProductoId == tenantA.ProductId)).Cantidad);
        Assert.Equal(1, await annulCheck.MovimientosInventario.IgnoreQueryFilters().CountAsync(x => x.CompraId == purchaseId && x.TipoMovimiento == Domain.Enums.TipoMovimiento.AnulacionCompraSalida));
    }

    private static async Task<TenantSetup> Setup(MySqlApiFactory factory, string ruc, string email, string suffix)
    {
        var client = factory.CreateClient();
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register-company", new { ruc, razonSocial = $"Empresa {suffix}", admin = new { nombre = "Admin", apellidos = suffix, email, password = "Secure123!" } });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var empresaId = (await Read(registration)).GetProperty("empresaId").GetGuid();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Secure123!" });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Read(login)).GetProperty("accessToken").GetString());
        var supplier = await Create(client, "/api/v1/proveedores", new { ruc = $"20{suffix[0]}00000000".Replace("A", "1").Replace("B", "2"), razonSocial = $"Proveedor {suffix}" });
        var category = await Create(client, "/api/v1/categorias", new { nombre = $"Compras {suffix}" });
        var warehouse = await Create(client, "/api/v1/almacenes", new { codigo = $"CP-{suffix}", nombre = $"Compras {suffix}" });
        var product = await Create(client, "/api/v1/productos", new { categoriaId = category.GetProperty("id").GetGuid(), codigo = $"CP-{suffix}", nombre = $"Producto {suffix}", unidadMedida = "UNIDAD", precioCompra = 11.80m, precioVenta = 15m, stockMinimo = 0m, afectoIgv = true });
        return new(client, empresaId, supplier.GetProperty("id").GetGuid(), warehouse.GetProperty("id").GetGuid(), product.GetProperty("id").GetGuid());
    }

    private static object Purchase(TenantSetup tenant, decimal quantity) => new { proveedorId = tenant.SupplierId, almacenId = tenant.WarehouseId, numeroDocumentoProveedor = $"DOC-{quantity}", items = new[] { new { productoId = tenant.ProductId, cantidad = quantity, precioUnitario = 11.80m } } };
    private static async Task<JsonElement> Create(HttpClient client, string path, object body) { var response = await client.PostAsJsonAsync(path, body); Assert.Equal(HttpStatusCode.Created, response.StatusCode); return await Read(response); }
    private static async Task<JsonElement> Read(HttpResponseMessage response) => (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();
    private sealed record TenantSetup(HttpClient Client, Guid EmpresaId, Guid SupplierId, Guid WarehouseId, Guid ProductId);
}
