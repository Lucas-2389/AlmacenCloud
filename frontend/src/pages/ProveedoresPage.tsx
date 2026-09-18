import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { salesApi } from '../api/client'
import type { Proveedor } from '../api/client'
import { AppLayout } from '../components/AppLayout'

const empty = { ruc: '', razonSocial: '', nombreComercial: '', direccion: '', telefono: '', email: '' }
export function ProveedoresPage() {
  const [items, setItems] = useState<Proveedor[]>([]), [form, setForm] = useState(empty), [id, setId] = useState<string>(), [search, setSearch] = useState(''), [message, setMessage] = useState('')
  const load = () => salesApi.proveedores(search).then(setItems).catch((e: Error) => setMessage(e.message)); useEffect(() => { void load() }, [])
  const update = (key: keyof typeof form, value: string) => setForm(x => ({ ...x, [key]: value }))
  async function submit(e: FormEvent) { e.preventDefault(); try { await salesApi.saveProveedor(id, form); setId(undefined); setForm(empty); await load() } catch (error) { setMessage((error as Error).message) } }
  const edit = (x: Proveedor) => { setId(x.id); setForm({ ruc: x.ruc, razonSocial: x.razonSocial, nombreComercial: x.nombreComercial ?? '', direccion: x.direccion ?? '', telefono: x.telefono ?? '', email: x.email ?? '' }) }
  const remove = async (x: Proveedor) => { if (confirm(`¿Desactivar ${x.razonSocial}?`)) { await salesApi.deleteProveedor(x.id); await load() } }
  return <AppLayout title="Proveedores"><div className="toolbar"><input value={search} onChange={e => setSearch(e.target.value)} placeholder="RUC o razón social"/><button onClick={load}>Buscar</button></div><form className="grid-form" onSubmit={submit}><input placeholder="RUC" value={form.ruc} onChange={e => update('ruc', e.target.value)} required/><input placeholder="Razón social" value={form.razonSocial} onChange={e => update('razonSocial', e.target.value)} required/><input placeholder="Nombre comercial" value={form.nombreComercial} onChange={e => update('nombreComercial', e.target.value)}/><input placeholder="Dirección" value={form.direccion} onChange={e => update('direccion', e.target.value)}/><input placeholder="Teléfono" value={form.telefono} onChange={e => update('telefono', e.target.value)}/><input type="email" placeholder="Email" value={form.email} onChange={e => update('email', e.target.value)}/><button>{id ? 'Actualizar' : 'Crear'}</button></form>{message && <p className="message">{message}</p>}<table><thead><tr><th>RUC</th><th>Proveedor</th><th>Contacto</th><th></th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td>{x.ruc}</td><td>{x.razonSocial}</td><td>{x.telefono} {x.email}</td><td><button onClick={() => edit(x)}>Editar</button> <button onClick={() => remove(x)}>Desactivar</button></td></tr>)}</tbody></table></AppLayout>
}
