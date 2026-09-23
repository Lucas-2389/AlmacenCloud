# ADR-006: numeración concurrente de compras

## Estado

Aceptada.

## Decisión

Cada empresa mantiene una fila en `SecuenciasCompra`, cuya clave primaria es `EmpresaId`. `UltimoNumero` y `Version` se actualizan mediante CAS dentro de la misma transacción que registra la compra.

El backend genera el formato `C00000001`; nunca acepta el número desde el frontend. El índice único `(EmpresaId, Numero)` funciona como última barrera de consistencia.

Si dos solicitudes compiten por la primera fila o por la misma versión, una transacción gana y la otra revierte, vuelve a leer y reintenta. Por ello dos empresas pueden usar `C00000001`, mientras una misma empresa no puede repetir números incluso con varias instancias de API.
