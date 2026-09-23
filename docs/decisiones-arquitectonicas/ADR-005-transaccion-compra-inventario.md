# ADR-005: transacción de compra e inventario

## Estado

Aceptada.

## Decisión

El registro y la anulación de una compra se ejecutan en una única transacción de base de datos. La compra, sus detalles, los cambios CAS de inventario y los movimientos históricos se confirman o revierten juntos.

Los precios unitarios se interpretan como importes finales incluidos IGV, igual que en ventas, y el backend recalcula los importes con la configuración central `SalesTax:Rate`. Los cálculos del frontend son solo una vista previa.

Se agregan los tipos `CompraEntrada` y `AnulacionCompraSalida` al final del enum existente para conservar los valores numéricos anteriores y mejorar la auditoría. Cada movimiento conserva `CompraId`, usuario, almacén, producto, stock anterior y posterior.

La anulación no elimina registros. Solo procede cuando el inventario actual permite retirar todas las cantidades originales sin producir stock negativo.

## Concurrencia

El inventario existente se actualiza mediante comparación de `Version` (CAS). Cuando todavía no existe, se intenta crear y el índice único `(EmpresaId, AlmacenId, ProductoId)` resuelve carreras entre instancias; un conflicto transitorio revierte y reintenta toda la operación.

No se utilizan locks locales, por lo que la solución funciona con múltiples instancias de API coordinadas por MySQL.
