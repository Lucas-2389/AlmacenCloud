import { useState } from 'react'
import type { FormEvent } from 'react'
import { authApi } from '../api/client'

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setLoading(true)
    setMessage('')
    try {
      const result = await authApi.login(email, password)
      sessionStorage.setItem('almacencloud_access_token', result.accessToken)
      window.location.href = '/categorias'
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'No fue posible iniciar sesión.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="auth-card">
      <h1>Iniciar sesión</h1>
      <p>Accede a tu empresa en AlmacenCloud.</p>
      <form onSubmit={submit}>
        <label>Email<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></label>
        <label>Contraseña<input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required /></label>
        <button disabled={loading}>{loading ? 'Ingresando…' : 'Ingresar'}</button>
      </form>
      {message && <p role="status" className="message">{message}</p>}
      <a href="/register">Registrar empresa</a>
    </main>
  )
}
