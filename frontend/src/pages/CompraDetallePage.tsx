import { useEffect, useState } from 'react'
import { purchasingApi } from '../api/client'
import type { Compra } from '../api/client'
import { AppLayout } from '../components/AppLayout'

export function CompraDetallePage({ id }: { id: string }) {
  const [purchase, setPurchase] = useState<Compra>(), [message, setMessage] = useState('')
  const load = () => purchasingApi.compra(id).then(setPurchase).catch((e: Error) => setMessage(e.message)); useEffect(() => { void load() }, [id])
  async function annul() { if (!confirm('¿Anular esta compra y retirar del inventario las cantidades ingresadas?')) return; try { setPurchase(await purchasingApi.annulCompra(id)) } catch (error) { setMessage((error as Error).message) } }
  return <AppLayout title={purchase ? `Compra ${purchase.numero}` : 'Detalle de compra'}>{message && <p className="message">{message}</p>}{purchase && <><div className="sale-summary"><span>Fecha: {new Date(purchase.fecha).toLocaleString()}</span><span>Proveedor: {purchase.proveedor}</span><span>Documento: {purchase.numeroDocumentoProveedor ?? '-'}</span><span>Almacén: {purchase.almacen}</span><span>Usuario: {purchase.usuario}</span><strong>Estado: {purchase.estado}</strong></div><table><thead><tr><th>Código</th><th>Producto</th><th>Cantidad</th><th>Precio</th><th>Subtotal</th><th>IGV</th><th>Total</th></tr></thead><tbody>{purchase.detalles.map(x => <tr key={x.id}><td>{x.codigoProducto}</td><td>{x.nombreProducto}</td><td>{x.cantidad} {x.unidadMedida}</td><td>S/ {x.precioUnitario.toFixed(2)}</td><td>S/ {x.subtotal.toFixed(2)}</td><td>S/ {x.igv.toFixed(2)}</td><td>S/ {x.total.toFixed(2)}</td></tr>)}</tbody></table><div className="totals"><span>Subtotal: S/ {purchase.subtotal.toFixed(2)}</span><span>IGV: S/ {purchase.igv.toFixed(2)}</span><strong>Total: S/ {purchase.total.toFixed(2)}</strong></div>{purchase.observacion && <p>{purchase.observacion}</p>}{purchase.estado === 'REGISTRADA' && <button className="danger" onClick={annul}>Anular compra</button>}</>}</AppLayout>
}
