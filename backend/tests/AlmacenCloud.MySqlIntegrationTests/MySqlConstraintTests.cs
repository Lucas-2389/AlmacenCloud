using System.Data.Common;
using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlmacenCloud.MySqlIntegrationTests;

[Collection(MySqlCollection.Name)]
public sealed class MySqlConstraintTests(MySqlTestDatabase database)
{
    [MySqlFact]
    public async Task Migrations_ApplyFromZero()
    {
        await database.ResetAsync();
        await using var context = database.CreateContext();
        var defined = context.Database.GetMigrations().ToArray();
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.NotEmpty(defined);
        Assert.Equal(defined, applied);
    }

    [MySqlFact]
    public async Task RequiredUniqueConstraints_AreEnforced()
    {
        await database.ResetAsync();
        var companyA = Empresa.Create("20123456789", "Empresa A", null, null, null, null);
        var companyB = Empresa.Create("20987654321", "Empresa B", null, null, null, null);
        await using (var setup = database.CreateContext())
        {
            setup.Empresas.AddRange(companyA, companyB);
            await setup.SaveChangesAsync();
        }

        await using (var duplicateRuc = database.CreateContext())
        {
            duplicateRuc.Empresas.Add(Empresa.Create("20123456789", "Duplicada", null, null, null, null));
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateRuc.SaveChangesAsync());
        }

        var userA = Usuario.Create(companyA.Id, "Admin", "A", "unique@test.com"); userA.SetPasswordHash("not-a-real-password-hash");
        var userB = Usuario.Create(companyB.Id, "Admin", "B", "unique@test.com"); userB.SetPasswordHash("not-a-real-password-hash");
        await using (var firstUser = database.CreateContext()) { firstUser.Usuarios.Add(userA); await firstUser.SaveChangesAsync(); }
        await using (var duplicateEmail = database.CreateContext())
        {
            duplicateEmail.Usuarios.Add(userB);
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateEmail.SaveChangesAsync());
        }

        var categoryA = Categoria.Create(companyA.Id, "General", null);
        var productA = Producto.Create(companyA.Id, categoryA.Id, "SKU-UNIQUE", "Producto 1", null, UnidadMedida.Unidad, 1, 2, 0, true);
        var warehouse = Almacen.Create(companyA.Id, "ALM-01", "Principal", null);
        await using (var catalog = database.CreateContext())
        {
            catalog.Categorias.Add(categoryA); catalog.Productos.Add(productA); catalog.Almacenes.Add(warehouse);
            await catalog.SaveChangesAsync();
        }
        await using (var duplicateProduct = database.CreateContext())
        {
            duplicateProduct.Productos.Add(Producto.Create(companyA.Id, categoryA.Id, "SKU-UNIQUE", "Producto 2", null, UnidadMedida.Unidad, 1, 2, 0, true));
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateProduct.SaveChangesAsync());
        }

        var inventory = Inventario.Create(companyA.Id, warehouse.Id, productA.Id, 5);
        await using (var stock = database.CreateContext()) { stock.Inventarios.Add(inventory); await stock.SaveChangesAsync(); }
        await using (var duplicateStock = database.CreateContext())
        {
            duplicateStock.Inventarios.Add(Inventario.Create(companyA.Id, warehouse.Id, productA.Id, 1));
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateStock.SaveChangesAsync());
        }
    }

    [MySqlFact]
    public async Task InventoryCheckConstraint_RejectsNegativeQuantity()
    {
        await database.ResetAsync();
        var company = Empresa.Create("20111111111", "Empresa", null, null, null, null);
        var category = Categoria.Create(company.Id, "General", null);
        var product = Producto.Create(company.Id, category.Id, "CHECK-1", "Producto", null, UnidadMedida.Unidad, 1, 2, 0, true);
        var warehouse = Almacen.Create(company.Id, "CHECK", "Almacén", null);
        var inventory = Inventario.Create(company.Id, warehouse.Id, product.Id, 1);
        await using var context = database.CreateContext();
        context.AddRange(company, category, product, warehouse, inventory);
        await context.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<DbException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Inventarios SET Cantidad = {-1m} WHERE Id = {inventory.Id}"));
    }
}
