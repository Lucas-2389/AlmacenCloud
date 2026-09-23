# AlmacenCloud

Plataforma SaaS multitenant para la gestión empresarial de pequeñas y medianas empresas peruanas.

## Stack

- Backend: C#, ASP.NET Core Web API, .NET 10, Entity Framework Core 10 y MySQL.
- Frontend: React, TypeScript y Vite.
- Arquitectura: Domain, Application, Infrastructure y API.

## Configuración local segura

El repositorio no contiene contraseñas, cadenas de conexión reales ni secretos JWT. Configúralos para el proyecto API mediante user-secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=AlmacenCloud;User=TU_USUARIO;Password=TU_PASSWORD;" --project backend/src/AlmacenCloud.API
dotnet user-secrets set "Jwt:Secret" "UNA_CLAVE_ALEATORIA_DE_AL_MENOS_32_BYTES" --project backend/src/AlmacenCloud.API
```

También pueden utilizarse las variables de entorno `ConnectionStrings__DefaultConnection` y `Jwt__Secret`.

## Base de datos y ejecución

```powershell
dotnet ef database update --project backend/src/AlmacenCloud.Infrastructure --startup-project backend/src/AlmacenCloud.API
dotnet run --project backend/src/AlmacenCloud.API
```

En desarrollo, Swagger UI está disponible en `/swagger` y no se habilita fuera de dicho entorno.

## Frontend

```powershell
cd frontend
Copy-Item .env.example .env
npm install
npm run dev
```

Rutas iniciales: `/login` y `/register`. El token recibido se conserva en `sessionStorage`, nunca en el código fuente.

## API inicial

- `GET /api/v1/health`
- `POST /api/v1/auth/register-company`
- `POST /api/v1/auth/login`
- `GET /api/v1/auth/me`
- `GET /api/v1/tenant/context`

## Núcleo de inventario

- CRUD lógico de categorías, productos y almacenes bajo `/api/v1`.
- Consulta de existencias y stock bajo en `/api/v1/inventario`.
- Entradas, salidas y transferencias mediante operaciones transaccionales.
- Historial paginado en `GET /api/v1/inventario/movimientos`.
- Control de concurrencia distribuido mediante versión y actualización condicional en MySQL.

La estrategia de concurrencia está documentada en `docs/decisiones-arquitectonicas/ADR-002-concurrencia-inventario.md`.

## Clientes, proveedores y ventas

- CRUD lógico multitenant de clientes y proveedores.
- Ventas multiproducto con numeración generada por backend.
- Cálculo centralizado de IGV usando `SalesTax:Rate`; el precio unitario se interpreta como precio final incluido IGV.
- Descuento CAS de inventario, movimientos vinculados mediante `VentaId` y rollback completo ante fallos.
- Consulta paginada, detalle histórico con snapshot comercial y anulación con reposición de stock.

Las decisiones transaccionales y de numeración están documentadas en `ADR-003-transaccion-venta-inventario.md` y `ADR-004-numeracion-ventas.md`.

## Compras

- Registro multiproducto vinculado a proveedor y almacén.
- Numeración concurrente por empresa con formato `C00000001`.
- Entrada automática de inventario y movimientos auditables vinculados mediante `CompraId`.
- Consulta paginada, detalle con snapshot histórico y anulación sin permitir stock negativo.
- Pantallas `/compras`, `/compras/nueva` y `/compras/:id`.

Las decisiones se documentan en `ADR-005-transaccion-compra-inventario.md` y `ADR-006-numeracion-compras.md`.

## Pruebas

El manifiesto local fija `dotnet-ef` en la versión 10.0.9. Para restaurarlo y comprobarlo:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef --version
```

Las pruebas existentes de Domain, Application y API continúan usando EF Core InMemory. Las pruebas relacionales están aisladas en `AlmacenCloud.MySqlIntegrationTests` y requieren una base MySQL dedicada llamada **exactamente** `almacencloud_tests`.

La conexión puede configurarse sin almacenarla en Git mediante una de estas opciones:

```powershell
$env:ALMACENCLOUD_TEST_MYSQL_CONNECTION = "Server=localhost;Port=3306;Database=almacencloud_tests;User=TU_USUARIO;Password=TU_PASSWORD;"

dotnet user-secrets set "ConnectionStrings:MySqlTests" "Server=localhost;Port=3306;Database=almacencloud_tests;User=TU_USUARIO;Password=TU_PASSWORD;" --project backend/tests/AlmacenCloud.MySqlIntegrationTests
```

Ejecución separada:

```powershell
dotnet test backend/tests/AlmacenCloud.Domain.Tests/AlmacenCloud.Domain.Tests.csproj
dotnet test backend/tests/AlmacenCloud.Application.Tests/AlmacenCloud.Application.Tests.csproj
dotnet test backend/tests/AlmacenCloud.IntegrationTests/AlmacenCloud.IntegrationTests.csproj

dotnet test backend/tests/AlmacenCloud.MySqlIntegrationTests/AlmacenCloud.MySqlIntegrationTests.csproj
```

La suite MySQL no reutiliza la conexión de la API. Antes de abrir una conexión o ejecutar `EnsureDeleted`, valida el nombre exacto de la base; una conexión a cualquier otra base aborta las pruebas. Cada prueba funcional recrea `almacencloud_tests` y aplica todas las migraciones, por lo que el usuario MySQL de testing necesita permisos para crear y eliminar exclusivamente esa base. Sin conexión configurada, las pruebas relacionales se reportan como omitidas.

## Alcance pendiente

No se han implementado SUNAT, facturación electrónica, Redis, AWS, Docker, balanceo de carga ni reportes avanzados.
