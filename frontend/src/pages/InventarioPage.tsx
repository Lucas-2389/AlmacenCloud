import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { inventoryApi } from '../api/client'
import type { Almacen, Inventario, Movimiento, Producto } from '../api/client'
import { AppLayout } from '../components/AppLayout'

export function InventarioPage() {
  const [stocks, setStocks] = useState<Inventario[]>([]), [moves, setMoves] = useState<Movimiento[]>([]), [products, setProducts] = useState<Producto[]>([]), [warehouses, setWarehouses] = useState<Almacen[]>([]), [message, setMessage] = useState('')
  const [operation, setOperation] = useState<'entrada' | 'salida' | 'transferencia'>('entrada')
  const [productoId, setProductoId] = useState(''), [almacenId, setAlmacenId] = useState(''), [destinoId, setDestinoId] = useState(''), [cantidad, setCantidad] = useState(''), [motivo, setMotivo] = useState('')
  const selectedProduct = products.find(x => x.id === productoId)
  const discreteQuantity = selectedProduct?.unidadMedida === 'UNIDAD' || selectedProduct?.unidadMedida === 'CAJA'
  const load = () => Promise.all([inventoryApi.inventario(), inventoryApi.movimientos(), inventoryApi.productos(), inventoryApi.almacenes()]).then(([s, m, p, w]) => { setStocks(s); setMoves(m.items); setProducts(p); setWarehouses(w) }).catch((e: Error) => setMessage(e.message))
  useEffect(() => { load() }, [])
  async function submit(e: FormEvent) { e.preventDefault(); try { const parsedQuantity = Number(cantidad); if (operation === 'transferencia') await inventoryApi.transferencia({ productoId, almacenOrigenId: almacenId, almacenDestinoId: destinoId, cantidad: parsedQuantity, motivo }); else await inventoryApi.movimiento(operation, { almacenId, productoId, cantidad: parsedQuantity, motivo }); setMessage('Movimiento registrado.'); setCantidad(''); setMotivo(''); await load() } catch (error) { setMessage((error as Error).message) } }
  return <AppLayout title="Inventario">
    <form className="grid-form" onSubmit={submit}>
      <label className="form-field">Tipo de movimiento<select value={operation} onChange={e => setOperation(e.target.value as typeof operation)}><option value="entrada">Entrada</option><option value="salida">Salida</option><option value="transferencia">Transferencia</option></select></label>
      <label className="form-field">Producto<select value={productoId} onChange={e => setProductoId(e.target.value)} required><option value="">Selecciona un producto</option>{products.map(x => <option key={x.id} value={x.id}>{x.codigo} - {x.nombre}</option>)}</select></label>
      <label className="form-field">{operation === 'transferencia' ? 'Almacén de origen' : 'Almacén'}<select value={almacenId} onChange={e => setAlmacenId(e.target.value)} required><option value="">Selecciona un almacén</option>{warehouses.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select></label>
      {operation === 'transferencia' && <label className="form-field">Almacén de destino<select value={destinoId} onChange={e => setDestinoId(e.target.value)} required><option value="">Selecciona el almacén destino</option>{warehouses.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select></label>}
      <label className="form-field">Cantidad {selectedProduct ? `(${discreteQuantity ? 'entera' : 'admite decimales'})` : ''}<input type="number" inputMode={discreteQuantity ? 'numeric' : 'decimal'} min={discreteQuantity ? '1' : '0.0001'} step={discreteQuantity ? '1' : '0.0001'} placeholder={discreteQuantity ? 'Ej. 10' : 'Ej. 10.5000'} value={cantidad} onChange={e => setCantidad(e.target.value)} required /></label>
      <label className="form-field">Motivo<input placeholder="Ej. Stock inicial" value={motivo} onChange={e => setMotivo(e.target.value)} required /></label>
      <button>Registrar movimiento</button>
    </form>{message && <p className="message">{message}</p>}
    <h2>Existencias</h2><table><thead><tr><th>Almacén</th><th>Producto</th><th>Cantidad</th><th>Estado</th></tr></thead><tbody>{stocks.map(x => <tr key={x.id}><td>{x.almacen}</td><td>{x.codigoProducto} - {x.producto}</td><td>{x.cantidad}</td><td>{x.stockBajo ? 'Stock bajo' : 'Normal'}</td></tr>)}</tbody></table>
    <h2>Movimientos recientes</h2><table><thead><tr><th>Fecha</th><th>Tipo</th><th>Cantidad</th><th>Anterior</th><th>Posterior</th><th>Motivo</th></tr></thead><tbody>{moves.map(x => <tr key={x.id}><td>{new Date(x.creadoEn).toLocaleString()}</td><td>{x.tipo}</td><td>{x.cantidad}</td><td>{x.stockAnterior}</td><td>{x.stockPosterior}</td><td>{x.motivo}</td></tr>)}</tbody></table>
  </AppLayout>
}
