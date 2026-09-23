import { useEffect, useState } from 'react'
import { purchasingApi } from '../api/client'
import type { CompraLista } from '../api/client'
import { AppLayout } from '../components/AppLayout'

export function ComprasPage() {
  const [items, setItems] = useState<CompraLista[]>([]), [message, setMessage] = useState('')
  useEffect(() => { purchasingApi.compras().then(x => setItems(x.items)).catch((e: Error) => setMessage(e.message)) }, [])
  return <AppLayout title="Compras"><p><a className="primary-link" href="/compras/nueva">Registrar nueva compra</a></p>{message && <p className="message">{message}</p>}<table><thead><tr><th>Número</th><th>Fecha</th><th>Proveedor</th><th>Documento</th><th>Almacén</th><th>Estado</th><th>Total</th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td><a href={`/compras/${x.id}`}>{x.numero}</a></td><td>{new Date(x.fecha).toLocaleString()}</td><td>{x.proveedor}</td><td>{x.numeroDocumentoProveedor ?? '-'}</td><td>{x.almacen}</td><td>{x.estado}</td><td>S/ {x.total.toFixed(2)}</td></tr>)}</tbody></table></AppLayout>
}
