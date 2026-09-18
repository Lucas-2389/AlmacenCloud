# AlmacenCloud

Plataforma SaaS multitenant para la gestión empresarial de pequeñas y medianas empresas peruanas.

## Stack

- Backend: C#, ASP.NET Core Web API, .NET 10, Entity Framework Core 10 y MySQL.
- Frontend: React, TypeScript y Vite.
- Arquitectura: Domain, Application, Infrastructure y API.

## Configuración local segura

El repositorio no contiene contraseñas, cadenas de conexión reales ni secretos JWT. Configúralos para el proyecto API mediante user-secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:MySql" "Server=localhost;Port=3306;Database=AlmacenCloud;User=TU_USUARIO;Password=TU_PASSWORD;" --project backend/src/AlmacenCloud.API
dotnet user-secrets set "Jwt:Secret" "UNA_CLAVE_ALEATORIA_DE_AL_MENOS_32_BYTES" --project backend/src/AlmacenCloud.API
```

También pueden utilizarse las variables de entorno `ConnectionStrings__MySql` y `Jwt__Secret`.

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

## Alcance pendiente

No se han implementado ventas, compras, SUNAT, Redis, AWS, Docker, balanceo de carga ni reportes avanzados.
