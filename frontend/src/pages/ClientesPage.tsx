import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { salesApi } from '../api/client'
import type { Cliente } from '../api/client'
import { AppLayout } from '../components/AppLayout'

const empty = { tipoDocumento: 'DNI', numeroDocumento: '', nombreRazonSocial: '', direccion: '', telefono: '', email: '' }
export function ClientesPage() {
  const [items, setItems] = useState<Cliente[]>([]), [form, setForm] = useState(empty), [id, setId] = useState<string>(), [search, setSearch] = useState(''), [message, setMessage] = useState('')
  const load = () => salesApi.clientes(search).then(setItems).catch((e: Error) => setMessage(e.message)); useEffect(() => { void load() }, [])
  const update = (key: keyof typeof form, value: string) => setForm(x => ({ ...x, [key]: value }))
  async function submit(e: FormEvent) { e.preventDefault(); try { await salesApi.saveCliente(id, form); setId(undefined); setForm(empty); await load() } catch (error) { setMessage((error as Error).message) } }
  const edit = (x: Cliente) => { setId(x.id); setForm({ tipoDocumento: x.tipoDocumento, numeroDocumento: x.numeroDocumento, nombreRazonSocial: x.nombreRazonSocial, direccion: x.direccion ?? '', telefono: x.telefono ?? '', email: x.email ?? '' }) }
  const remove = async (x: Cliente) => { if (confirm(`¿Desactivar ${x.nombreRazonSocial}?`)) { await salesApi.deleteCliente(x.id); await load() } }
  return <AppLayout title="Clientes"><div className="toolbar"><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Documento o nombre"/><button onClick={load}>Buscar</button></div><form className="grid-form" onSubmit={submit}><select value={form.tipoDocumento} onChange={e => update('tipoDocumento', e.target.value)}>{['DNI','RUC','CE','OTRO'].map(x => <option key={x}>{x}</option>)}</select><input placeholder="Documento" value={form.numeroDocumento} onChange={e => update('numeroDocumento', e.target.value)} required/><input placeholder="Nombre o razón social" value={form.nombreRazonSocial} onChange={e => update('nombreRazonSocial', e.target.value)} required/><input placeholder="Dirección" value={form.direccion} onChange={e => update('direccion', e.target.value)}/><input placeholder="Teléfono" value={form.telefono} onChange={e => update('telefono', e.target.value)}/><input type="email" placeholder="Email" value={form.email} onChange={e => update('email', e.target.value)}/><button>{id ? 'Actualizar' : 'Crear'}</button></form>{message && <p className="message">{message}</p>}<table><thead><tr><th>Documento</th><th>Cliente</th><th>Contacto</th><th></th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td>{x.tipoDocumento} {x.numeroDocumento}</td><td>{x.nombreRazonSocial}</td><td>{x.telefono} {x.email}</td><td><button onClick={() => edit(x)}>Editar</button> <button onClick={() => remove(x)}>Desactivar</button></td></tr>)}</tbody></table></AppLayout>
}
