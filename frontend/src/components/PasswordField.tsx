import { useState } from 'react'

type Props = {
  label: string
  value: string
  onChange: (value: string) => void
  autoComplete?: string
  minLength?: number
}

export function PasswordField({ label, value, onChange, autoComplete, minLength = 8 }: Props) {
  const [visible, setVisible] = useState(false)
  return <label>{label}<span className="password-input">
    <input type={visible ? 'text' : 'password'} autoComplete={autoComplete} minLength={minLength} value={value} onChange={e => onChange(e.target.value)} required />
    <button type="button" className="password-toggle" aria-pressed={visible} onClick={() => setVisible(current => !current)}>{visible ? 'Ocultar' : 'Mostrar'}</button>
  </span></label>
}
