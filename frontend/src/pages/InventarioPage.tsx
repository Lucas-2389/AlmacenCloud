import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { inventoryApi } from '../api/client'
import type { Almacen, Inventario, Movimiento, Producto } from '../api/client'
import { AppLayout } from '../components/AppLayout'

export function InventarioPage() {
  const [stocks, setStocks] = useState<Inventario[]>([]), [moves, setMoves] = useState<Movimiento[]>([]), [products, setProducts] = useState<Producto[]>([]), [warehouses, setWarehouses] = useState<Almacen[]>([]), [message, setMessage] = useState('')
  const [operation, setOperation] = useState<'entrada' | 'salida' | 'transferencia'>('entrada')
  const [productoId, setProductoId] = useState(''), [almacenId, setAlmacenId] = useState(''), [destinoId, setDestinoId] = useState(''), [cantidad, setCantidad] = useState(1), [motivo, setMotivo] = useState('')
  const load = () => Promise.all([inventoryApi.inventario(), inventoryApi.movimientos(), inventoryApi.productos(), inventoryApi.almacenes()]).then(([s, m, p, w]) => { setStocks(s); setMoves(m.items); setProducts(p); setWarehouses(w) }).catch((e: Error) => setMessage(e.message))
  useEffect(() => { load() }, [])
  async function submit(e: FormEvent) { e.preventDefault(); try { if (operation === 'transferencia') await inventoryApi.transferencia({ productoId, almacenOrigenId: almacenId, almacenDestinoId: destinoId, cantidad, motivo }); else await inventoryApi.movimiento(operation, { almacenId, productoId, cantidad, motivo }); setMessage('Movimiento registrado.'); await load() } catch (error) { setMessage((error as Error).message) } }
  return <AppLayout title="Inventario">
    <form className="grid-form" onSubmit={submit}>
      <select value={operation} onChange={e => setOperation(e.target.value as typeof operation)}><option value="entrada">Entrada</option><option value="salida">Salida</option><option value="transferencia">Transferencia</option></select>
      <select value={productoId} onChange={e => setProductoId(e.target.value)} required><option value="">Producto</option>{products.map(x => <option key={x.id} value={x.id}>{x.codigo} - {x.nombre}</option>)}</select>
      <select value={almacenId} onChange={e => setAlmacenId(e.target.value)} required><option value="">{operation === 'transferencia' ? 'Almacén origen' : 'Almacén'}</option>{warehouses.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select>
      {operation === 'transferencia' && <select value={destinoId} onChange={e => setDestinoId(e.target.value)} required><option value="">Almacén destino</option>{warehouses.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select>}
      <input type="number" min="0.0001" step="0.0001" value={cantidad} onChange={e => setCantidad(Number(e.target.value))} required /><input placeholder="Motivo" value={motivo} onChange={e => setMotivo(e.target.value)} required /><button>Registrar</button>
    </form>{message && <p className="message">{message}</p>}
    <h2>Existencias</h2><table><thead><tr><th>Almacén</th><th>Producto</th><th>Cantidad</th><th>Estado</th></tr></thead><tbody>{stocks.map(x => <tr key={x.id}><td>{x.almacen}</td><td>{x.codigoProducto} - {x.producto}</td><td>{x.cantidad}</td><td>{x.stockBajo ? 'Stock bajo' : 'Normal'}</td></tr>)}</tbody></table>
    <h2>Movimientos recientes</h2><table><thead><tr><th>Fecha</th><th>Tipo</th><th>Cantidad</th><th>Anterior</th><th>Posterior</th><th>Motivo</th></tr></thead><tbody>{moves.map(x => <tr key={x.id}><td>{new Date(x.creadoEn).toLocaleString()}</td><td>{x.tipo}</td><td>{x.cantidad}</td><td>{x.stockAnterior}</td><td>{x.stockPosterior}</td><td>{x.motivo}</td></tr>)}</tbody></table>
  </AppLayout>
}
