import './App.css'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'

export default function App() {
  return window.location.pathname === '/register' ? <RegisterPage /> : <LoginPage />
}
