import type { ReactNode } from 'react'

export function AppLayout({ title, children }: { title: string; children: ReactNode }) {
  const logout = () => { sessionStorage.removeItem('almacencloud_access_token'); window.location.href = '/login' }
  return <div className="app-shell">
    <header><strong>AlmacenCloud</strong><nav>
      <a href="/categorias">Categorías</a><a href="/productos">Productos</a><a href="/almacenes">Almacenes</a><a href="/inventario">Inventario</a><a href="/clientes">Clientes</a><a href="/proveedores">Proveedores</a><a href="/ventas">Ventas</a>
      <button type="button" onClick={logout}>Salir</button>
    </nav></header>
    <main className="page"><h1>{title}</h1>{children}</main>
  </div>
}
