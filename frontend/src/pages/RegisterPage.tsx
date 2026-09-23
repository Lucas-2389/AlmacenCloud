import { useState } from 'react'
import type { FormEvent } from 'react'
import { authApi } from '../api/client'
import { Brand } from '../components/Brand'

const initial = { ruc: '', razonSocial: '', nombreComercial: '', nombre: '', apellidos: '', email: '', password: '' }

export function RegisterPage() {
  const [form, setForm] = useState(initial)
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const update = (field: keyof typeof form, value: string) => setForm((current) => ({ ...current, [field]: value }))

  async function submit(event: FormEvent) {
    event.preventDefault()
    setLoading(true)
    setMessage('')
    try {
      await authApi.registerCompany({
        ruc: form.ruc,
        razonSocial: form.razonSocial,
        nombreComercial: form.nombreComercial || undefined,
        admin: { nombre: form.nombre, apellidos: form.apellidos, email: form.email, password: form.password },
      })
      setMessage('Empresa registrada. Ya puedes iniciar sesión.')
      setForm(initial)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'No fue posible registrar la empresa.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="auth-card wide">
      <Brand />
      <div className="auth-heading"><p>Gestión inteligente de inventarios</p><h1>Registrar empresa</h1><span>Crea el espacio de trabajo de tu organización en AlmacenCloud.</span></div>
      <form onSubmit={submit}>
        <label>RUC<input value={form.ruc} onChange={(e) => update('ruc', e.target.value)} pattern="[0-9]{11}" maxLength={11} required /></label>
        <label>Razón social<input value={form.razonSocial} onChange={(e) => update('razonSocial', e.target.value)} required /></label>
        <label>Nombre comercial<input value={form.nombreComercial} onChange={(e) => update('nombreComercial', e.target.value)} /></label>
        <label>Nombre del administrador<input value={form.nombre} onChange={(e) => update('nombre', e.target.value)} required /></label>
        <label>Apellidos<input value={form.apellidos} onChange={(e) => update('apellidos', e.target.value)} required /></label>
        <label>Email<input type="email" value={form.email} onChange={(e) => update('email', e.target.value)} required /></label>
        <label>Contraseña<input type="password" minLength={8} value={form.password} onChange={(e) => update('password', e.target.value)} required /></label>
        <button disabled={loading}>{loading ? 'Registrando…' : 'Crear empresa'}</button>
      </form>
      {message && <p role="status" className="message">{message}</p>}
      <a href="/login">Volver al inicio de sesión</a>
    </main>
  )
}
