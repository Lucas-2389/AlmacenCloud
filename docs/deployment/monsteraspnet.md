# Despliegue de AlmacenCloud en MonsterASP.NET

Esta guía prepara una publicación de **un solo website ASP.NET Core**. Durante el desarrollo, React y la API continúan ejecutándose por separado; en producción, ASP.NET Core sirve tanto `/api/v1/...` como la SPA compilada.

## Requisitos locales

- Visual Studio 2022 con soporte para ASP.NET Core y Web Deploy.
- SDK de .NET 10.
- Node.js y npm disponibles en el equipo que publica.
- `dotnet-ef` 10.0.9.
- Acceso al panel de MonsterASP.NET.

El proceso `dotnet publish` ejecuta automáticamente `npm ci` y `npm run build`. MonsterASP.NET recibe los archivos ya compilados y no necesita ejecutar Vite ni un servidor Node.js.

## 1. Crear los recursos en MonsterASP.NET

1. Crear una cuenta desde la página de [MonsterASP.NET](https://www.monsterasp.net/).
2. En el panel, abrir **Websites** y crear un website gratuito.
3. Conservar el subdominio gratuito asignado por la plataforma.
4. Abrir **Databases**, elegir **Add database**, seleccionar el plan gratuito y el tipo **MySQL**. El procedimiento general está descrito en la [documentación oficial de creación de bases](https://help.monsterasp.net/books/databases/page/create-database).
5. Guardar de forma segura los valores que muestra el panel:
   - host y puerto MySQL;
   - nombre de la base;
   - usuario;
   - contraseña.

No agregar estos valores a `appsettings.json`, al frontend, a un `.pubxml` ni al repositorio.

> Importante: la tabla de planes de MonsterASP.NET indica que el plan gratuito no incluye actualmente HTTPS/Let's Encrypt. Verificar esta condición en el panel antes de una demostración pública. No se debe transmitir una contraseña o un JWT por HTTP en un entorno real; para uso público, activar HTTPS mediante una opción compatible del proveedor o usar un plan que lo incluya.

## 2. Configuración de producción

AlmacenCloud utiliza la configuración estándar de ASP.NET Core. Configurar estos nombres en el mecanismo de variables/configuración que ofrezca el panel:

| Clave de ASP.NET Core | Variable de entorno | Contenido |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Cadena MySQL entregada por Monster |
| `Jwt:Secret` | `Jwt__Secret` | Secreto aleatorio largo y exclusivo de producción |
| `Jwt:Issuer` | `Jwt__Issuer` | `AlmacenCloud` |
| `Jwt:Audience` | `Jwt__Audience` | `AlmacenCloud.Web` |
| `Jwt:ExpirationMinutes` | `Jwt__ExpirationMinutes` | `60` |
| `SalesTax:Rate` | `SalesTax__Rate` | `0.18` |
| `PasswordReset:Enabled` | `PasswordReset__Enabled` | `false` por defecto; `true` para activar recuperación |
| `PasswordReset:FrontendBaseUrl` | `PasswordReset__FrontendBaseUrl` | URL pública HTTPS, sin `/` final |
| `Smtp:Host` | `Smtp__Host` | Host del proveedor SMTP |
| `Smtp:Port` | `Smtp__Port` | Normalmente `587` |
| `Smtp:EnableSsl` | `Smtp__EnableSsl` | `true` |
| `Smtp:FromAddress` | `Smtp__FromAddress` | Remitente verificado |
| `Smtp:FromName` | `Smtp__FromName` | `AlmacenCloud` |
| `Smtp:Username` | `Smtp__Username` | Usuario SMTP (secreto) |
| `Smtp:Password` | `Smtp__Password` | Contraseña SMTP (secreto) |
| Entorno | `ASPNETCORE_ENVIRONMENT` | `Production` |

La variable `ConnectionStrings__DefaultConnection` debe usar el host remoto mostrado por Monster, nunca `localhost`. No registrar valores de secretos en capturas, logs ni documentación.

Si el panel no ofrece variables de entorno directamente, usar únicamente el mecanismo de configuración privada recomendado por el proveedor. No crear un `appsettings.Production.json` con credenciales versionadas.

La recuperación está deshabilitada por defecto en `Production`. Con `PasswordReset__Enabled=false`, la aplicación inicia normalmente sin SMTP y el frontend oculta el enlace de recuperación. Los endpoints permanecen disponibles pero responden `503` sin revelar datos de cuentas.

Para activarla posteriormente, configurar `PasswordReset__Enabled=true`, `PasswordReset__FrontendBaseUrl=https://...`, `Smtp__Host`, `Smtp__Port=587`, `Smtp__EnableSsl=true`, `Smtp__FromAddress`, `Smtp__FromName=AlmacenCloud`, `Smtp__Username` y `Smtp__Password`. Solo cuando la función está habilitada, el arranque valida SMTP y exige una URL pública HTTPS. El remitente debe estar verificado con el proveedor; las credenciales SMTP no pertenecen a Git.

## 3. Aplicar migraciones a MySQL

Las migraciones no se ejecutan durante el arranque de la aplicación. Esto evita que varias instancias intenten modificar simultáneamente el esquema.

Desde un equipo autorizado que pueda alcanzar el servidor MySQL, abrir una terminal de PowerShell, definir temporalmente la cadena recibida del panel y ejecutar:

```powershell
$env:ConnectionStrings__DefaultConnection = '<cadena MySQL proporcionada por Monster>'
dotnet ef database update `
  --project backend/src/AlmacenCloud.Infrastructure `
  --startup-project backend/src/AlmacenCloud.API `
  -- --environment Production
Remove-Item Env:ConnectionStrings__DefaultConnection
```

La cadena real no debe guardarse en un script. Antes de aplicar futuras migraciones, crear un respaldo desde el panel si el plan lo permite. Si Monster restringe conexiones MySQL externas, utilizar la herramienta administrativa que ofrezca su panel o solicitar el método admitido al soporte; no habilitar migraciones automáticas al arrancar como solución alternativa.

## 4. Compilar y verificar antes de publicar

Desde la raíz del repositorio:

```powershell
npm --prefix frontend ci
npm --prefix frontend run build
dotnet build AlmacenCloud.sln
dotnet test AlmacenCloud.sln
dotnet publish backend/src/AlmacenCloud.API `
  -c Release `
  -o publish/AlmacenCloud
```

El proyecto de la API ejecuta nuevamente el build reproducible del frontend durante `dotnet publish`. El resultado debe contener `publish/AlmacenCloud/wwwroot/index.html` y `publish/AlmacenCloud/wwwroot/assets/`.

En producción no se define `VITE_API_URL`; el navegador solicita rutas relativas como `/api/v1/productos` en el mismo dominio. Para desarrollo, `frontend/.env.development` puede contener solamente:

```dotenv
VITE_API_URL=http://localhost:5022
```

## 5. Publicar con Visual Studio y WebDeploy

MonsterASP.NET documenta este flujo en su guía oficial [How to deploy .NET Core Web Application using Visual Studio](https://help.monsterasp.net/books/deploy/page/how-to-deploy-net-core-web-application-using-visual-studio):

1. Abrir el website en el panel de MonsterASP.NET.
2. Activar la cuenta de **WebDeploy**.
3. Descargar el perfil `.publishSettings`.
4. Abrir `AlmacenCloud.sln` en Visual Studio.
5. En Solution Explorer, hacer clic derecho sobre **AlmacenCloud.API** y seleccionar **Publish**.
6. Elegir **Import Profile** e importar el archivo descargado.
7. Revisar que la configuración sea **Release** y que el destino corresponda al website correcto.
8. Pulsar **Publish**.
9. No agregar el `.publishSettings` ni archivos `*.pubxml.user` a Git. Estos patrones están ignorados por el repositorio.

El perfil contiene información sensible de despliegue. Guardarlo fuera del repositorio y eliminar copias innecesarias.

Mientras las fotos de productos se almacenen en `wwwroot/uploads`, no activar la opción de WebDeploy que elimina archivos adicionales en el destino: podría borrar imágenes cargadas por usuarios durante una publicación. Respaldar esa carpeta antes de publicar. Para una etapa posterior conviene mover estos archivos a almacenamiento persistente externo.

## 6. Comprobación posterior

Abrir la URL pública y validar:

1. `/` carga React.
2. `/login` y `/productos` cargan React incluso al abrirlas directamente.
3. `/api/v1/health` responde JSON con estado `ok`.
4. `/api/v1/ruta-inexistente` responde `404` y no devuelve `index.html`.
5. `/swagger` no está disponible en `Production` por defecto.
6. Registrar una empresa de demostración.
7. Solicitar recuperación desde `/forgot-password`, abrir el correo y comprobar que el enlace de un solo uso permite cambiar la contraseña.
7. Iniciar sesión.
8. Probar productos, compras, inventario y ventas con pocos datos.
9. Revisar los logs de aplicación del panel sin copiar secretos.

## 7. Desarrollo local

La integración de publicación no cambia el flujo diario:

```powershell
dotnet run --project backend/src/AlmacenCloud.API
```

En otra terminal:

```powershell
cd frontend
npm run dev
```

En `Development`, CORS permite únicamente `http://localhost:5173`. En `Production`, React y la API comparten origen y no se habilita esa política CORS.

## 8. HTTPS, proxy y Swagger

- No hay URLs HTTPS hardcodeadas en el proyecto.
- `UseHttpsRedirection` permanece activo fuera de `Testing`. Si los logs del hosting muestran bucles o redirecciones incorrectas, primero revisar la configuración HTTPS y de proxy indicada por Monster antes de agregar forwarded headers.
- Al ejecutar localmente el artefacto `Production` únicamente sobre HTTP puede aparecer `Failed to determine the https port for redirect`; es esperable cuando no existe un endpoint HTTPS local y no implica un error del bundle ni del fallback SPA.
- No se agregaron forwarded headers de forma especulativa: su configuración segura requiere conocer los proxies/redes confiables del hosting.
- Swagger permanece habilitado solamente en `Development`. Para una demostración remota, es preferible usar una herramienta cliente o habilitar Swagger de manera explícita y temporal mediante una decisión posterior, no dejarlo público por defecto.

## Datos que faltan del panel

Para completar el despliegue real todavía se necesitan:

- URL/subdominio público asignado;
- cadena MySQL real;
- mecanismo exacto del panel para configuración privada;
- secreto JWT de producción;
- perfil WebDeploy;
- confirmación de HTTPS disponible para el website elegido;
- confirmación de acceso remoto a MySQL para aplicar migraciones.
