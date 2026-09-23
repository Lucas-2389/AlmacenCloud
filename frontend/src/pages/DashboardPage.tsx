import { useEffect, useMemo, useState } from 'react'
import { inventoryApi, purchasingApi, salesApi } from '../api/client'
import type { Almacen, Cliente, CompraLista, Inventario, Movimiento, Producto, VentaLista } from '../api/client'
import { AppLayout } from '../components/AppLayout'

const money = new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' })
const quantity = new Intl.NumberFormat('es-PE', { maximumFractionDigits: 4 })

export function DashboardPage() {
  const [sales, setSales] = useState<VentaLista[]>([])
  const [purchases, setPurchases] = useState<CompraLista[]>([])
  const [stock, setStock] = useState<Inventario[]>([])
  const [products, setProducts] = useState<Producto[]>([])
  const [warehouses, setWarehouses] = useState<Almacen[]>([])
  const [clients, setClients] = useState<Cliente[]>([])
  const [movements, setMovements] = useState<Movimiento[]>([])
  const [message, setMessage] = useState('')

  useEffect(() => {
    Promise.all([salesApi.ventas(), purchasingApi.compras(), inventoryApi.inventario(), inventoryApi.productos(), inventoryApi.almacenes(), salesApi.clientes(), inventoryApi.movimientos()])
      .then(([v, c, i, p, a, clientes, m]) => { setSales(v.items); setPurchases(c.items); setStock(i); setProducts(p); setWarehouses(a); setClients(clientes); setMovements(m.items) })
      .catch((error: Error) => setMessage(error.message))
  }, [])

  const activeSales = sales.filter(x => x.estado !== 'ANULADA')
  const activePurchases = purchases.filter(x => x.estado !== 'ANULADA')
  const today = new Date(); today.setHours(0, 0, 0, 0)
  const tomorrow = new Date(today); tomorrow.setDate(tomorrow.getDate() + 1)
  const todaySales = activeSales.filter(x => { const date = new Date(x.fecha); return date >= today && date < tomorrow })
  const salesIncome = todaySales.reduce((sum, x) => sum + x.total, 0)
  const purchaseExpense = activePurchases.reduce((sum, x) => sum + x.total, 0)
  const lowStock = stock.filter(x => x.stockBajo)
  const productById = useMemo(() => new Map(products.map(x => [x.id, x])), [products])
  const warehouseById = useMemo(() => new Map(warehouses.map(x => [x.id, x])), [warehouses])
  const chart = useMemo(() => Array.from({ length: 7 }, (_, index) => {
    const day = new Date(); day.setHours(0, 0, 0, 0); day.setDate(day.getDate() - (6 - index))
    const next = new Date(day); next.setDate(next.getDate() + 1)
    const total = activeSales.filter(x => { const date = new Date(x.fecha); return date >= day && date < next }).reduce((sum, x) => sum + x.total, 0)
    return { label: day.toLocaleDateString('es-PE', { weekday: 'short' }).replace('.', ''), total }
  }), [sales])
  const chartMax = Math.max(...chart.map(x => x.total), 1)

  return <AppLayout title="Resumen del negocio">
    <div className="dashboard-heading"><div><p className="eyebrow">AlmacenCloud · Panel general</p><p className="dashboard-subtitle">Gestión inteligente de inventarios, ventas y compras en un solo lugar.</p></div><a className="primary-link" href="/ventas/nueva">+ Nueva venta</a></div>
    {message && <p className="message">{message}</p>}
    <section className="metric-grid" aria-label="Indicadores principales">
      <article className="metric-card metric-primary"><span>Ventas de hoy</span><strong>{money.format(salesIncome)}</strong><small>{todaySales.length} ventas vigentes hoy</small></article>
      <article className="metric-card"><span>Compras registradas</span><strong>{money.format(purchaseExpense)}</strong><small>{activePurchases.length} compras vigentes</small></article>
      <article className="metric-card"><span>Productos registrados</span><strong>{products.length}</strong><small>Catálogo de la empresa</small></article>
      <article className={`metric-card ${lowStock.length ? 'metric-warning' : ''}`}><span>Alertas de stock</span><strong>{lowStock.length}</strong><small>{lowStock.length ? 'Productos por reponer' : 'Inventario en orden'}</small></article>
    </section>
    <section className="dashboard-grid">
      <article className="panel chart-panel"><div className="panel-title"><div><h2>Ventas de los últimos 7 días</h2><p>Solo ventas vigentes</p></div></div><div className="bar-chart">{chart.map(day => <div className="bar-column" key={day.label}><span className="bar-value">{day.total ? money.format(day.total) : '—'}</span><div className="bar-track"><div className="bar-fill" style={{ height: `${Math.max(day.total / chartMax * 100, day.total ? 8 : 0)}%` }} /></div><strong>{day.label}</strong></div>)}</div></article>
      <article className="panel"><div className="panel-title"><div><h2>Estado del sistema</h2><p>Datos activos</p></div></div><dl className="summary-list"><div><dt>Productos</dt><dd>{products.length}</dd></div><div><dt>Almacenes</dt><dd>{warehouses.length}</dd></div><div><dt>Clientes</dt><dd>{clients.length}</dd></div><div><dt>Stock total</dt><dd>{quantity.format(stock.reduce((sum, x) => sum + x.cantidad, 0))}</dd></div></dl></article>
    </section>
    <section className="dashboard-grid">
      <article className="panel"><div className="panel-title"><div><h2>Movimientos recientes</h2><p>Entradas y salidas de inventario</p></div><a href="/inventario">Ver inventario</a></div>{movements.length ? <div className="compact-list">{movements.slice(0, 6).map(x => <a href="/inventario" key={x.id}><span><strong>{x.tipo} · {productById.get(x.productoId)?.nombre ?? 'Producto'}</strong><small>{warehouseById.get(x.almacenId)?.nombre ?? 'Almacén'} · {x.motivo} · {new Date(x.creadoEn).toLocaleString('es-PE')}</small></span><b>{x.tipo === 'SALIDA' ? '−' : '+'}{quantity.format(x.cantidad)}</b></a>)}</div> : <p className="empty-state">Todavía no hay movimientos de inventario.</p>}</article>
      <article className="panel"><div className="panel-title"><div><h2>Compras recientes</h2><p>Abastecimiento registrado</p></div><a href="/compras">Ver compras</a></div>{activePurchases.length ? <div className="compact-list">{activePurchases.slice(0, 5).map(x => <a href={`/compras/${x.id}`} key={x.id}><span><strong>{x.numero}</strong><small>{x.proveedor} · {new Date(x.fecha).toLocaleDateString('es-PE')}</small></span><b>{money.format(x.total)}</b></a>)}</div> : <p className="empty-state">Todavía no hay compras registradas.</p>}</article>
    </section>
    <section className="dashboard-grid">
      <article className="panel"><div className="panel-title"><div><h2>Ventas recientes</h2><p>Últimos movimientos comerciales</p></div><a href="/ventas">Ver todas</a></div>{activeSales.length ? <div className="compact-list">{activeSales.slice(0, 5).map(x => <a href={`/ventas/${x.id}`} key={x.id}><span><strong>{x.numero}</strong><small>{x.cliente} · {new Date(x.fecha).toLocaleDateString('es-PE')}</small></span><b>{money.format(x.total)}</b></a>)}</div> : <p className="empty-state">Todavía no hay ventas registradas.</p>}</article>
      <article className="panel"><div className="panel-title"><div><h2>Stock por reponer</h2><p>Existencias iguales o menores al mínimo</p></div><a href="/inventario">Ver inventario</a></div>{lowStock.length ? <div className="compact-list">{lowStock.slice(0, 5).map(x => <a href="/inventario" key={x.id}><span><strong>{x.producto}</strong><small>{x.almacen} · mínimo {quantity.format(x.stockMinimo)}</small></span><b>{quantity.format(x.cantidad)}</b></a>)}</div> : <p className="empty-state">No hay alertas de stock.</p>}</article>
    </section>
  </AppLayout>
}
