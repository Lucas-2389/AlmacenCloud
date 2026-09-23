// Development defines VITE_API_URL=http://localhost:5022. Production leaves it
// empty so requests use the same origin that serves the React application.
const API_URL = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

type ApiError = { title?: string; detail?: string }

async function request<T>(path: string, options: RequestInit): Promise<T> {
  const token = sessionStorage.getItem('almacencloud_access_token')
  const isFormData = options.body instanceof FormData
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: { ...(!isFormData ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options.headers },
  })

  if (response.status === 401) {
    sessionStorage.removeItem('almacencloud_access_token')
    if (window.location.pathname !== '/login') window.location.href = '/login'
    throw new Error('Tu sesión venció. Inicia sesión nuevamente.')
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ApiError
    throw new Error(problem.detail ?? problem.title ?? 'No fue posible completar la solicitud.')
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export type LoginResponse = {
  accessToken: string
  expiresIn: number
  user: { id: string; nombre: string; email: string; empresaId: string; roles: string[] }
}

export type RegisterCompanyInput = {
  ruc: string
  razonSocial: string
  nombreComercial?: string
  admin: { nombre: string; apellidos: string; email: string; password: string }
}

export const authApi = {
  publicConfiguration: () => request<{ passwordResetEnabled: boolean }>('/api/v1/config/public', { method: 'GET' }),
  login: (email: string, password: string) =>
    request<LoginResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  registerCompany: (input: RegisterCompanyInput) =>
    request<{ empresaId: string; usuarioId: string }>('/api/v1/auth/register-company', {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  forgotPassword: (email: string) =>
    request<{ message: string }>('/api/v1/auth/forgot-password', { method: 'POST', body: JSON.stringify({ email }) }),
  resetPassword: (token: string, newPassword: string) =>
    request<{ message: string }>('/api/v1/auth/reset-password', { method: 'POST', body: JSON.stringify({ token, newPassword }) }),
}

export type Categoria = { id: string; nombre: string; descripcion?: string; activo: boolean }
export type Almacen = { id: string; codigo: string; nombre: string; direccion?: string; activo: boolean }
export type Producto = {
  id: string; categoriaId: string; categoria: string; codigo: string; nombre: string; descripcion?: string
  unidadMedida: string; precioCompra: number; precioVenta: number; stockMinimo: number; afectoIgv: boolean; activo: boolean; imagenUrl?: string
}
export type Inventario = {
  id: string; almacenId: string; almacen: string; productoId: string; codigoProducto: string; producto: string
  cantidad: number; stockMinimo: number; stockBajo: boolean; version: number; actualizadoEn: string
}
export type Movimiento = {
  id: string; almacenId: string; productoId: string; usuarioId: string; tipo: string; cantidad: number
  stockAnterior: number; stockPosterior: number; motivo: string; referencia?: string; transferenciaId?: string; ventaId?: string; compraId?: string; creadoEn: string
}
export type Cliente = { id: string; tipoDocumento: string; numeroDocumento: string; nombreRazonSocial: string; direccion?: string; telefono?: string; email?: string; activo: boolean }
export type Proveedor = { id: string; ruc: string; razonSocial: string; nombreComercial?: string; direccion?: string; telefono?: string; email?: string; activo: boolean }
export type VentaDetalle = { id: string; productoId: string; codigoProducto: string; nombreProducto: string; unidadMedida: string; cantidad: number; precioUnitario: number; subtotal: number; igv: number; total: number }
export type Venta = { id: string; numero: string; fecha: string; estado: string; clienteId?: string; cliente: string; almacenId: string; almacen: string; usuarioId: string; usuario: string; subtotal: number; igv: number; total: number; observacion?: string; anuladaPorUsuarioId?: string; anuladaEn?: string; detalles: VentaDetalle[] }
export type VentaLista = { id: string; numero: string; fecha: string; estado: string; cliente: string; almacen: string; total: number }
export type CompraDetalle = { id: string; productoId: string; codigoProducto: string; nombreProducto: string; unidadMedida: string; cantidad: number; precioUnitario: number; subtotal: number; igv: number; total: number }
export type Compra = { id: string; numero: string; fecha: string; estado: string; proveedorId: string; proveedor: string; almacenId: string; almacen: string; usuarioId: string; usuario: string; numeroDocumentoProveedor?: string; subtotal: number; igv: number; total: number; observacion?: string; anuladoPorUsuarioId?: string; anuladoEn?: string; detalles: CompraDetalle[] }
export type CompraLista = { id: string; numero: string; fecha: string; estado: string; proveedor: string; almacen: string; numeroDocumentoProveedor?: string; total: number }

export const inventoryApi = {
  categorias: () => request<Categoria[]>('/api/v1/categorias', { method: 'GET' }),
  saveCategoria: (id: string | undefined, body: { nombre: string; descripcion?: string }) =>
    request<Categoria>(id ? `/api/v1/categorias/${id}` : '/api/v1/categorias', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }),
  deleteCategoria: (id: string) => request<void>(`/api/v1/categorias/${id}`, { method: 'DELETE' }),

  productos: (search = '') => request<Producto[]>(`/api/v1/productos?search=${encodeURIComponent(search)}`, { method: 'GET' }),
  saveProducto: (id: string | undefined, body: Omit<Producto, 'id' | 'categoria' | 'activo'>) =>
    request<Producto>(id ? `/api/v1/productos/${id}` : '/api/v1/productos', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }),
  deleteProducto: (id: string) => request<void>(`/api/v1/productos/${id}`, { method: 'DELETE' }),
  uploadProductoImage: (id: string, file: File) => { const body = new FormData(); body.append('imagen', file); return request<Producto>(`/api/v1/productos/${id}/imagen`, { method: 'POST', body }) },
  deleteProductoImage: (id: string) => request<Producto>(`/api/v1/productos/${id}/imagen`, { method: 'DELETE' }),

  almacenes: () => request<Almacen[]>('/api/v1/almacenes', { method: 'GET' }),
  saveAlmacen: (id: string | undefined, body: { codigo: string; nombre: string; direccion?: string }) =>
    request<Almacen>(id ? `/api/v1/almacenes/${id}` : '/api/v1/almacenes', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }),
  deleteAlmacen: (id: string) => request<void>(`/api/v1/almacenes/${id}`, { method: 'DELETE' }),

  inventario: () => request<Inventario[]>('/api/v1/inventario', { method: 'GET' }),
  movimientos: () => request<{ items: Movimiento[] }>('/api/v1/inventario/movimientos?page=1&pageSize=50', { method: 'GET' }),
  movimiento: (operation: 'entrada' | 'salida', body: { almacenId: string; productoId: string; cantidad: number; motivo: string; referencia?: string }) =>
    request<Inventario>(`/api/v1/inventario/${operation}`, { method: 'POST', body: JSON.stringify(body) }),
  transferencia: (body: { productoId: string; almacenOrigenId: string; almacenDestinoId: string; cantidad: number; motivo: string }) =>
    request<void>('/api/v1/inventario/transferencia', { method: 'POST', body: JSON.stringify(body) }),
}

