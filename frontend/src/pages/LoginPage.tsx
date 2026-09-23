import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { authApi } from '../api/client'
import { Brand } from '../components/Brand'
import { PasswordField } from '../components/PasswordField'

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const [passwordResetEnabled, setPasswordResetEnabled] = useState(false)

  useEffect(() => { authApi.publicConfiguration().then(x => setPasswordResetEnabled(x.passwordResetEnabled)).catch(() => setPasswordResetEnabled(false)) }, [])

  async function submit(event: FormEvent) {
    event.preventDefault()
    setLoading(true)
    setMessage('')
    try {
      const result = await authApi.login(email, password)
      sessionStorage.setItem('almacencloud_access_token', result.accessToken)
      window.location.href = '/dashboard'
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'No fue posible iniciar sesión.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="auth-card">
      <Brand />
      <div className="auth-heading"><p>Gestión inteligente de inventarios</p><h1>Iniciar sesión</h1><span>Accede al espacio de trabajo de tu empresa.</span></div>
      <form onSubmit={submit}>
        <label>Email<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></label>
        <PasswordField label="Contraseña" autoComplete="current-password" value={password} onChange={setPassword} />
        <button disabled={loading}>{loading ? 'Ingresando…' : 'Ingresar'}</button>
      </form>
      {message && <p role="status" className="message">{message}</p>}
      <div className="auth-links">{passwordResetEnabled && <a href="/forgot-password">Olvidé mi contraseña</a>}<a href="/register">Registrar empresa</a></div>
    </main>
  )
}
