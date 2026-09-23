import './App.css'
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

export default function App() {
  const routes: Record<string, ReactNode> = {
    '/register': <RegisterPage />, '/login': <LoginPage />, '/dashboard': <DashboardPage />, '/categorias': <CategoriasPage />, '/productos': <ProductosPage />, '/almacenes': <AlmacenesPage />, '/inventario': <InventarioPage />, '/clientes': <ClientesPage />, '/proveedores': <ProveedoresPage />, '/ventas': <VentasPage />, '/ventas/nueva': <NuevaVentaPage />, '/compras': <ComprasPage />, '/compras/nueva': <NuevaCompraPage />,
  }
  const saleMatch = window.location.pathname.match(/^\/ventas\/([0-9a-f-]{36})$/i)
  const purchaseMatch = window.location.pathname.match(/^\/compras\/([0-9a-f-]{36})$/i)
  return routes[window.location.pathname] ?? (saleMatch ? <VentaDetallePage id={saleMatch[1]} /> : purchaseMatch ? <CompraDetallePage id={purchaseMatch[1]} /> : <LoginPage />)
}
