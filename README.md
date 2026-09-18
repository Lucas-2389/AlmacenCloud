# AlmacenCloud

Plataforma SaaS multitenant para la gestión de inventario, almacenes y ventas de pequeñas y medianas empresas peruanas.

## Stack

- Backend: C#, ASP.NET Core Web API, .NET 10, Entity Framework Core y MySQL.
- Frontend: React, TypeScript y Vite.

## Estructura

- `backend/src/AlmacenCloud.Domain`: modelo de dominio puro.
- `backend/src/AlmacenCloud.Application`: casos de uso y contratos.
- `backend/src/AlmacenCloud.Infrastructure`: persistencia e implementaciones externas.
- `backend/src/AlmacenCloud.API`: API REST versionada.
- `frontend`: cliente React.
- `docs`: documentación técnica y decisiones arquitectónicas.

## Endpoint de salud

`GET /api/v1/health`

## Estado actual

Solo contiene el esqueleto inicial. No incluye autenticación, entidades de negocio, migraciones, inventario funcional, ventas, SUNAT, Redis, AWS, Docker ni balanceador de carga.
