[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5022',
    [Parameter(Mandatory)]
    [string]$AdminEmail,
    [Parameter(Mandatory)]
    [string]$ExpectedRazonSocial,
    [switch]$IncludeTransactions,
    [switch]$AllowInsecureHttp
)

$ErrorActionPreference = 'Stop'
$apiRoot = $BaseUrl.TrimEnd('/')
$uri = [Uri]$apiRoot
$isLoopback = $uri.IsLoopback -or $uri.Host -eq 'localhost'
if ($uri.Scheme -ne 'https' -and -not $isLoopback -and -not $AllowInsecureHttp) {
    throw 'Se rechazó el envío de credenciales por HTTP remoto. Use HTTPS o ejecute la API localmente conectada a la base demo. Use -AllowInsecureHttp solo si acepta explícitamente ese riesgo.'
}

function Invoke-JsonApi {
    param(
        [Parameter(Mandatory)][ValidateSet('GET', 'POST', 'PUT', 'DELETE')][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [object]$Body,
        [hashtable]$Headers = @{}
    )
    $parameters = @{ Method = $Method; Uri = "$apiRoot$Path"; Headers = $Headers }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json; charset=utf-8'
        $parameters.Body = $Body | ConvertTo-Json -Depth 12 -Compress
    }
    Invoke-RestMethod @parameters
}

function Index-By {
    param([object[]]$Items, [string]$Property)
    $index = @{}
    foreach ($item in $Items) { $index[[string]$item.$Property] = $item }
    return $index
}

function Get-AllPagedItems {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][hashtable]$Headers
    )
    $items = [System.Collections.Generic.List[object]]::new()
    $page = 1
    do {
        $separator = if ($Path.Contains('?')) { '&' } else { '?' }
        $response = Invoke-JsonApi GET "$Path${separator}page=$page&pageSize=100" $null $Headers
        foreach ($item in @($response.items)) { $items.Add($item) }
        $page++
    } while ($items.Count -lt [int]$response.total)
    return $items.ToArray()
}

Write-Host 'AlmacenCloud - carga explícita de datos de demostración' -ForegroundColor Cyan
$securePassword = Read-Host 'Contraseña del administrador (no se almacenará)' -AsSecureString
$credential = [System.Net.NetworkCredential]::new('', $securePassword)
try {
    $login = Invoke-JsonApi POST '/api/v1/auth/login' @{ email = $AdminEmail; password = $credential.Password }
}
finally {
    $credential = $null
    $securePassword.Dispose()
}

