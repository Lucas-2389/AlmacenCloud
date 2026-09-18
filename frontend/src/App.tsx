import './App.css'
import type { ReactNode } from 'react'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { CategoriasPage } from './pages/CategoriasPage'
import { ProductosPage } from './pages/ProductosPage'
import { AlmacenesPage } from './pages/AlmacenesPage'
import { InventarioPage } from './pages/InventarioPage'

export default function App() {
  const routes: Record<string, ReactNode> = {
    '/register': <RegisterPage />, '/login': <LoginPage />, '/categorias': <CategoriasPage />, '/productos': <ProductosPage />, '/almacenes': <AlmacenesPage />, '/inventario': <InventarioPage />,
  }
  return routes[window.location.pathname] ?? <LoginPage />
}
