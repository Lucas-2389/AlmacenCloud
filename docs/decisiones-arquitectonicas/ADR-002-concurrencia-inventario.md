# ADR-002: Concurrencia de inventario con compare-and-swap

- Estado: aceptada
- Fecha: 2026-09-18

## Problema

Dos solicitudes pueden leer el mismo stock y tratar de descontarlo simultáneamente. Un flujo basado únicamente en leer, validar y actualizar puede aprobar ambas salidas y producir stock negativo. Los locks en memoria no sirven cuando existen varias instancias de API.

## Decisión

`Inventario` tiene un contador `Version` configurado como token de concurrencia. Cada modificación se ejecuta en MySQL como una actualización condicional equivalente a:

```sql
UPDATE Inventarios
SET Cantidad = @nuevaCantidad, Version = @version + 1
WHERE Id = @id AND Version = @version;
```

La cantidad se valida antes de emitir la operación y existe además un `CHECK (Cantidad >= 0)`. Si otra solicitud modificó la fila, la versión ya no coincide y se actualizan cero filas. La transacción completa se revierte y la operación se reintenta con una lectura nueva, hasta un límite acotado.

Las transferencias actualizan origen y destino y crean ambos movimientos dentro de una única transacción. Un conflicto en cualquiera de las dos filas revierte toda la transferencia.

## Por qué funciona con varias instancias

La exclusión se decide mediante una condición atómica ejecutada por MySQL, no mediante estado del proceso ASP.NET. Todas las instancias compiten contra la misma versión almacenada en la base de datos; solo una puede actualizar una versión determinada.

## Limitaciones

- Bajo contención sostenida una operación puede agotar los reintentos y responder `409 Conflict`; el cliente puede reintentar.
- La creación concurrente de la primera fila se protege además con el índice único `(EmpresaId, AlmacenId, ProductoId)`.
- EF InMemory sirve para pruebas funcionales, pero no reproduce aislamiento, SQL condicional ni transacciones reales. Antes de producción se deben ejecutar pruebas relacionales y de carga contra una instancia MySQL dedicada para testing.
