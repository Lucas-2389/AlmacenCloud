import { useEffect, useState } from 'react'
import { salesApi } from '../api/client'
import type { Venta } from '../api/client'
import { AppLayout } from '../components/AppLayout'

export function VentaDetallePage({ id }: { id: string }) {
  const [sale, setSale] = useState<Venta>(), [message, setMessage] = useState('')
  const load = () => salesApi.venta(id).then(setSale).catch((e: Error) => setMessage(e.message)); useEffect(() => { void load() }, [id])
  async function annul() { if (!confirm('¿Anular esta venta y reponer el inventario?')) return; try { setSale(await salesApi.annulVenta(id)) } catch (error) { setMessage((error as Error).message) } }
  return <AppLayout title={sale ? `Venta ${sale.numero}` : 'Detalle de venta'}>{message && <p className="message">{message}</p>}{sale && <><div className="sale-summary"><span>Fecha: {new Date(sale.fecha).toLocaleString()}</span><span>Cliente: {sale.cliente}</span><span>Almacén: {sale.almacen}</span><span>Vendedor: {sale.usuario}</span><strong>Estado: {sale.estado}</strong></div><table><thead><tr><th>Código</th><th>Producto</th><th>Cantidad</th><th>Precio</th><th>Subtotal</th><th>IGV</th><th>Total</th></tr></thead><tbody>{sale.detalles.map(x => <tr key={x.id}><td>{x.codigoProducto}</td><td>{x.nombreProducto}</td><td>{x.cantidad} {x.unidadMedida}</td><td>S/ {x.precioUnitario.toFixed(2)}</td><td>S/ {x.subtotal.toFixed(2)}</td><td>S/ {x.igv.toFixed(2)}</td><td>S/ {x.total.toFixed(2)}</td></tr>)}</tbody></table><div className="totals"><span>Subtotal: S/ {sale.subtotal.toFixed(2)}</span><span>IGV: S/ {sale.igv.toFixed(2)}</span><strong>Total: S/ {sale.total.toFixed(2)}</strong></div>{sale.estado === 'REGISTRADA' && <button className="danger" onClick={annul}>Anular venta</button>}</>}</AppLayout>
}
