# ADR-003: Venta e inventario en una sola transacción

- Estado: aceptada
- Fecha: 2026-09-18

## Decisión

La cabecera, detalles, movimientos y descuentos de inventario se confirman en una sola transacción MySQL. Antes de modificar existencias se cargan y validan todas las líneas. Los inventarios se procesan en orden ascendente por `ProductoId`, reduciendo ciclos de espera entre ventas multiproducto.

Cada descuento reutiliza compare-and-swap sobre `Inventario.Version`. Un conflicto revierte toda la transacción y reinicia la operación, con un máximo de cuatro intentos. Los deadlocks y timeouts MySQL 1213/1205 se consideran transitorios y usan el mismo límite. No se emplean locks en memoria.

`MovimientoInventario.VentaId` enlaza cada salida o reposición con su venta. La anulación repone todas las cantidades, crea movimientos `AJUSTE_ENTRADA` y cambia el estado de la venta dentro de otra transacción. La futura anulación tributaria SUNAT será un flujo adicional y no está cubierta por este estado interno.

## Consecuencias

- Un fallo en cualquier producto evita ventas, movimientos y descuentos parciales.
- El orden determinista disminuye deadlocks, aunque no los elimina; los reintentos acotados evitan bucles infinitos.
- EF InMemory no reproduce rollback ni aislamiento relacional. Se requieren pruebas posteriores contra MySQL dedicado.
