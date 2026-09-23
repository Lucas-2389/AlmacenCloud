import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { authApi } from '../api/client'
import { Brand } from '../components/Brand'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const [enabled, setEnabled] = useState<boolean | null>(null)

  useEffect(() => { authApi.publicConfiguration().then(x => setEnabled(x.passwordResetEnabled)).catch(() => setEnabled(false)) }, [])

  async function submit(event: FormEvent) {
    event.preventDefault(); setLoading(true); setMessage('')
    try { setMessage((await authApi.forgotPassword(email)).message) }
    catch (error) { setMessage(error instanceof Error ? error.message : 'No fue posible procesar la solicitud.') }
    finally { setLoading(false) }
  }

  return <main className="auth-card">
    <Brand />
    <div className="auth-heading"><p>Recuperación segura</p><h1>¿Olvidaste tu contraseña?</h1><span>Ingresa el correo asociado a tu empresa.</span></div>
    {enabled === null && <p className="message">Verificando disponibilidad…</p>}
    {enabled === false && <p className="message">La recuperación de contraseña no está disponible temporalmente.</p>}
    {enabled && <form onSubmit={submit}>
      <label>Correo electrónico<input type="email" autoComplete="email" value={email} onChange={e => setEmail(e.target.value)} required /></label>
      <button disabled={loading}>{loading ? 'Enviando…' : 'Enviar instrucciones'}</button>
    </form>}
    {message && <p role="status" className="message">{message}</p>}
    <a href="/login">Volver a iniciar sesión</a>
  </main>
}
