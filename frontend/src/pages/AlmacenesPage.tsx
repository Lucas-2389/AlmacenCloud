import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { inventoryApi } from '../api/client'
import type { Almacen } from '../api/client'
import { AppLayout } from '../components/AppLayout'

export function AlmacenesPage() {
  const [items, setItems] = useState<Almacen[]>([]), [id, setId] = useState<string>(), [codigo, setCodigo] = useState(''), [nombre, setNombre] = useState(''), [direccion, setDireccion] = useState(''), [message, setMessage] = useState('')
  const load = () => inventoryApi.almacenes().then(setItems).catch((e: Error) => setMessage(e.message))
  useEffect(() => { void load() }, [])
  async function submit(e: FormEvent) { e.preventDefault(); try { await inventoryApi.saveAlmacen(id, { codigo, nombre, direccion }); setId(undefined); setCodigo(''); setNombre(''); setDireccion(''); await load() } catch (error) { setMessage((error as Error).message) } }
  const edit = (x: Almacen) => { setId(x.id); setCodigo(x.codigo); setNombre(x.nombre); setDireccion(x.direccion ?? '') }
  const remove = async (x: Almacen) => { if (confirm(`¿Desactivar ${x.nombre}?`)) { await inventoryApi.deleteAlmacen(x.id); await load() } }
  return <AppLayout title="Almacenes"><form className="grid-form" onSubmit={submit}><label className="form-field"><span>Código del almacén</span><input placeholder="Ejemplo: ALM-001" value={codigo} onChange={e => setCodigo(e.target.value)} required /></label><label className="form-field"><span>Nombre del almacén</span><input placeholder="Ejemplo: Almacén principal" value={nombre} onChange={e => setNombre(e.target.value)} required /></label><label className="form-field"><span>Dirección</span><input placeholder="Ejemplo: Av. Industrial 123, Lima" value={direccion} onChange={e => setDireccion(e.target.value)} /></label><button>{id ? 'Actualizar almacén' : 'Crear almacén'}</button></form>{message && <p className="message">{message}</p>}<table><thead><tr><th>Código</th><th>Nombre</th><th>Dirección</th><th></th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td>{x.codigo}</td><td>{x.nombre}</td><td>{x.direccion || 'Sin dirección'}</td><td><button onClick={() => edit(x)}>Editar</button> <button onClick={() => remove(x)}>Desactivar</button></td></tr>)}</tbody></table></AppLayout>
}
