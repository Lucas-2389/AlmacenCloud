# Datos para la demostración académica

El script `scripts/seed-demo.ps1` prepara una empresa ya registrada con datos ficticios y reconocibles. La carga es explícita: nunca se ejecuta al iniciar la API, no publica un endpoint de *seed* y no contiene usuarios, contraseñas, JWT ni cadenas de conexión.

## Antes de ejecutar

1. Cree o elija una empresa dedicada a la demostración y asegúrese de conocer la cuenta `ADMIN_EMPRESA`.
2. Inicie la API con la configuración de esa base de datos.
3. Confirme la razón social exacta de la empresa. El script valida este valor mediante `/api/v1/auth/me` antes de crear cualquier dato.

La opción más segura para una base remota es ejecutar la API localmente con sus secretos de desarrollo apuntando temporalmente a la base de demostración y sembrar contra `localhost`. Así la contraseña administrativa no viaja hacia un sitio HTTP público.

## Ejecución

Desde la raíz del repositorio:

```powershell
.\scripts\seed-demo.ps1 `
  -BaseUrl http://localhost:5022 `
  -AdminEmail admin.demo@example.test `
  -ExpectedRazonSocial "Ferretería Huamanga Demo S.A.C." `
  -IncludeTransactions
```

La contraseña se solicita de forma interactiva y no se almacena. Sustituya el correo y la razón social por los valores reales del tenant elegido. Si se omite `-IncludeTransactions`, solo se crean maestros y stock inicial.

El script rechaza HTTP cuando el destino no es local. `-AllowInsecureHttp` permite omitir esa protección de forma explícita, pero no se recomienda porque las credenciales y el token quedarían expuestos al transporte sin cifrar.

## Datos creados

- 7 categorías: herramientas eléctricas, herramientas manuales, construcción, electricidad, plomería, pinturas y seguridad industrial.
- 20 productos de ferretería con códigos `TAL-001` a `GUA-001`, precios de compra/venta y stock mínimo.
- 2 almacenes: `ALM-01` Almacén Principal y `ALM-02` Almacén Secundario.
- 3 proveedores ficticios claramente identificados como demo.
- 5 clientes ficticios: tres personas y dos empresas.
- Stock inicial mediante entradas auditadas. Quedan tres productos en alerta: `LLA-001`, `BRO-001` y `GUA-001`.
- Con `-IncludeTransactions`: 4 compras y 6 ventas mediante los servicios transaccionales existentes.

No se insertan filas directamente en MySQL. Las compras, ventas y movimientos pasan por la API autenticada y conservan las validaciones, transacciones, numeración, auditoría y aislamiento por `EmpresaId` de la aplicación.

## Idempotencia y seguridad multitenant

Los maestros se reconocen por sus claves naturales (nombre, código, RUC o documento). El stock inicial usa referencias `DEMO-SEED-V1-STOCK-*`; las compras usan `DEMO-COMPRA-*`; y las ventas, `DEMO-VENTA-*`. Ejecutar el script otra vez no debe duplicar estos datos.

La comprobación de `ExpectedRazonSocial` es obligatoria. Si la sesión corresponde a otra empresa, el proceso se detiene antes de la primera escritura. Aun así, use siempre una empresa exclusiva de demostración y verifique el destino antes de ingresar la contraseña.

## Limpieza

La estrategia recomendada es conservar una empresa exclusiva para demos y no mezclarla con información real. Si necesita revertir una presentación:

1. Anule primero las ventas demo desde la aplicación.
2. Anule después las compras demo.
3. Corrija el stock inicial únicamente con movimientos auditados.
4. Desactive clientes, proveedores, productos, categorías y almacenes que ya no se usarán.

No ejecute borrados SQL manuales: podrían romper relaciones, auditoría o consistencia de inventario.
