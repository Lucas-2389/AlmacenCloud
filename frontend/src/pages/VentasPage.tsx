import { useEffect, useState } from 'react'
import { salesApi } from '../api/client'
import type { VentaLista } from '../api/client'
import { AppLayout } from '../components/AppLayout'

export function VentasPage() {
  const [items, setItems] = useState<VentaLista[]>([]), [message, setMessage] = useState('')
  useEffect(() => { salesApi.ventas().then(x => setItems(x.items)).catch((e: Error) => setMessage(e.message)) }, [])
  return <AppLayout title="Ventas"><p><a className="primary-link" href="/ventas/nueva">Registrar nueva venta</a></p>{message && <p className="message">{message}</p>}<table><thead><tr><th>Número</th><th>Fecha</th><th>Cliente</th><th>Almacén</th><th>Estado</th><th>Total</th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td><a href={`/ventas/${x.id}`}>{x.numero}</a></td><td>{new Date(x.fecha).toLocaleString()}</td><td>{x.cliente}</td><td>{x.almacen}</td><td>{x.estado}</td><td>S/ {x.total.toFixed(2)}</td></tr>)}</tbody></table></AppLayout>
}