export const apiAssetUrl = (path?: string) => path ? `${API_URL}${path}` : undefined

export const salesApi = {
  clientes: (search = '') => request<Cliente[]>(`/api/v1/clientes?search=${encodeURIComponent(search)}`, { method: 'GET' }),
  saveCliente: (id: string | undefined, body: Omit<Cliente, 'id' | 'activo'>) => request<Cliente>(id ? `/api/v1/clientes/${id}` : '/api/v1/clientes', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }),
  deleteCliente: (id: string) => request<void>(`/api/v1/clientes/${id}`, { method: 'DELETE' }),
  proveedores: (search = '') => request<Proveedor[]>(`/api/v1/proveedores?search=${encodeURIComponent(search)}`, { method: 'GET' }),
  saveProveedor: (id: string | undefined, body: Omit<Proveedor, 'id' | 'activo'>) => request<Proveedor>(id ? `/api/v1/proveedores/${id}` : '/api/v1/proveedores', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }),
  deleteProveedor: (id: string) => request<void>(`/api/v1/proveedores/${id}`, { method: 'DELETE' }),
  ventas: () => request<{ items: VentaLista[]; total: number }>('/api/v1/ventas?page=1&pageSize=50', { method: 'GET' }),
  venta: (id: string) => request<Venta>(`/api/v1/ventas/${id}`, { method: 'GET' }),
  createVenta: (body: { clienteId?: string; almacenId: string; items: { productoId: string; cantidad: number; precioUnitario: number }[]; observacion?: string }) => request<Venta>('/api/v1/ventas', { method: 'POST', body: JSON.stringify(body) }),
  annulVenta: (id: string) => request<Venta>(`/api/v1/ventas/${id}/anular`, { method: 'POST' }),
}

export const purchasingApi = {
  compras: () => request<{ items: CompraLista[]; total: number }>('/api/v1/compras?page=1&pageSize=50', { method: 'GET' }),
  compra: (id: string) => request<Compra>(`/api/v1/compras/${id}`, { method: 'GET' }),
  createCompra: (body: { proveedorId: string; almacenId: string; numeroDocumentoProveedor?: string; items: { productoId: string; cantidad: number; precioUnitario: number }[]; observacion?: string }) =>
    request<Compra>('/api/v1/compras', { method: 'POST', body: JSON.stringify(body) }),
  annulCompra: (id: string) => request<Compra>(`/api/v1/compras/${id}/anular`, { method: 'POST' }),
}
