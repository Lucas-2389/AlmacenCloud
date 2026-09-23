# Arquitectura inicial

AlmacenCloud aplica Clean Architecture: Domain no depende de otras capas; Application depende de Domain; Infrastructure depende de Application y Domain; API compone Application e Infrastructure.

La API usará rutas versionadas bajo `/api/v1`. Los servicios se diseñarán sin estado para facilitar una futura ejecución detrás de un balanceador de carga.

## Multitenancy

La aplicación usará una única base MySQL compartida. Cada entidad de negocio aislable incluirá `EmpresaId`; EF Core aplicará filtros globales de tenant. El tenant se obtendrá exclusivamente de los claims del JWT, no de valores confiados al cliente.

## Modelo previsto

Todos los identificadores serán GUID. Productos y proveedores permanecerán independientes en el MVP inicial. La relación `ProductoProveedor` se añadirá en una etapa posterior.

Inventario tendrá una restricción única compuesta: `EmpresaId + AlmacenId + ProductoId`. Sus cantidades usarán `decimal(18,4)` y los valores monetarios `decimal(18,2)`.

Para cambios futuros de stock se aplicará concurrencia optimista (token de versión) y actualizaciones transaccionales. Esto permitirá detectar conflictos de stock y evitar cantidades negativas ante operaciones simultáneas. Una transferencia producirá una salida y una entrada dentro de la misma transacción, vinculadas opcionalmente por `TransferenciaId`.

Los primeros roles previstos son `ADMIN_EMPRESA`, `VENDEDOR` y `ALMACENERO`; en la primera implementación solo se habilitará `ADMIN_EMPRESA`. Cada usuario pertenecerá obligatoriamente a una empresa.

## Ventas

Las ventas guardan snapshots comerciales de producto y se integran con inventario mediante una única transacción. La numeración se genera en backend con una secuencia CAS por empresa. Los precios enviados por el cliente se recalculan en backend; la tasa de IGV es configuración centralizada y el precio unitario se interpreta como precio final incluido IGV.

## Compras y ciclo operativo

```text
Proveedor
   ↓
Compra ──→ CompraDetalle (snapshot del producto)
   ↓
MovimientoInventario (COMPRA_ENTRADA)
   ↓
Inventario
   ↓
Venta ──→ MovimientoInventario (SALIDA)
```

Compras y ventas comparten el inventario, el cálculo central de IGV y la estrategia CAS, pero mantienen agregados, repositorios y secuencias independientes. Las anulaciones son compensaciones auditables; no eliminan cabeceras, detalles ni movimientos.

Las decisiones de Compras están documentadas en `decisiones-arquitectonicas/ADR-005-transaccion-compra-inventario.md` y `decisiones-arquitectonicas/ADR-006-numeracion-compras.md`.
