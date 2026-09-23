import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { inventoryApi } from '../api/client'
import type { Categoria } from '../api/client'
import { AppLayout } from '../components/AppLayout'

export function CategoriasPage() {
  const [items, setItems] = useState<Categoria[]>([]), [id, setId] = useState<string>(), [nombre, setNombre] = useState(''), [descripcion, setDescripcion] = useState(''), [message, setMessage] = useState('')
  const load = () => inventoryApi.categorias().then(setItems).catch((e: Error) => setMessage(e.message))
  useEffect(() => { void load() }, [])
  async function submit(e: FormEvent) { e.preventDefault(); try { await inventoryApi.saveCategoria(id, { nombre, descripcion }); setId(undefined); setNombre(''); setDescripcion(''); await load() } catch (error) { setMessage((error as Error).message) } }
  const edit = (x: Categoria) => { setId(x.id); setNombre(x.nombre); setDescripcion(x.descripcion ?? '') }
  const remove = async (x: Categoria) => { if (confirm(`¿Desactivar ${x.nombre}?`)) { await inventoryApi.deleteCategoria(x.id); await load() } }
  return <AppLayout title="Categorías"><form className="inline-form" onSubmit={submit}><label className="form-field">Nombre<input placeholder="Ej. Bebidas" value={nombre} onChange={e => setNombre(e.target.value)} required /></label><label className="form-field">Descripción<input placeholder="Descripción opcional" value={descripcion} onChange={e => setDescripcion(e.target.value)} /></label><button>{id ? 'Actualizar' : 'Crear categoría'}</button></form>{message && <p className="message">{message}</p>}<table><thead><tr><th>Nombre</th><th>Descripción</th><th></th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td>{x.nombre}</td><td>{x.descripcion}</td><td><button onClick={() => edit(x)}>Editar</button> <button onClick={() => remove(x)}>Desactivar</button></td></tr>)}</tbody></table></AppLayout>
}
