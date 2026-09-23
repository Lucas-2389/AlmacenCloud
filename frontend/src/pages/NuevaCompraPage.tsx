import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { inventoryApi, purchasingApi, salesApi } from '../api/client'
import type { Almacen, Producto, Proveedor } from '../api/client'
import { AppLayout } from '../components/AppLayout'

type Line = { product: Producto; cantidad: string; precioUnitario: string }
const isDiscreteUnit = (unit: string) => unit === 'UNIDAD' || unit === 'CAJA'
export function NuevaCompraPage() {
  const [suppliers, setSuppliers] = useState<Proveedor[]>([]), [warehouses, setWarehouses] = useState<Almacen[]>([]), [products, setProducts] = useState<Producto[]>([])
  const [proveedorId, setProveedorId] = useState(''), [almacenId, setAlmacenId] = useState(''), [productId, setProductId] = useState(''), [productSearch, setProductSearch] = useState(''), [lines, setLines] = useState<Line[]>([])
  const [documento, setDocumento] = useState(''), [observacion, setObservacion] = useState(''), [message, setMessage] = useState('')
  useEffect(() => { Promise.all([salesApi.proveedores(), inventoryApi.almacenes(), inventoryApi.productos()]).then(([s,w,p]) => { setSuppliers(s); setWarehouses(w); setProducts(p) }).catch((e: Error) => setMessage(e.message)) }, [])
  const preview = useMemo(() => lines.reduce((sum, x) => { const total = Math.round(Number(x.cantidad) * Number(x.precioUnitario) * 100) / 100; const subtotal = x.product.afectoIgv ? Math.round(total / 1.18 * 100) / 100 : total; return { subtotal: sum.subtotal + subtotal, igv: sum.igv + total - subtotal, total: sum.total + total } }, { subtotal: 0, igv: 0, total: 0 }), [lines])
  const visibleProducts = products.filter(x => `${x.codigo} ${x.nombre}`.toLowerCase().includes(productSearch.toLowerCase()))
  function addProduct() { const product = products.find(x => x.id === productId); if (!product || lines.some(x => x.product.id === product.id)) return; setLines(x => [...x, { product, cantidad: '1', precioUnitario: String(product.precioCompra) }]); setProductId('') }
  async function submit(e: FormEvent) { e.preventDefault(); try { const purchase = await purchasingApi.createCompra({ proveedorId, almacenId, numeroDocumentoProveedor: documento || undefined, items: lines.map(x => ({ productoId: x.product.id, cantidad: Number(x.cantidad), precioUnitario: Number(x.precioUnitario) })), observacion }); window.location.href = `/compras/${purchase.id}` } catch (error) { setMessage((error as Error).message) } }
  return <AppLayout title="Nueva compra"><form onSubmit={submit}>
    <div className="grid-form">
      <label className="form-field">Proveedor<select value={proveedorId} onChange={e => setProveedorId(e.target.value)} required><option value="">Selecciona un proveedor</option>{suppliers.map(x => <option key={x.id} value={x.id}>{x.ruc} - {x.razonSocial}</option>)}</select></label>
      <label className="form-field">Almacén de ingreso<select value={almacenId} onChange={e => setAlmacenId(e.target.value)} required><option value="">Selecciona un almacén</option>{warehouses.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select></label>
      <label className="form-field">Documento del proveedor<input placeholder="Ej. F001-123" value={documento} onChange={e => setDocumento(e.target.value)} /></label>
      <label className="form-field">Observación<input placeholder="Información opcional" value={observacion} onChange={e => setObservacion(e.target.value)} /></label>
    </div>
    <div className="toolbar"><label className="form-field">Buscar producto<input placeholder="Código o nombre" value={productSearch} onChange={e => setProductSearch(e.target.value)} /></label><label className="form-field">Producto<select value={productId} onChange={e => setProductId(e.target.value)}><option value="">Selecciona un producto</option>{visibleProducts.map(x => <option key={x.id} value={x.id}>{x.codigo} - {x.nombre}</option>)}</select></label><button type="button" onClick={addProduct}>Agregar producto</button></div>
    <table><thead><tr><th>Producto</th><th>Cantidad</th><th>Costo unitario</th><th>Total</th><th></th></tr></thead><tbody>{lines.map((line, index) => <tr key={line.product.id}><td>{line.product.nombre}<small className="cell-helper">{line.product.unidadMedida}</small></td><td><input aria-label={`Cantidad de ${line.product.nombre}`} type="number" min={isDiscreteUnit(line.product.unidadMedida) ? '1' : '0.0001'} step={isDiscreteUnit(line.product.unidadMedida) ? '1' : '0.0001'} value={line.cantidad} onChange={e => setLines(current => current.map((x,i) => i === index ? {...x, cantidad: e.target.value} : x))} required /></td><td><input aria-label={`Costo de ${line.product.nombre}`} type="number" min="0" step="0.01" value={line.precioUnitario} onChange={e => setLines(current => current.map((x,i) => i === index ? {...x, precioUnitario: e.target.value} : x))} required /></td><td>S/ {(Number(line.cantidad) * Number(line.precioUnitario)).toFixed(2)}</td><td><button type="button" onClick={() => setLines(x => x.filter((_,i) => i !== index))}>Quitar</button></td></tr>)}</tbody></table>
    <div className="totals"><span>Subtotal: S/ {preview.subtotal.toFixed(2)}</span><span>IGV: S/ {preview.igv.toFixed(2)}</span><strong>Total: S/ {preview.total.toFixed(2)}</strong></div>{message && <p className="message">{message}</p>}<button className="submit-sale" disabled={!proveedorId || !almacenId || lines.length === 0}>Registrar compra</button>
  </form></AppLayout>
}
