import './App.css'
import { useEffect } from 'react'
import type { ReactNode } from 'react'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { CategoriasPage } from './pages/CategoriasPage'
import { ProductosPage } from './pages/ProductosPage'
import { AlmacenesPage } from './pages/AlmacenesPage'
import { InventarioPage } from './pages/InventarioPage'
import { ClientesPage } from './pages/ClientesPage'
import { ProveedoresPage } from './pages/ProveedoresPage'
import { VentasPage } from './pages/VentasPage'
import { NuevaVentaPage } from './pages/NuevaVentaPage'
import { VentaDetallePage } from './pages/VentaDetallePage'
import { ComprasPage } from './pages/ComprasPage'
import { NuevaCompraPage } from './pages/NuevaCompraPage'
import { CompraDetallePage } from './pages/CompraDetallePage'
import { DashboardPage } from './pages/DashboardPage'

const SESSION_KEY = 'almacencloud_access_token'

function Redirect({ to }: { to: string }) {
  useEffect(() => { window.location.replace(to) }, [to])
  return null
}

export default function App() {
  const path = window.location.pathname
  const isAuthenticated = Boolean(sessionStorage.getItem(SESSION_KEY))
  const publicRoutes: Record<string, ReactNode> = {
    '/register': <RegisterPage />,
    '/login': <LoginPage />,
  }
  const protectedRoutes: Record<string, ReactNode> = {
    '/dashboard': <DashboardPage />, '/categorias': <CategoriasPage />, '/productos': <ProductosPage />, '/almacenes': <AlmacenesPage />, '/inventario': <InventarioPage />, '/clientes': <ClientesPage />, '/proveedores': <ProveedoresPage />, '/ventas': <VentasPage />, '/ventas/nueva': <NuevaVentaPage />, '/compras': <ComprasPage />, '/compras/nueva': <NuevaCompraPage />,
  }

  if (path === '/') return <Redirect to={isAuthenticated ? '/dashboard' : '/login'} />
  if (publicRoutes[path]) return publicRoutes[path]

  const saleMatch = path.match(/^\/ventas\/([0-9a-f-]{36})$/i)
  const purchaseMatch = path.match(/^\/compras\/([0-9a-f-]{36})$/i)
  const protectedPage = protectedRoutes[path]
    ?? (saleMatch ? <VentaDetallePage id={saleMatch[1]} /> : purchaseMatch ? <CompraDetallePage id={purchaseMatch[1]} /> : undefined)

  if (!protectedPage) return <Redirect to={isAuthenticated ? '/dashboard' : '/login'} />
  return isAuthenticated ? protectedPage : <Redirect to="/login" />
}
