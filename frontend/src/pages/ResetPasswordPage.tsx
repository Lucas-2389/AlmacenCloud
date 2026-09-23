import { useState } from 'react'
import type { FormEvent } from 'react'
import { authApi } from '../api/client'
import { Brand } from '../components/Brand'

export function ResetPasswordPage() {
  const token = new URLSearchParams(window.location.search).get('token') ?? ''
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [message, setMessage] = useState(token ? '' : 'El enlace de recuperación no contiene un token válido.')
  const [complete, setComplete] = useState(false)
  const [loading, setLoading] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault(); setMessage('')
    if (password !== confirmation) { setMessage('Las contraseñas no coinciden.'); return }
    setLoading(true)
    try { setMessage((await authApi.resetPassword(token, password)).message); setComplete(true) }
    catch (error) { setMessage(error instanceof Error ? error.message : 'No fue posible cambiar la contraseña.') }
    finally { setLoading(false) }
  }

  return <main className="auth-card">
    <Brand />
    <div className="auth-heading"><p>Recuperación segura</p><h1>Nueva contraseña</h1><span>El enlace vence en 30 minutos y solo funciona una vez.</span></div>
    {!complete && token && <form onSubmit={submit}>
      <label>Nueva contraseña<input type="password" autoComplete="new-password" minLength={8} value={password} onChange={e => setPassword(e.target.value)} required /></label>
      <label>Confirmar contraseña<input type="password" autoComplete="new-password" minLength={8} value={confirmation} onChange={e => setConfirmation(e.target.value)} required /></label>
      <button disabled={loading}>{loading ? 'Actualizando…' : 'Cambiar contraseña'}</button>
    </form>}
    {message && <p role="status" className="message">{message}</p>}
    <a href="/login">Ir a iniciar sesión</a>
  </main>
}
