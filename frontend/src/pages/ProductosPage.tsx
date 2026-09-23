import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { apiAssetUrl, inventoryApi } from '../api/client'
import type { Categoria, Producto } from '../api/client'
import { AppLayout } from '../components/AppLayout'

const empty = { categoriaId: '', codigo: '', nombre: '', descripcion: '', unidadMedida: 'UNIDAD', precioCompra: '', precioVenta: '', stockMinimo: '', afectoIgv: true }
const isDiscreteUnit = (unit: string) => unit === 'UNIDAD' || unit === 'CAJA'

export function ProductosPage() {
  const [items, setItems] = useState<Producto[]>([]), [categories, setCategories] = useState<Categoria[]>([]), [id, setId] = useState<string>(), [form, setForm] = useState(empty), [search, setSearch] = useState(''), [message, setMessage] = useState('')
  const load = () => Promise.all([inventoryApi.productos(search), inventoryApi.categorias()]).then(([p, c]) => { setItems(p); setCategories(c) }).catch((e: Error) => setMessage(e.message))
  useEffect(() => { load() }, [])
  const update = (key: keyof typeof form, value: string | number | boolean) => setForm(x => ({ ...x, [key]: value }))
  async function submit(e: FormEvent) { e.preventDefault(); try { await inventoryApi.saveProducto(id, { ...form, precioCompra: Number(form.precioCompra), precioVenta: Number(form.precioVenta), stockMinimo: Number(form.stockMinimo) }); setId(undefined); setForm(empty); await load() } catch (error) { setMessage((error as Error).message) } }
  const edit = (x: Producto) => { setId(x.id); setForm({ categoriaId: x.categoriaId, codigo: x.codigo, nombre: x.nombre, descripcion: x.descripcion ?? '', unidadMedida: x.unidadMedida, precioCompra: String(x.precioCompra), precioVenta: String(x.precioVenta), stockMinimo: String(x.stockMinimo), afectoIgv: x.afectoIgv }) }
  const remove = async (x: Producto) => { if (confirm(`¿Desactivar ${x.nombre}?`)) { await inventoryApi.deleteProducto(x.id); await load() } }
  const uploadImage = async (x: Producto, file?: File) => { if (!file) return; try { await inventoryApi.uploadProductoImage(x.id, file); setMessage('Foto actualizada correctamente.'); await load() } catch (error) { setMessage((error as Error).message) } }
  const removeImage = async (x: Producto) => { if (!confirm(`¿Eliminar la foto de ${x.nombre}?`)) return; try { await inventoryApi.deleteProductoImage(x.id); setMessage('Foto eliminada.'); await load() } catch (error) { setMessage((error as Error).message) } }
  return <AppLayout title="Productos">
    <div className="toolbar"><label className="form-field"><span>Buscar por código o nombre</span><input placeholder="Ejemplo: LAP-001" value={search} onChange={e => setSearch(e.target.value)} /></label><button onClick={load}>Buscar</button></div>
    <form className="grid-form" onSubmit={submit}>
      <label className="form-field"><span>Categoría</span><select value={form.categoriaId} onChange={e => update('categoriaId', e.target.value)} required><option value="">Selecciona una categoría</option>{categories.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select></label>
      <label className="form-field"><span>Código del producto</span><input placeholder="Ejemplo: LAP-001" value={form.codigo} onChange={e => update('codigo', e.target.value)} required /></label>
      <label className="form-field"><span>Nombre del producto</span><input placeholder="Ejemplo: Laptop empresarial" value={form.nombre} onChange={e => update('nombre', e.target.value)} required /></label>
      <label className="form-field"><span>Descripción</span><input placeholder="Características principales" value={form.descripcion} onChange={e => update('descripcion', e.target.value)} /></label>
      <label className="form-field"><span>Unidad de medida</span><select value={form.unidadMedida} onChange={e => update('unidadMedida', e.target.value)}>{['UNIDAD','KILOGRAMO','LITRO','METRO','CAJA'].map(x => <option key={x}>{x}</option>)}</select></label>
      <label className="form-field"><span>Precio de compra (S/)</span><input type="number" inputMode="decimal" min="0" step="0.01" placeholder="0.00" value={form.precioCompra} onChange={e => update('precioCompra', e.target.value)} required /></label>
      <label className="form-field"><span>Precio de venta (S/)</span><input type="number" inputMode="decimal" min="0" step="0.01" placeholder="0.00" value={form.precioVenta} onChange={e => update('precioVenta', e.target.value)} required /></label>
      <label className="form-field"><span>Stock mínimo ({isDiscreteUnit(form.unidadMedida) ? 'entero' : 'admite decimales'})</span><input type="number" inputMode={isDiscreteUnit(form.unidadMedida) ? 'numeric' : 'decimal'} min="0" step={isDiscreteUnit(form.unidadMedida) ? '1' : '0.0001'} placeholder="0" value={form.stockMinimo} onChange={e => update('stockMinimo', e.target.value)} required /></label>
      <label className="checkbox-field"><input type="checkbox" checked={form.afectoIgv} onChange={e => update('afectoIgv', e.target.checked)} /> Producto afecto a IGV</label><button>{id ? 'Actualizar producto' : 'Crear producto'}</button>
    </form>{message && <p className="message">{message}</p>}
    <table><thead><tr><th>Foto</th><th>Código</th><th>Producto</th><th>Categoría</th><th>Compra</th><th>Venta</th><th>Acciones</th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td>{x.imagenUrl ? <img className="product-thumb" src={apiAssetUrl(x.imagenUrl)} alt={x.nombre}/> : <span className="product-placeholder">Sin foto</span>}</td><td>{x.codigo}</td><td>{x.nombre}</td><td>{x.categoria}</td><td>S/ {x.precioCompra.toFixed(2)}</td><td>S/ {x.precioVenta.toFixed(2)}</td><td><div className="table-actions"><button onClick={() => edit(x)}>Editar</button><label className="upload-button">{x.imagenUrl ? 'Cambiar foto' : 'Subir foto'}<input type="file" accept="image/jpeg,image/png,image/webp" onChange={e => { void uploadImage(x, e.target.files?.[0]); e.target.value = '' }}/></label>{x.imagenUrl && <button className="danger-small" onClick={() => removeImage(x)}>Eliminar foto</button>}<button onClick={() => remove(x)}>Desactivar</button></div></td></tr>)}</tbody></table>
  </AppLayout>
}
