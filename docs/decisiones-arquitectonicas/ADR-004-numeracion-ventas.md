# ADR-004: Numeración concurrente de ventas

- Estado: aceptada
- Fecha: 2026-09-18

## Decisión

Cada empresa posee una fila en `SecuenciasVenta`, identificada por `EmpresaId`, con `UltimoNumero` y `Version`. La aplicación intenta avanzar la secuencia mediante una actualización condicional por versión dentro de la misma transacción de la venta.

El número visible usa inicialmente el formato `V00000001`. El frontend no puede proponerlo ni modificarlo. El índice único `(EmpresaId, Numero)` actúa como última defensa ante duplicados.

## Concurrencia

Dos instancias que leen la misma versión no pueden reservar el mismo correlativo: solo una actualización CAS afecta una fila. La otra transacción se revierte, relee la secuencia y reintenta. Al pertenecer la secuencia a la transacción, una venta fallida no confirma su incremento.

## Limitaciones

- La secuencia es continua por empresa mientras todas las operaciones usen esta transacción; fallos externos posteriores al commit no recuperan números.
- Una futura numeración fiscal SUNAT requerirá series y reglas documentarias independientes.
