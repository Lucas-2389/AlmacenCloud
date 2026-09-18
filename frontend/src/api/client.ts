const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

type ApiError = { title?: string; detail?: string }

async function request<T>(path: string, options: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options.headers },
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ApiError
    throw new Error(problem.detail ?? problem.title ?? 'No fue posible completar la solicitud.')
  }

  return response.json() as Promise<T>
}

export type LoginResponse = {
  accessToken: string
  expiresIn: number
  user: { id: string; nombre: string; email: string; empresaId: string; roles: string[] }
}

export type RegisterCompanyInput = {
  ruc: string
  razonSocial: string
  nombreComercial?: string
  admin: { nombre: string; apellidos: string; email: string; password: string }
}

export const authApi = {
  login: (email: string, password: string) =>
    request<LoginResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  registerCompany: (input: RegisterCompanyInput) =>
    request<{ empresaId: string; usuarioId: string }>('/api/v1/auth/register-company', {
      method: 'POST',
      body: JSON.stringify(input),
    }),
}