$headers = @{ Authorization = "Bearer $($login.accessToken)" }
$me = Invoke-JsonApi GET '/api/v1/auth/me' $null $headers
if (-not [string]::Equals([string]$me.razonSocial, $ExpectedRazonSocial, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Tenant incorrecto. La sesión pertenece a '$($me.razonSocial)' ($($me.empresaId)), no a '$ExpectedRazonSocial'. No se modificó ningún dato."
}
Write-Host "Tenant verificado: $($me.razonSocial) [$($me.empresaId)]" -ForegroundColor Green

$categoryDefinitions = @(
    @{ nombre = 'Herramientas eléctricas'; descripcion = 'Equipos eléctricos para construcción y mantenimiento.' },
    @{ nombre = 'Herramientas manuales'; descripcion = 'Herramientas de uso manual para trabajos generales.' },
    @{ nombre = 'Construcción'; descripcion = 'Materiales e insumos para obras.' },
    @{ nombre = 'Electricidad'; descripcion = 'Materiales para instalaciones eléctricas.' },
    @{ nombre = 'Plomería'; descripcion = 'Accesorios para instalaciones sanitarias.' },
    @{ nombre = 'Pinturas'; descripcion = 'Pinturas y accesorios de aplicación.' },
    @{ nombre = 'Seguridad industrial'; descripcion = 'Equipos de protección personal.' }
)
$categories = @(Invoke-JsonApi GET '/api/v1/categorias' $null $headers)
$categoryByName = Index-By $categories 'nombre'
foreach ($definition in $categoryDefinitions) {
    if (-not $categoryByName.ContainsKey($definition.nombre)) {
        $created = Invoke-JsonApi POST '/api/v1/categorias' $definition $headers
        $categoryByName[$created.nombre] = $created
        Write-Host "Categoría creada: $($created.nombre)"
    }
}

$productDefinitions = @(
    @{ codigo='TAL-001'; nombre='Taladro percutor 650W'; categoria='Herramientas eléctricas'; unidadMedida='UNIDAD'; precioCompra=145.00; precioVenta=189.90; stockMinimo=3 },
    @{ codigo='AMA-001'; nombre='Amoladora angular 4 1/2"'; categoria='Herramientas eléctricas'; unidadMedida='UNIDAD'; precioCompra=135.00; precioVenta=179.90; stockMinimo=3 },
    @{ codigo='MAR-001'; nombre='Martillo carpintero 16 oz'; categoria='Herramientas manuales'; unidadMedida='UNIDAD'; precioCompra=25.00; precioVenta=39.90; stockMinimo=5 },
    @{ codigo='DES-001'; nombre='Juego de destornilladores 6 piezas'; categoria='Herramientas manuales'; unidadMedida='UNIDAD'; precioCompra=34.00; precioVenta=49.90; stockMinimo=4 },
    @{ codigo='ALIC-001'; nombre='Alicate universal 8"'; categoria='Herramientas manuales'; unidadMedida='UNIDAD'; precioCompra=28.00; precioVenta=42.00; stockMinimo=4 },
    @{ codigo='CEM-001'; nombre='Cemento Portland Tipo I 42.5 kg'; categoria='Construcción'; unidadMedida='UNIDAD'; precioCompra=27.50; precioVenta=34.90; stockMinimo=20 },
    @{ codigo='CLA-001'; nombre='Clavos para madera 2"'; categoria='Construcción'; unidadMedida='KILOGRAMO'; precioCompra=6.50; precioVenta=9.50; stockMinimo=10 },
    @{ codigo='ALA-001'; nombre='Alambre galvanizado'; categoria='Construcción'; unidadMedida='KILOGRAMO'; precioCompra=7.50; precioVenta=11.00; stockMinimo=8 },
    @{ codigo='CAB-001'; nombre='Cable eléctrico THW 2.5 mm²'; categoria='Electricidad'; unidadMedida='METRO'; precioCompra=1.80; precioVenta=2.80; stockMinimo=100 },
    @{ codigo='INT-001'; nombre='Interruptor simple'; categoria='Electricidad'; unidadMedida='UNIDAD'; precioCompra=5.50; precioVenta=8.90; stockMinimo=15 },
    @{ codigo='TOM-001'; nombre='Tomacorriente doble'; categoria='Electricidad'; unidadMedida='UNIDAD'; precioCompra=7.00; precioVenta=11.50; stockMinimo=15 },
    @{ codigo='LED-001'; nombre='Foco LED 12W'; categoria='Electricidad'; unidadMedida='UNIDAD'; precioCompra=6.00; precioVenta=9.90; stockMinimo=20 },
    @{ codigo='PVC-001'; nombre='Tubo PVC 1/2" x 3 m'; categoria='Plomería'; unidadMedida='UNIDAD'; precioCompra=8.00; precioVenta=12.50; stockMinimo=15 },
    @{ codigo='COD-001'; nombre='Codo PVC 1/2"'; categoria='Plomería'; unidadMedida='UNIDAD'; precioCompra=0.80; precioVenta=1.50; stockMinimo=30 },
    @{ codigo='LLA-001'; nombre='Llave de paso PVC 1/2"'; categoria='Plomería'; unidadMedida='UNIDAD'; precioCompra=7.50; precioVenta=12.00; stockMinimo=10 },
    @{ codigo='PIN-001'; nombre='Pintura látex blanca 1 galón'; categoria='Pinturas'; unidadMedida='UNIDAD'; precioCompra=38.00; precioVenta=54.90; stockMinimo=8 },
    @{ codigo='ROD-001'; nombre='Rodillo para pintar 9"'; categoria='Pinturas'; unidadMedida='UNIDAD'; precioCompra=9.00; precioVenta=15.50; stockMinimo=10 },
    @{ codigo='BRO-001'; nombre='Brocha 3"'; categoria='Pinturas'; unidadMedida='UNIDAD'; precioCompra=6.00; precioVenta=10.50; stockMinimo=10 },
    @{ codigo='CAS-001'; nombre='Casco de seguridad'; categoria='Seguridad industrial'; unidadMedida='UNIDAD'; precioCompra=18.00; precioVenta=29.90; stockMinimo=6 },
    @{ codigo='GUA-001'; nombre='Guantes de trabajo reforzados'; categoria='Seguridad industrial'; unidadMedida='UNIDAD'; precioCompra=8.50; precioVenta=14.90; stockMinimo=10 }
)
$products = @(Invoke-JsonApi GET '/api/v1/productos?search=' $null $headers)
$productByCode = Index-By $products 'codigo'
foreach ($definition in $productDefinitions) {
    if (-not $productByCode.ContainsKey($definition.codigo)) {
        $body = @{
            categoriaId = $categoryByName[$definition.categoria].id
            codigo = $definition.codigo
            nombre = $definition.nombre
            descripcion = "Producto demo para $($definition.categoria.ToLowerInvariant())."
            unidadMedida = $definition.unidadMedida
            precioCompra = $definition.precioCompra
            precioVenta = $definition.precioVenta
            stockMinimo = $definition.stockMinimo
            afectoIgv = $true
        }
        $created = Invoke-JsonApi POST '/api/v1/productos' $body $headers
        $productByCode[$created.codigo] = $created
        Write-Host "Producto creado: $($created.codigo) - $($created.nombre)"
    }
}

$warehouseDefinitions = @(
    @{ codigo='ALM-01'; nombre='Almacén Principal'; direccion='Ayacucho' },
    @{ codigo='ALM-02'; nombre='Almacén Secundario'; direccion='San Juan Bautista' }
)
$warehouses = @(Invoke-JsonApi GET '/api/v1/almacenes' $null $headers)
$warehouseByCode = Index-By $warehouses 'codigo'
foreach ($definition in $warehouseDefinitions) {
    if (-not $warehouseByCode.ContainsKey($definition.codigo)) {
        $created = Invoke-JsonApi POST '/api/v1/almacenes' $definition $headers
        $warehouseByCode[$created.codigo] = $created
        Write-Host "Almacén creado: $($created.nombre)"
    }
}

$supplierDefinitions = @(
    @{ ruc='20999990003'; razonSocial='Distribuidora Andina Demo S.A.C.'; nombreComercial='Distribuidora Andina Demo'; direccion='Ayacucho'; telefono='066-900101'; email='ventas@andina.demo' },
    @{ ruc='20999990011'; razonSocial='Comercial Huamanga Demo S.R.L.'; nombreComercial='Comercial Huamanga Demo'; direccion='Huamanga'; telefono='066-900102'; email='pedidos@huamanga.demo' },
    @{ ruc='20999990020'; razonSocial='Suministros del Sur Demo S.A.C.'; nombreComercial='Suministros del Sur Demo'; direccion='San Juan Bautista'; telefono='066-900103'; email='contacto@surdemo.test' }
)
$suppliers = @(Invoke-JsonApi GET '/api/v1/proveedores?search=' $null $headers)
$supplierByRuc = Index-By $suppliers 'ruc'
foreach ($definition in $supplierDefinitions) {
    if (-not $supplierByRuc.ContainsKey($definition.ruc)) {
        $created = Invoke-JsonApi POST '/api/v1/proveedores' $definition $headers
        $supplierByRuc[$created.ruc] = $created
        Write-Host "Proveedor creado: $($created.razonSocial)"
    }
}

$clientDefinitions = @(
    @{ tipoDocumento='DNI'; numeroDocumento='70000001'; nombreRazonSocial='Ana Torres Demo'; direccion='Ayacucho'; telefono='900000101'; email='ana.torres@example.test' },
    @{ tipoDocumento='DNI'; numeroDocumento='70000002'; nombreRazonSocial='Luis Quispe Demo'; direccion='Carmen Alto'; telefono='900000102'; email='luis.quispe@example.test' },
    @{ tipoDocumento='DNI'; numeroDocumento='70000003'; nombreRazonSocial='María Cárdenas Demo'; direccion='San Juan Bautista'; telefono='900000103'; email='maria.cardenas@example.test' },
    @{ tipoDocumento='RUC'; numeroDocumento='20999990101'; nombreRazonSocial='Constructora Wari Demo S.A.C.'; direccion='Huamanga'; telefono='066-900201'; email='compras@wari.demo' },
    @{ tipoDocumento='RUC'; numeroDocumento='20999990128'; nombreRazonSocial='Servicios Quinua Demo E.I.R.L.'; direccion='Quinua'; telefono='066-900202'; email='administracion@quinua.demo' }
)
$clients = @(Invoke-JsonApi GET '/api/v1/clientes?search=' $null $headers)
$clientByDocument = Index-By $clients 'numeroDocumento'
foreach ($definition in $clientDefinitions) {
    if (-not $clientByDocument.ContainsKey($definition.numeroDocumento)) {
        $created = Invoke-JsonApi POST '/api/v1/clientes' $definition $headers
        $clientByDocument[$created.numeroDocumento] = $created
        Write-Host "Cliente creado: $($created.nombreRazonSocial)"
    }
}

$stockDefinitions = @(
    @{ codigo='TAL-001'; almacen='ALM-01'; cantidad=12 }, @{ codigo='AMA-001'; almacen='ALM-01'; cantidad=8 },
    @{ codigo='MAR-001'; almacen='ALM-01'; cantidad=25 }, @{ codigo='DES-001'; almacen='ALM-01'; cantidad=15 },
    @{ codigo='ALIC-001'; almacen='ALM-02'; cantidad=18 }, @{ codigo='CEM-001'; almacen='ALM-01'; cantidad=80 },
    @{ codigo='CLA-001'; almacen='ALM-02'; cantidad=35 }, @{ codigo='ALA-001'; almacen='ALM-02'; cantidad=24 },
    @{ codigo='CAB-001'; almacen='ALM-01'; cantidad=300 }, @{ codigo='INT-001'; almacen='ALM-02'; cantidad=30 },
    @{ codigo='TOM-001'; almacen='ALM-01'; cantidad=28 }, @{ codigo='LED-001'; almacen='ALM-01'; cantidad=45 },
    @{ codigo='PVC-001'; almacen='ALM-02'; cantidad=40 }, @{ codigo='COD-001'; almacen='ALM-02'; cantidad=60 },
    @{ codigo='LLA-001'; almacen='ALM-02'; cantidad=7 }, @{ codigo='PIN-001'; almacen='ALM-01'; cantidad=20 },
    @{ codigo='ROD-001'; almacen='ALM-01'; cantidad=18 }, @{ codigo='BRO-001'; almacen='ALM-01'; cantidad=5 },
    @{ codigo='CAS-001'; almacen='ALM-02'; cantidad=18 }, @{ codigo='GUA-001'; almacen='ALM-02'; cantidad=4 }
)
$existingReferences = @{}
foreach ($movement in @(Get-AllPagedItems '/api/v1/inventario/movimientos' $headers)) { if ($movement.referencia) { $existingReferences[[string]$movement.referencia] = $true } }
foreach ($definition in $stockDefinitions) {
    $reference = "DEMO-SEED-V1-STOCK-$($definition.codigo)-$($definition.almacen)"
    if (-not $existingReferences.ContainsKey($reference)) {
        $body = @{ almacenId=$warehouseByCode[$definition.almacen].id; productoId=$productByCode[$definition.codigo].id; cantidad=$definition.cantidad; motivo='Stock inicial de demostración'; referencia=$reference }
        $null = Invoke-JsonApi POST '/api/v1/inventario/entrada' $body $headers
        Write-Host "Stock demo registrado: $($definition.codigo) / $($definition.almacen) = $($definition.cantidad)"
    }
}

if ($IncludeTransactions) {
    Write-Host 'Creando compras y ventas demo faltantes...' -ForegroundColor Yellow
    $purchaseDefinitions = @(
        @{ marker='DEMO-COMPRA-001'; proveedor='20999990003'; almacen='ALM-01'; items=@(@{codigo='TAL-001';cantidad=5},@{codigo='CEM-001';cantidad=40},@{codigo='MAR-001';cantidad=10}) },
        @{ marker='DEMO-COMPRA-002'; proveedor='20999990011'; almacen='ALM-02'; items=@(@{codigo='CLA-001';cantidad=20},@{codigo='PVC-001';cantidad=25},@{codigo='INT-001';cantidad=20}) },
        @{ marker='DEMO-COMPRA-003'; proveedor='20999990020'; almacen='ALM-01'; items=@(@{codigo='CAB-001';cantidad=200},@{codigo='LED-001';cantidad=30},@{codigo='TOM-001';cantidad=15}) },
        @{ marker='DEMO-COMPRA-004'; proveedor='20999990003'; almacen='ALM-01'; items=@(@{codigo='PIN-001';cantidad=12},@{codigo='ROD-001';cantidad=10},@{codigo='DES-001';cantidad=8}) }
    )
    $purchaseMarkers = @{}
    foreach ($purchase in @(Get-AllPagedItems '/api/v1/compras' $headers)) { if ($purchase.numeroDocumentoProveedor) { $purchaseMarkers[[string]$purchase.numeroDocumentoProveedor] = $true } }
    foreach ($definition in $purchaseDefinitions) {
        if (-not $purchaseMarkers.ContainsKey($definition.marker)) {
            $items = @($definition.items | ForEach-Object { $p=$productByCode[$_.codigo]; @{ productoId=$p.id; cantidad=$_.cantidad; precioUnitario=$p.precioCompra } })
            $body = @{ proveedorId=$supplierByRuc[$definition.proveedor].id; almacenId=$warehouseByCode[$definition.almacen].id; numeroDocumentoProveedor=$definition.marker; observacion="Datos de demostración · $($definition.marker)"; items=$items }
            $created = Invoke-JsonApi POST '/api/v1/compras' $body $headers
            Write-Host "Compra demo creada: $($created.numero) / $($definition.marker)"
        }
    }

    $saleDefinitions = @(
        @{ marker='DEMO-VENTA-001'; cliente='70000001'; almacen='ALM-01'; items=@(@{codigo='TAL-001';cantidad=1},@{codigo='CEM-001';cantidad=10}) },
        @{ marker='DEMO-VENTA-002'; cliente='70000002'; almacen='ALM-01'; items=@(@{codigo='MAR-001';cantidad=3},@{codigo='LED-001';cantidad=5}) },
        @{ marker='DEMO-VENTA-003'; cliente='70000003'; almacen='ALM-02'; items=@(@{codigo='PVC-001';cantidad=5},@{codigo='CLA-001';cantidad=4}) },
        @{ marker='DEMO-VENTA-004'; cliente='20999990101'; almacen='ALM-01'; items=@(@{codigo='CAB-001';cantidad=25},@{codigo='TOM-001';cantidad=2}) },
        @{ marker='DEMO-VENTA-005'; cliente='20999990128'; almacen='ALM-01'; items=@(@{codigo='PIN-001';cantidad=2},@{codigo='ROD-001';cantidad=1}) },
        @{ marker='DEMO-VENTA-006'; cliente='70000001'; almacen='ALM-01'; items=@(@{codigo='AMA-001';cantidad=1},@{codigo='DES-001';cantidad=2}) }
    )
    $saleMarkers = @{}
    foreach ($sale in @(Get-AllPagedItems '/api/v1/ventas' $headers)) {
        $detail = Invoke-JsonApi GET "/api/v1/ventas/$($sale.id)" $null $headers
        if ($detail.observacion -like 'DEMO-VENTA-*') { $saleMarkers[[string]$detail.observacion] = $true }
    }
    foreach ($definition in $saleDefinitions) {
        if (-not $saleMarkers.ContainsKey($definition.marker)) {
            $items = @($definition.items | ForEach-Object { $p=$productByCode[$_.codigo]; @{ productoId=$p.id; cantidad=$_.cantidad; precioUnitario=$p.precioVenta } })
            $body = @{ clienteId=$clientByDocument[$definition.cliente].id; almacenId=$warehouseByCode[$definition.almacen].id; observacion=$definition.marker; items=$items }
            $created = Invoke-JsonApi POST '/api/v1/ventas' $body $headers
            Write-Host "Venta demo creada: $($created.numero) / $($definition.marker)"
        }
    }
}
else {
    Write-Host 'Compras y ventas no fueron creadas. Ejecute nuevamente con -IncludeTransactions para agregarlas de forma idempotente.' -ForegroundColor DarkYellow
}

Write-Host 'Carga demo terminada. Puede ejecutar el script nuevamente sin duplicar los datos marcados.' -ForegroundColor Green
