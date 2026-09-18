import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { inventoryApi } from '../api/client'
import type { Categoria, Producto } from '../api/client'
import { AppLayout } from '../components/AppLayout'

const empty = { categoriaId: '', codigo: '', nombre: '', descripcion: '', unidadMedida: 'UNIDAD', precioCompra: 0, precioVenta: 0, stockMinimo: 0, afectoIgv: true }

export function ProductosPage() {
  const [items, setItems] = useState<Producto[]>([]), [categories, setCategories] = useState<Categoria[]>([]), [id, setId] = useState<string>(), [form, setForm] = useState(empty), [search, setSearch] = useState(''), [message, setMessage] = useState('')
  const load = () => Promise.all([inventoryApi.productos(search), inventoryApi.categorias()]).then(([p, c]) => { setItems(p); setCategories(c) }).catch((e: Error) => setMessage(e.message))
  useEffect(() => { load() }, [])
  const update = (key: keyof typeof form, value: string | number | boolean) => setForm(x => ({ ...x, [key]: value }))
  async function submit(e: FormEvent) { e.preventDefault(); try { await inventoryApi.saveProducto(id, form); setId(undefined); setForm(empty); await load() } catch (error) { setMessage((error as Error).message) } }
  const edit = (x: Producto) => { setId(x.id); setForm({ categoriaId: x.categoriaId, codigo: x.codigo, nombre: x.nombre, descripcion: x.descripcion ?? '', unidadMedida: x.unidadMedida, precioCompra: x.precioCompra, precioVenta: x.precioVenta, stockMinimo: x.stockMinimo, afectoIgv: x.afectoIgv }) }
  const remove = async (x: Producto) => { if (confirm(`¿Desactivar ${x.nombre}?`)) { await inventoryApi.deleteProducto(x.id); await load() } }
  return <AppLayout title="Productos">
    <div className="toolbar"><input placeholder="Código o nombre" value={search} onChange={e => setSearch(e.target.value)} /><button onClick={load}>Buscar</button></div>
    <form className="grid-form" onSubmit={submit}>
      <select value={form.categoriaId} onChange={e => update('categoriaId', e.target.value)} required><option value="">Categoría</option>{categories.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select>
      <input placeholder="Código" value={form.codigo} onChange={e => update('codigo', e.target.value)} required /><input placeholder="Nombre" value={form.nombre} onChange={e => update('nombre', e.target.value)} required />
      <input placeholder="Descripción" value={form.descripcion} onChange={e => update('descripcion', e.target.value)} />
      <select value={form.unidadMedida} onChange={e => update('unidadMedida', e.target.value)}>{['UNIDAD','KILOGRAMO','LITRO','METRO','CAJA'].map(x => <option key={x}>{x}</option>)}</select>
      <input type="number" min="0" step="0.01" aria-label="Precio compra" value={form.precioCompra} onChange={e => update('precioCompra', Number(e.target.value))} />
      <input type="number" min="0" step="0.01" aria-label="Precio venta" value={form.precioVenta} onChange={e => update('precioVenta', Number(e.target.value))} />
      <input type="number" min="0" step="0.0001" aria-label="Stock mínimo" value={form.stockMinimo} onChange={e => update('stockMinimo', Number(e.target.value))} />
      <label><input type="checkbox" checked={form.afectoIgv} onChange={e => update('afectoIgv', e.target.checked)} /> Afecto IGV</label><button>{id ? 'Actualizar' : 'Crear'}</button>
    </form>{message && <p className="message">{message}</p>}
    <table><thead><tr><th>Código</th><th>Producto</th><th>Categoría</th><th>Venta</th><th></th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td>{x.codigo}</td><td>{x.nombre}</td><td>{x.categoria}</td><td>S/ {x.precioVenta.toFixed(2)}</td><td><button onClick={() => edit(x)}>Editar</button> <button onClick={() => remove(x)}>Desactivar</button></td></tr>)}</tbody></table>
  </AppLayout>
}
